using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace sroBot.SROBot
{
    public partial class Bot : MVVM.ViewModelBase
    {
        public Character Char
        {
            get { return GetValue(() => Char); }
            set { SetValue(() => Char, value); }
        }
        public Party Party
        {
            get { return GetValue(() => Party); }
            set { SetValue(() => Party, value); }
        }
        public Chat Chat
        {
            get { return GetValue(() => Chat); }
            set { SetValue(() => Chat, value); }
        }
        public Spawn.Spawns Spawns { get; private set; }
        public Inventory Inventory { get; private set; }
        public Inventory Storage { get; private set; }
        public Inventory GuildStorage { get; private set; }
        public Loop Loop { get; private set; }
        public Configuration Config
        {
            get { return GetValue(() => Config); }
            set { SetValue(() => Config, value); }
        }
        public Proxy Proxy { get; set; }
        public SROServer.Server Server;
        public Alchemy Alchemy
        {
            get { return GetValue(() => Alchemy); }
            set { SetValue(() => Alchemy, value); }
        }

        public Stalling Stall
        {
            get { return GetValue(() => Stall); }
            set { SetValue(() => Stall, value); }
        }

        public Exchanging Exchange
        {
            get { return GetValue(() => Exchange); }
            set { SetValue(() => Exchange, value); }
        }

        public Consignment Consig;
        public ObservableCollection<Consignment.ConsignmentItem> ConsignmentItems { get; set; } = new ObservableCollection<Consignment.ConsignmentItem>();
        private object _consignmentItemsLock = new object();

        public bool Clientless = true;
        public bool AutoReconnect = true;

        public String CharName
        {
            get; set;
        } = "";
        public Mob CurSelected;
        public Mob LastSelected;

        public void SaveLastMob(Mob mob)
        {
            if (mob == null) return;
            LastSelected = mob;
        }

        private bool m_bReturning = false;
        private String m_sBotDir = "";
        //private bool firstSpawn = false;
        private bool firstSpawn => !ConnectionTimes.Any() || ConnectionTimes.Last().Type == ConnectionInfo.CONNECTION_TYPE.DISCONNECTED;
        public class ConnectionInfo
        {
            public enum CONNECTION_TYPE
            {
                NONE = 0,
                DISCONNECTED,
                CONNECTED
            }

            public CONNECTION_TYPE Type { get; set; } = CONNECTION_TYPE.NONE;
            public DateTime Time { get; set; } = DateTime.Now;

            public ConnectionInfo (CONNECTION_TYPE type)
            {
                Type = type;
            }
        }
        public ObservableCollection<ConnectionInfo> ConnectionTimes { get; set; } = new ObservableCollection<ConnectionInfo>();

        private ObservableCollection<SkillInfo> availableSkills = new ObservableCollection<SkillInfo>();
        private object availableSkillsLock = new object();

        private ObservableCollection<SkillInfo> activeBuffs = new ObservableCollection<SkillInfo>();
        private object activeBuffsLock = new object();

        private Dictionary<uint, uint> SkillCastedUniqueIds = new Dictionary<uint, uint>();

        private ObservableCollection<String> logs = new ObservableCollection<string>();
        private object logsLock = new object();

        public static Bot Copy(Bot bot)
        {
            return null;
        }

        public static Bot Load(SROServer.Server server, String bot)
        {
            try
            {
                return new Bot(server, bot);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }

            return null;
        }

        private Bot(SROServer.Server server, String bot)
        {
            Server = server;
            CharName = bot;

            m_sBotDir = Path.Combine(App.ExecutingPath, "server", server.Name, "bots", bot);
            Config = Configuration.Load(server, this);
            Config.Load();

            Char = new Character();
            Party = Party.Create(this);
            Chat = new Chat(this);

            Spawns = new Spawn.Spawns(this);
            Inventory = Inventory.Create(this);
            Storage = Inventory.Create(this);
            GuildStorage = Inventory.Create(this);
            Alchemy = new Alchemy(this);
            Stall = new Stalling(this);
            Exchange= new Exchanging(this);

            Loop = new Loop(this);

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(availableSkills);
            view.Filter = (p) => { var skill = p as SkillInfo; return skill != null && !skill.Type.Contains("_PASSIVE_"); };

            ((CollectionView)CollectionViewSource.GetDefaultView(availableSkills)).SortDescriptions.Add(new SortDescription(nameof(SkillInfo.RequiredMastery1), ListSortDirection.Ascending));
            ((CollectionView)CollectionViewSource.GetDefaultView(availableSkills)).SortDescriptions.Add(new SortDescription(nameof(SkillInfo.SkillGroup), ListSortDirection.Ascending));
            ((CollectionView)CollectionViewSource.GetDefaultView(availableSkills)).SortDescriptions.Add(new SortDescription(nameof(SkillInfo.RequiredMastery1Level), ListSortDirection.Ascending));

            BindingOperations.EnableCollectionSynchronization(availableSkills, availableSkillsLock);
            BindingOperations.EnableCollectionSynchronization(activeBuffs, activeBuffsLock);
            BindingOperations.EnableCollectionSynchronization(logs, logsLock);
            BindingOperations.EnableCollectionSynchronization(ConsignmentItems, _consignmentItemsLock);

            CharSelected += (s, e) =>
            {
                if (Config.AutoStart)
                {
                    new Thread(autoStart).Start();
                }

                new Thread(checkSpawn).Start(Proxy);

                if (Server != null && Server.GetBots().Any())
                {
                    if (Server.GetBots().First().CharName == CharName)
                    {
                        // i am the first..
                        if (SROBot.Party.PartyLeader != CharName)
                        {
                            // i am not the party leader ..
                            Log("get my partyleader position BACK!");
                            SROBot.Party.PartyLeader = CharName;
                        }
                    }
                }

                if (sroClient != null)
                {
                    ProcessHelper.SetWindowTitle(sroClient, "SRO_Client", "SRO_Client " + CharName);
                }

            };

            Disconnected -= bot_Disconnected;
            Disconnected += bot_Disconnected;
        }

        private void checkSpawn(object param)
        {
            try
            {
                var proxy = param as Proxy;

                var timer = 30;
                while (timer-- > 0 && proxy == Proxy && !MainWindow.WillBeClosed)
                {
                    Thread.Sleep(1000);
                }

                if (proxy == null) return;
                if (proxy != Proxy) return;
                if (MainWindow.WillBeClosed) return;

                if (firstSpawn)
                {
                    Log("SPAWN TIMEOUT..");
                    Proxy.Close();
                }
            }
            catch
            {

            }
        }

        public bool IsUsingReturnScroll
        {
            get { return m_bReturning; }
            set { m_bReturning = value; }
        }

        public InventoryItem GetWeapon()
        {
            return Inventory[6];
        }

        public bool UseReturnScroll()
        {
            if (m_bReturning) return true;

            var scroll = Inventory.GetReturnScroll();
            if (scroll == null)
            {
                Log("NO return scroll found!");
                Loop.Stop(Statistic.STOP_REASON.NO_RETURNSCROLL);
                return false;
            }

            Log("use a return scroll!");

            Actions.UseReturnScroll(scroll.Slot, this);

            //m_bReturning = true; // will be set on server-answer -> Inventory.ItemUsed !!

            return true;
        }

        private void reconnect(object arg)
        {
            var timer = Config.AutoReconnectTimer;
            if (timer <= 5) timer = 15;

            long reconnectHandle;

            try
            {
                reconnectHandle = (long)arg;
            }
            catch { return; }

            timer = new Random().Next(1, 5);
            timer *= 60;

            Log($"start reconnect timer: {timer}s -- curHandle: {reconnectHandle}");

            while (timer-- > 0 && !MainWindow.WillBeClosed)
            {
                Thread.Sleep(1000);
            }
            
            if (Proxy != null) return;
            if (MainWindow.WillBeClosed) return;
            if (reconnectHandle != _reconnectHandle)
            {
                Debug($"reconnectHandle: {reconnectHandle} != _reconnectHandle: {_reconnectHandle}");
                return;
            }

            Start();
        }

        private void autoStart()
        {
            var timer = 15;
            //if (!Clientless)
            //    timer *= 2;

            Log("start autostart timer: {0}s", timer);

            while (timer-- > 0 && !MainWindow.WillBeClosed)
            {
                Thread.Sleep(1000);
            }

            if (Proxy == null) return;

            Loop.Start();
        }

        #region loader

        public static String SROPATH = @"C:\sroEurope\alchemy";

        private uint FindPattern(byte[] Pattern, byte[] FileByteArray, uint Result)
        {
            uint num2 = 0;
            for (uint i = 0; i < (FileByteArray.Length - Pattern.Length); i++)
            {
                bool flag = true;
                for (uint j = 0; j < Pattern.Length; j++)
                {
                    if (FileByteArray[i + j] != Pattern[j])
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    num2++;
                    if (Result == num2)
                    {
                        return i;
                    }
                }
            }
            return 0;
        }

        private uint FindStringPattern(byte[] StringByteArray, byte[] FileArray, uint BaseAddress, byte StringWorker, uint Result)
        {
            byte[] buffer3 = new byte[5];
            buffer3[0] = StringWorker;
            byte[] pattern = buffer3;
            byte[] bytes = new byte[4];
            bytes = BitConverter.GetBytes((uint)(BaseAddress + this.FindPattern(StringByteArray, FileArray, 1)));
            pattern[1] = bytes[0];
            pattern[2] = bytes[1];
            pattern[3] = bytes[2];
            pattern[4] = bytes[3];
            return (BaseAddress + this.FindPattern(pattern, FileArray, Result));
        }


        [DllImport("kernel32.dll")]
        private static extern uint VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flAllocationType, uint flProtect);
        [DllImport("kernel32.dll")]
        private static extern uint WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32.dll")]
        private static extern uint WriteProcessMemory(IntPtr hProcess, uint lpBaseAddress, byte[] lpBuffer, int nSize, uint lpNumberOfBytesWritten);

        private uint CallForwardAddress;
        private uint lpBaseAddress;
        private uint MultiClientAddress;
        private uint ByteArray;
        private uint AlreadyProgramExe;
        private byte[] JMP = new byte[] { 0xeb };
        private uint MultiClientError;
        private uint SeedPatchAdress;
        private byte[] SeedPatch = new byte[] { 0xb9, 0x33, 0, 0, 0, 0x90, 0x90, 0x90, 0x90, 0x90 };
        private uint RedirectIPAddress;
        private byte[] AlreadyProgramExeStringPattern = Encoding.ASCII.GetBytes("//////////////////////////////////////////////////////////////////");
        private uint BaseAddress = 0x400000;
        private byte[] RedirectIPAddressPattern = new byte[] {
            0x89, 0x86, 0x2c, 1, 0, 0, 0x8b, 0x17, 0x89, 0x56, 80, 0x8b, 0x47, 4, 0x89, 70,
            0x54, 0x8b, 0x4f, 8, 0x89, 0x4e, 0x58, 0x8b, 0x57, 12, 0x89, 0x56, 0x5c, 0x5e, 0xb8, 1,
            0, 0, 0, 0x5d, 0xc3
         };
        private byte[] SeedPatchPattern = new byte[] { 0x8b, 0x4c, 0x24, 4, 0x81, 0xe1, 0xff, 0xff, 0xff, 0x7f };
        private byte[] MultiClientErrorStringPattern = Encoding.Default.GetBytes("\x00bd\x00c7\x00c5\x00a9\x00b7\x00ce\x00b5\x00e5\x00b0\x00a1 \x00c0\x00cc\x00b9\x00cc \x00bd\x00c7\x00c7\x00e0 \x00c1\x00df \x00c0\x00d4\x00b4\x00cf\x00b4\x00d9.");
        private byte[] MulticlientPattern = new byte[] { 0x6a, 6, 0x8d, 0x44, 0x24, 0x48, 80, 0x8b, 0xcf };
        private byte PUSH = 0x68;
        private byte[] CallForwardPattern = new byte[] {
            0x56, 0x8b, 0xf1, 15, 0xb7, 0x86, 0x3e, 0x10, 0, 0, 0x57, 0x66, 0x8b, 0x7c, 0x24, 0x10,
            15, 0xb7, 0xcf, 0x8d, 20, 1, 0x3b, 150, 0x4c, 0x10, 0, 0
         };

        private void MultiClient(IntPtr SroProcessHandle)
        {
            uint lpBaseAddress = VirtualAllocEx(SroProcessHandle, IntPtr.Zero, 0x2d, 0x1000, 4);
            uint num2 = VirtualAllocEx(SroProcessHandle, IntPtr.Zero, 4, 0x1000, 4);
            uint procAddress = (uint)WinAPI.GetProcAddress(WinAPI.GetModuleHandle("kernel32.dll"), "GetTickCount");
            byte[] bytes = BitConverter.GetBytes((uint)(lpBaseAddress + 0x29));
            byte[] buffer2 = BitConverter.GetBytes((uint)((this.CallForwardAddress - lpBaseAddress) - 0x22));
            byte[] buffer3 = BitConverter.GetBytes(num2);
            byte[] buffer4 = BitConverter.GetBytes((uint)((procAddress - lpBaseAddress) - 0x12));
            byte[] buffer5 = BitConverter.GetBytes((uint)((lpBaseAddress - this.MultiClientAddress) - 5));
            byte[] lpBuffer = new byte[] { 0xe8, buffer5[0], buffer5[1], buffer5[2], buffer5[3] };
            byte[] buffer8 = new byte[] {
                0x8f, 5, 0, 0, 0, 0, 0xa3, 0, 0, 0, 0, 0x60, 0x9c, 0xe8, 0, 0,
                0, 0, 0x8b, 13, 0, 0, 0, 0, 0x89, 0x41, 2, 0x9d, 0x61, 0xe8, 0, 0,
                0, 0, 0xff, 0x35, 0, 0, 0, 0, 0xc3
             };
            buffer8[2] = bytes[0];
            buffer8[3] = bytes[1];
            buffer8[4] = bytes[2];
            buffer8[5] = bytes[3];
            buffer8[7] = buffer3[0];
            buffer8[8] = buffer3[1];
            buffer8[9] = buffer3[2];
            buffer8[10] = buffer3[3];
            buffer8[14] = buffer4[0];
            buffer8[15] = buffer4[1];
            buffer8[0x10] = buffer4[2];
            buffer8[0x11] = buffer4[3];
            buffer8[20] = buffer3[0];
            buffer8[0x15] = buffer3[1];
            buffer8[0x16] = buffer3[2];
            buffer8[0x17] = buffer3[3];
            buffer8[30] = buffer2[0];
            buffer8[0x1f] = buffer2[1];
            buffer8[0x20] = buffer2[2];
            buffer8[0x21] = buffer2[3];
            buffer8[0x24] = bytes[0];
            buffer8[0x25] = bytes[1];
            buffer8[0x26] = bytes[2];
            buffer8[0x27] = bytes[3];
            byte[] buffer7 = buffer8;
            WriteProcessMemory(SroProcessHandle, lpBaseAddress, buffer7, buffer7.Length, this.ByteArray);
            WriteProcessMemory(SroProcessHandle, this.MultiClientAddress, lpBuffer, lpBuffer.Length, this.ByteArray);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, int dwProcessId);
        private void Quickpatches(IntPtr SroProcessHandle)
        {
            WriteProcessMemory(SroProcessHandle, this.AlreadyProgramExe, this.JMP, this.JMP.Length, this.ByteArray);
            WriteProcessMemory(SroProcessHandle, this.MultiClientError, this.JMP, this.JMP.Length, this.ByteArray);
            WriteProcessMemory(SroProcessHandle, this.SeedPatchAdress, this.SeedPatch, this.SeedPatch.Length, this.ByteArray);
        }

        [DllImport("kernel32.dll")]
        private static extern uint ReadProcessMemory(IntPtr hProcess, uint lpBaseAddress, uint lpbuffer, uint nSize, uint lpNumberOfBytesRead);
        public void RedirectIP(IntPtr SroProcessHandle, uint gwPort)
        {
            uint lpBaseAddress = VirtualAllocEx(SroProcessHandle, IntPtr.Zero, 0x1b, 0x1000, 4);
            uint num2 = VirtualAllocEx(SroProcessHandle, IntPtr.Zero, 8, 0x1000, 4);
            byte[] bytes = BitConverter.GetBytes((uint)(((uint)WinAPI.GetProcAddress(WinAPI.GetModuleHandle("WS2_32.dll"), "connect") - lpBaseAddress) - 0x1a));
            byte[] buffer2 = BitConverter.GetBytes(num2);
            byte[] buffer3 = BitConverter.GetBytes((uint)((lpBaseAddress - this.RedirectIPAddress) - 5));
            byte[] buffer4 = BitConverter.GetBytes(Convert.ToUInt32(gwPort));
            byte[] buffer5 = BitConverter.GetBytes(Convert.ToUInt16("127"));
            byte[] buffer6 = BitConverter.GetBytes(Convert.ToUInt16("0"));
            byte[] buffer7 = BitConverter.GetBytes(Convert.ToUInt16("0"));
            byte[] buffer8 = BitConverter.GetBytes(Convert.ToUInt16("1"));
            byte[] buffer12 = new byte[8];
            buffer12[0] = 2;
            buffer12[2] = buffer4[1];
            buffer12[3] = buffer4[0];
            buffer12[4] = buffer5[0];
            buffer12[5] = buffer6[0];
            buffer12[6] = buffer7[0];
            buffer12[7] = buffer8[0];
            byte[] lpBuffer = buffer12;
            byte[] buffer10 = new byte[] { 0xe8, buffer3[0], buffer3[1], buffer3[2], buffer3[3] };
            buffer12 = new byte[] {
                80, 0x66, 0x8b, 0x47, 2, 0x66, 0x3d, 0x3d, 0xa3, 0x75, 5, 0xbf, 0, 0, 0, 0,
                0x58, 0x6a, 0x10, 0x57, 0x51, 0xe8, 0, 0, 0, 0, 0xc3
             };
            buffer12[12] = buffer2[0];
            buffer12[13] = buffer2[1];
            buffer12[14] = buffer2[2];
            buffer12[15] = buffer2[3];
            buffer12[0x16] = bytes[0];
            buffer12[0x17] = bytes[1];
            buffer12[0x18] = bytes[2];
            buffer12[0x19] = bytes[3];
            byte[] buffer11 = buffer12;
            WriteProcessMemory(SroProcessHandle, lpBaseAddress, buffer11, buffer11.Length, this.ByteArray);
            WriteProcessMemory(SroProcessHandle, num2, lpBuffer, lpBuffer.Length, this.ByteArray);
            WriteProcessMemory(SroProcessHandle, this.RedirectIPAddress, buffer10, buffer10.Length, this.ByteArray);
        }


        #endregion

        private Process sroClient = null;

        private static void KillProcessAndChildren(int pid)
        {
            try
            {
                var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
                var moc = searcher.Get();
                foreach (ManagementObject mo in moc)
                {
                    var childPid = Convert.ToInt32(mo["ProcessID"]);
                    if (childPid == pid) continue;
                    KillProcessAndChildren(childPid);
                }
                try
                {
                    Process proc = Process.GetProcessById(pid);
                    proc.Kill();
                }
                catch (ArgumentException)
                {
                    // Process already exited.
                }
            }
            catch { }
        }

        public void KillClient()
        {
            try
            {
                if (sroClient != null)
                {
                    Clientless = true;
                    sroClientVisible = false;
                    KillProcessAndChildren(sroClient.Id);
                    sroClient.Kill();
                    sroClient.Close();
                    sroClient.Dispose();
                    sroClient = null;
                }
            }
            catch { }
        }

        #region show/hide client


        public abstract class ProcessHelper
        {
            [SuppressUnmanagedCodeSecurity]
            public static class UnsafeNativeMethods
            {
                [DllImport("user32.dll")]
                internal static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

                [DllImport("user32.dll")]
                public static extern int SetWindowText(IntPtr hWnd, string text);

                /// <summary>Enumeration of the different ways of showing a window using 
                /// ShowWindow</summary>
                public enum WindowShowStyle : uint
                {
                    /// <summary>Hides the window and activates another window.</summary>
                    /// <remarks>See SW_HIDE</remarks>
                    Hide = 0,
                    /// <summary>Activates and displays a window. If the window is minimized 
                    /// or maximized, the system restores it to its original size and 
                    /// position. An application should specify this flag when displaying 
                    /// the window for the first time.</summary>
                    /// <remarks>See SW_SHOWNORMAL</remarks>
                    ShowNormal = 1,
                    /// <summary>Activates the window and displays it as a minimized window.</summary>
                    /// <remarks>See SW_SHOWMINIMIZED</remarks>
                    ShowMinimized = 2,
                    /// <summary>Activates the window and displays it as a maximized window.</summary>
                    /// <remarks>See SW_SHOWMAXIMIZED</remarks>
                    ShowMaximized = 3,
                    /// <summary>Maximizes the specified window.</summary>
                    /// <remarks>See SW_MAXIMIZE</remarks>
                    Maximize = 3,
                    /// <summary>Displays a window in its most recent size and position. 
                    /// This value is similar to "ShowNormal", except the window is not 
                    /// actived.</summary>
                    /// <remarks>See SW_SHOWNOACTIVATE</remarks>
                    ShowNormalNoActivate = 4,
                    /// <summary>Activates the window and displays it in its current size 
                    /// and position.</summary>
                    /// <remarks>See SW_SHOW</remarks>
                    Show = 5,
                    /// <summary>Minimizes the specified window and activates the next 
                    /// top-level window in the Z order.</summary>
                    /// <remarks>See SW_MINIMIZE</remarks>
                    Minimize = 6,
                    /// <summary>Displays the window as a minimized window. This value is 
                    /// similar to "ShowMinimized", except the window is not activated.</summary>
                    /// <remarks>See SW_SHOWMINNOACTIVE</remarks>
                    ShowMinNoActivate = 7,
                    /// <summary>Displays the window in its current size and position. This 
                    /// value is similar to "Show", except the window is not activated.</summary>
                    /// <remarks>See SW_SHOWNA</remarks>
                    ShowNoActivate = 8,
                    /// <summary>Activates and displays the window. If the window is 
                    /// minimized or maximized, the system restores it to its original size 
                    /// and position. An application should specify this flag when restoring 
                    /// a minimized window.</summary>
                    /// <remarks>See SW_RESTORE</remarks>
                    Restore = 9,
                    /// <summary>Sets the show state based on the SW_ value specified in the 
                    /// STARTUPINFO structure passed to the CreateProcess function by the 
                    /// program that started the application.</summary>
                    /// <remarks>See SW_SHOWDEFAULT</remarks>
                    ShowDefault = 10,
                    /// <summary>Windows 2000/XP: Minimizes a window, even if the thread 
                    /// that owns the window is hung. This flag should only be used when 
                    /// minimizing windows from a different thread.</summary>
                    /// <remarks>See SW_FORCEMINIMIZE</remarks>
                    ForceMinimized = 11
                }

                internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

                [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
                internal static extern int GetWindowTextLength(IntPtr hWnd);

                [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
                internal static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);

                [DllImport("user32.dll", SetLastError = true)]
                internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

                [DllImport("user32.dll")]
                internal static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

                [DllImport("user32.dll")]
                internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

                internal const int PROCESS_WM_READ = 0x0010;

                [DllImport("kernel32.dll")]
                public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

                [DllImport("kernel32.dll")]
                public static extern bool ReadProcessMemory(int hProcess,
                  int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

                internal const int WM_COMMAND = 0x0111;
                internal const int BN_CLICKED = 0;
                //[DllImport("user32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
                //internal static extern IntPtr GetDlgItem(IntPtr hWnd, int nIDDlgItem);
                [DllImport("user32.dll")]
                internal static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);
                [DllImport("kernel32.dll")]
                internal static extern uint GetLastError();

                [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
                public static extern int SendMessageA(IntPtr hwnd, int wMsg, int wParam, uint lParam);

                [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
                public static extern IntPtr GetDlgItem(int hwnd, int childID);

                public const int WM_LBUTTONDOWN = 0x0201;
                public const int WM_LBUTTONUP = 0x0202;

                internal static ProcessModule GetModule(Process p)
                {
                    ProcessModule pm = null;
                    try { pm = p.MainModule; }
                    catch
                    {
                        return null;
                    }
                    return pm;
                }
            }

            public static IntPtr GetWindow(Process proc, String wndTitleMatch)
            {
                IntPtr window = IntPtr.Zero;
                if (proc == null) return window;
                uint tId = 0;

                UnsafeNativeMethods.EnumWindows((wnd, param) =>
                {
                    var pId = UnsafeNativeMethods.GetWindowThreadProcessId(wnd, out tId);

                    if (proc.Threads.OfType<ProcessThread>().FirstOrDefault(t => t.Id == pId) != null)
                    {
                        var curWndTitle = new StringBuilder(UnsafeNativeMethods.GetWindowTextLength(wnd) + 1);
                        UnsafeNativeMethods.GetWindowText(wnd, curWndTitle, curWndTitle.Capacity);

                        if (curWndTitle.ToString().Contains(wndTitleMatch))
                        {
                            window = wnd;
                            return false; // stops EnumWindows ?!
                        }
                    }
                    return true;
                },
                    IntPtr.Zero
                );

                return window;
            }

            public static IntPtr GetWindowWithTitleContaining(Process proc, String wndTitleMatch)
            {
                IntPtr window = IntPtr.Zero;
                uint tId = 0;

                UnsafeNativeMethods.EnumWindows((wnd, param) =>
                {
                    var pId = UnsafeNativeMethods.GetWindowThreadProcessId(wnd, out tId);

                    var thread = proc.Threads.OfType<ProcessThread>().FirstOrDefault(t => t.Id == pId);
                    if (thread != null)
                    {

                        var curWndTitle = new StringBuilder(UnsafeNativeMethods.GetWindowTextLength(wnd) + 1);
                        UnsafeNativeMethods.GetWindowText(wnd, curWndTitle, curWndTitle.Capacity);

                        if (curWndTitle.ToString().Contains(wndTitleMatch))
                        {
                            window = wnd;
                            return false; // stops EnumWindows ?!
                        }
                    }
                    return true;
                },
                    IntPtr.Zero
                );

                return window;
            }

            public static String GetWindowTitle(Process proc, String wndTitleMatch)
            {
                String processWndTitle = "";
                uint tId = 0;

                UnsafeNativeMethods.EnumWindows((wnd, param) =>
                {
                    var pId = UnsafeNativeMethods.GetWindowThreadProcessId(wnd, out tId);

                    if (proc.Threads.OfType<ProcessThread>().FirstOrDefault(t => t.Id == pId) != null)
                    {
                        var curWndTitle = new StringBuilder(UnsafeNativeMethods.GetWindowTextLength(wnd) + 1);
                        UnsafeNativeMethods.GetWindowText(wnd, curWndTitle, curWndTitle.Capacity);

                        if (curWndTitle.ToString().Contains(wndTitleMatch))
                        {
                            processWndTitle = curWndTitle.ToString();
                            return false; // stops EnumWindows ?!
                        }
                    }
                    return true;
                },
                    IntPtr.Zero
                );

                return processWndTitle;
            }

            public static String GetWindowTitle(IntPtr wnd)
            {
                var curWndTitle = new StringBuilder(UnsafeNativeMethods.GetWindowTextLength(wnd) + 1);
                UnsafeNativeMethods.GetWindowText(wnd, curWndTitle, curWndTitle.Capacity);
                return curWndTitle.ToString();
            }

            public static double GetWorkingSet(Process proc)
            {
                if (proc == null) return 0;
                return (double)proc.WorkingSet64 / 1024 / 1024;
            }

            public static void ShowWindow(IntPtr hWnd, UnsafeNativeMethods.WindowShowStyle nCmdShow)
            {
                UnsafeNativeMethods.ShowWindow(hWnd, nCmdShow);
            }

            public static void ShowWindow(Process proc, String wndTitleMatch, UnsafeNativeMethods.WindowShowStyle nCmdShow)
            {
                UnsafeNativeMethods.ShowWindow(ProcessHelper.GetWindow(proc, wndTitleMatch), nCmdShow);
            }

            public static void SetWindowTitle(IntPtr hWnd, String title)
            {
                UnsafeNativeMethods.SetWindowText(hWnd, title);
            }

            public static void SetWindowTitle(Process process, String wndTitleMath, String title)
            {
                UnsafeNativeMethods.SetWindowText(GetWindow(process, wndTitleMath), title);
            }
        }


        private bool sroClientVisible = false;
        private void toggleClient(object param)
        {
            try
            {
                var state = (bool)param;
                if (!state)
                {
                    ProcessHelper.ShowWindow(sroClient, "SRO_Client", ProcessHelper.UnsafeNativeMethods.WindowShowStyle.ShowMinimized);
                }
                else
                {
                    ProcessHelper.ShowWindow(sroClient, "SRO_Client", ProcessHelper.UnsafeNativeMethods.WindowShowStyle.Hide);
                }
            }
            catch { }
        }
        public void ToggleClient()
        {
            new Thread(toggleClient).Start(sroClientVisible);
            sroClientVisible = !sroClientVisible;
        }

        #endregion

        private int cannotStartClientCount = 0;
        private void watchProcess(object param)
        {
            try
            {
                var proc = param as Process;
                if (proc == null) return;

                var running = true;
                do
                {
                    running = !proc.WaitForExit(1000);
                }
                while (running && !MainWindow.WillBeClosed);

                if (!running)
                {
                    Debug("sro client crashed..");
                    if (Proxy != null && !Proxy.HasGatewayConnected)
                    {
                        ++cannotStartClientCount;
                        Proxy.Close();
                    }
                }
            }
            catch { }
        }

        public void StopReconnecting()
        {
            Debug($"stop reconnecting.. {_reconnectHandle + 1}");

            AutoReconnect = false;
            ++_reconnectHandle;
        }

        private long _reconnectHandle = 0;

        private void bot_Disconnected(object sender, object args)
        {
            // is it a real disconnect ?
            if (ConnectionTimes.Any() && ConnectionTimes.Last().Type == ConnectionInfo.CONNECTION_TYPE.CONNECTED)
            {
                Loop.Stop(Statistic.STOP_REASON.DISCONNECTED);

                Party.Left();
                Char.CurHP = 0;
                Char.CurMP = 0;
                Char.IsParsed = false;
            }

            if (AutoReconnect)
            {
                new Thread(reconnect).Start(++_reconnectHandle);
            }

            KillClient();
        }

        public bool Start(bool startedViaUser = false)
        {
            if (Proxy != null) return false;
            if (MainWindow.WillBeClosed) return false;

            Log("trying to connect bot!");

            Clientless = Config.Clientless;
            if (!startedViaUser)
            {
                if (cannotStartClientCount > 5)
                {
                    Clientless = true;
                }
            }
            else
            {
                cannotStartClientCount = 0;
            }

            StopReconnecting();
            AutoReconnect = Config.AutoReconnect;

            //firstSpawn = false;

            Proxy = Proxy.Create("127.0.0.1", Server.Ip, Server.Port, this);
            if (Proxy == null) return false;

            Proxy.PacketReceived += (t, p) => { HandlePacket(t, p); };
            Proxy.Destroyed += (s, e) =>
            {
                //Log("proxy destroyed");
                Proxy = null;
                disconnected();
            };
            
            if (Clientless) return true;

            try
            {
                byte[] fileArray = File.ReadAllBytes(SROPATH + @"\sro_client.exe");
                this.AlreadyProgramExe = this.FindStringPattern(this.AlreadyProgramExeStringPattern, fileArray, this.BaseAddress, this.PUSH, 1) - 2;
                this.SeedPatchAdress = this.BaseAddress + this.FindPattern(this.SeedPatchPattern, fileArray, 1);
                this.MultiClientAddress = (this.BaseAddress + this.FindPattern(this.MulticlientPattern, fileArray, 1)) + 9;
                this.CallForwardAddress = this.BaseAddress + this.FindPattern(this.CallForwardPattern, fileArray, 1);
                this.MultiClientError = this.FindStringPattern(this.MultiClientErrorStringPattern, fileArray, this.BaseAddress, this.PUSH, 1) - 8;
                //Config.StartingMSG = this.FindStringPattern(this.StartingMSGStringPattern, fileArray, this.BaseAddress, this.PUSH, 1) + 0x18;
                //Config.ChangeVersion = this.FindStringPattern(this.ChangeVersionStringPattern, fileArray, this.BaseAddress, this.PUSH, 1);
                this.RedirectIPAddress = (this.BaseAddress + this.FindPattern(this.RedirectIPAddressPattern, fileArray, 1)) - 50;

                string str2 = SROPATH + @"\sro_client.exe";
                //SilkroadSecurityApi.Proxy.Init();
                IntPtr ptr = WinAPI.CreateMutex(IntPtr.Zero, false, "Silkroad Online Launcher");
                IntPtr ptr2 = WinAPI.CreateMutex(IntPtr.Zero, false, "Ready");

                if (sroClient != null)
                {
                    try
                    {
                        KillProcessAndChildren(sroClient.Id);
                        sroClient.Close();
                        sroClient.Dispose();
                        sroClient = null;
                    }
                    catch { }
                }

                sroClient = new Process();
                sroClient.StartInfo.FileName = SROPATH + @"\sro_client.exe";
                sroClient.StartInfo.Arguments = String.Format("0 /{0} 0 0", Server.LocaleVersion);
                sroClient.Start();
                IntPtr sroProcessHandle = WinAPI.OpenProcess(0x1f0fff, 0, (IntPtr)sroClient.Id);
                this.Quickpatches(sroProcessHandle);
                this.RedirectIP(sroProcessHandle, GetGwPort());
                this.MultiClient(sroProcessHandle);
                string startingText = "##########################\nThanks for using AweAlchemy !\nIf you have any question contact me !\nmade by Awesome\n##########################\n(Also thanks to QQ and CP-G Member)";
                byte[] buffer3 = new byte[4];
                buffer3[0] = 0xff;
                buffer3[1] = 0xa5;
                byte[] hexColor = buffer3;
                //this.StartingTextMSG(sroProcessHandle, startingText, hexColor);
                byte[] fileByteArray = File.ReadAllBytes(SROPATH + @"\sro_client.exe");
                this.RedirectIPAddress = (this.BaseAddress + this.FindPattern(this.RedirectIPAddressPattern, fileByteArray, 1)) - 50;
                //Config.guiEvents.Info("Silkroad started", Colors.LimeGreen);
                Debug("started client");
                new Thread(watchProcess).Start(sroClient);

                if (/*Config.MHTCorVSRO == 1*/ true)
                {
                    string sError = "";
                    WinAPI.DoInject(sroClient, "cDetour.dll", out sError);
                }
                sroClientVisible = true;
            }
            catch (Exception a)
            {
                Log("Cannot Launch Silkroad ! " + a.Message);
                Clientless = true;
            }

            return true;
        }

        public SkillInfo GetSkill(String name)
        {
            return availableSkills.FirstOrDefault(s => s.Name.Equals(name));
        }

        public void ClearSkills()
        {
            lock (availableSkillsLock)
            {
                availableSkills.Clear();
            }
        }

        public void AddSkill(SkillInfo skillinfo)
        {
            if (skillinfo == null) return;
            
            var oldSkill = GetSkill(skillinfo.Name);

            lock (availableSkillsLock)
            {
                if (oldSkill != null)
                {
                    Debug($"UPDATED SKILL: type: {skillinfo.Type} skill: {skillinfo.Name} level: {skillinfo.RequiredMastery1Level} model: {skillinfo.Model} id: {skillinfo.SkillId}\r\n");

                    var oldIdx = availableSkills.IndexOf(oldSkill);
                    availableSkills[oldIdx] = skillinfo;
                    Config.UpdateSkill(skillinfo);
                }
                else
                {
                    availableSkills.Add(skillinfo);
                }

            }
        }

        public void RemoveActiveBuff(uint uid)
        {
            lock (activeBuffsLock)
            {
                var buff = activeBuffs.FirstOrDefault(s => s.IngameId == uid);
                if (buff == null) return;
                activeBuffs.Remove(buff);
            }
        }

        public void ClearActiveBuffs()
        {
            lock (activeBuffsLock)
            {
                activeBuffs.Clear();
            }
        }

        public void AddActiveBuff(SkillInfo skillinfo)
        {
            lock (activeBuffsLock)
            {
                foreach (var skill in activeBuffs.Where(s => s.Name == skillinfo.Name).ToArray())
                {
                    activeBuffs.Remove(skill);
                }
                activeBuffs.Add(skillinfo);


            }
        }

        public ObservableCollection<SkillInfo> GetActiveBuffs()
        {
            return activeBuffs;
        }

        public ObservableCollection<SkillInfo> GetAvailableSkills()
        {
            return availableSkills;
        }

        public override string ToString()
        {
            return CharName;
        }

        public uint GetGwPort()
        {
            if (Proxy == null) return 0;
            return Proxy.GwPort;
        }

        public void Debug(String s = "", params object[] args)
        {
            var dateTime = DateTime.Now.ToString("[dd.MM.yy HH:mm:ss.fff]");
            var logString = String.Format(s, args);

            Console.WriteLine("{0}[DEBUG][{1}] {2}", dateTime, CharName, logString);
        }

        public void Log(String s = "", params object[] args)
        {
            var dateTime = DateTime.Now.ToString("[dd.MM.yy HH:mm:ss.fff]");
            var logString = String.Format(s, args);

            lock (logsLock)
            {
                logs.Add(String.Format("{0} {1}", dateTime, logString));
            }

            Console.WriteLine("{0}[ LOG ][{1}] {2}", dateTime, CharName, logString);
        }

        public ObservableCollection<String> GetLogs()
        {
            return logs;
        }
    }
}
