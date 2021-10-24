using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace sroBot
{
    internal class WinAPI
    {
        private const uint MouseEventLeftDown = 2;
        private const uint MouseEventLeftUp = 4;
        public static uint Rights = 0x38;
        private static uint WM_KEYDOWN = 0x100;

        private static byte[] CalcBytes(string sToConvert)
        {
            return Encoding.ASCII.GetBytes(sToConvert);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateMutex(IntPtr lpMutexAttributes, bool bInitialOwner, string lpName);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttribute, IntPtr dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);
        private static bool CRT(Process pToBeInjected, string sDllPath, out string sError, out IntPtr hwnd)
        {
            sError = string.Empty;
            IntPtr hProcess = OpenProcess(0x43a, 1, (uint)pToBeInjected.Id);
            hwnd = hProcess;
            if (hProcess == IntPtr.Zero)
            {
                sError = "Unable to attatch to process.\n";
                sError = sError + "Error code: " + Marshal.GetLastWin32Error();
                return false;
            }
            IntPtr procAddress = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if (procAddress == IntPtr.Zero)
            {
                sError = "Unable to find address of \"LoadLibraryA\".\n";
                sError = sError + "Error code: " + Marshal.GetLastWin32Error();
                return false;
            }
            IntPtr lpBaseAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (IntPtr)sDllPath.Length, 0x3000, 0x40);
            if ((lpBaseAddress == IntPtr.Zero) && (lpBaseAddress == IntPtr.Zero))
            {
                sError = "Unable to allocate memory to target process.\n";
                sError = sError + "Error code: " + Marshal.GetLastWin32Error();
                return false;
            }
            byte[] buffer = CalcBytes(sDllPath);
            IntPtr zero = IntPtr.Zero;
            WriteProcessMemory(hProcess, lpBaseAddress, buffer, (uint)buffer.Length, out zero);
            if (Marshal.GetLastWin32Error() != 0)
            {
                sError = "Unable to write memory to process.";
                sError = sError + "Error code: " + Marshal.GetLastWin32Error();
                return false;
            }
            if (CreateRemoteThread(hProcess, IntPtr.Zero, IntPtr.Zero, procAddress, lpBaseAddress, 0, IntPtr.Zero) == IntPtr.Zero)
            {
                sError = "Unable to load dll into memory.";
                sError = sError + "Error code: " + Marshal.GetLastWin32Error();
                return false;
            }
            return true;
        }

        public static bool DoInject(Process pToBeInjected, string sDllPath, out string sError)
        {
            IntPtr zero = IntPtr.Zero;
            if (!CRT(pToBeInjected, sDllPath, out sError, out zero))
            {
                if (zero != IntPtr.Zero)
                {
                    CloseHandle(zero);
                }
                return false;
            }
            int num = Marshal.GetLastWin32Error();
            return true;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        [DllImport("user32")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheiritHandle, IntPtr dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);
        //[DllImport("user32.dll", SetLastError = true)]
        //private static extern bool PostMessage(IntPtr hWnd, uint msg, Keys wParam, long lParam);
        [DllImport("kernel32")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, uint dwAddress, ref byte[] lpBuffer, int nSize, out int lpBytesRead);
        //public static void SendKey(IntPtr MainWindowHandle, Keys Key)
        //{
        //    PostMessage(FindWindowEx(MainWindowHandle, IntPtr.Zero, "Edit", ""), WM_KEYDOWN, Key, 0L);
        //}

        //public static void SendText(IntPtr MainWindowHandle, string Text)
        //{
        //    IntPtr hWnd = FindWindowEx(MainWindowHandle, IntPtr.Zero, "Edit", "");
        //    foreach (char ch in Text)
        //    {
        //        PostMessage(hWnd, WM_KEYDOWN, (Keys)ch, 0L);
        //    }
        //}

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("KERNEL32.DLL")]
        public static extern bool SetProcessAffinityMask(IntPtr hProcess, uint dwProcessAffinityMask);
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int width, int height, uint uFlags);
        [DllImport("User32")]
        public static extern int ShowWindow(IntPtr hWnd, CmdShow nCmdShow);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);
        public static bool WriteBytes(IntPtr hProcess, uint dwAdress, byte[] lpByteBuffer, int nSize)
        {
            IntPtr zero = IntPtr.Zero;
            IntPtr lpBuffer = GCHandle.Alloc(lpByteBuffer, GCHandleType.Pinned).AddrOfPinnedObject();
            return WriteProcessMemory(hProcess, dwAdress, lpBuffer, nSize, out zero);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, uint size, out IntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, uint dwAddress, IntPtr lpBuffer, int nSize, out IntPtr iBytesWritten);

        public enum CmdShow
        {
            SW_HIDE = 0,
            SW_MAXIMIZE = 3,
            SW_MINIMIZE = 6,
            SW_RESTORE = 9,
            SW_SHOW = 5
        }

        [Flags]
        public enum MouseEventFlags
        {
            LeftDown = 2,
            LeftUp = 4
        }

        public static class VAE_Enums
        {
            public enum AllocationType
            {
                MEM_COMMIT = 0x1000,
                MEM_RESERVE = 0x2000,
                MEM_RESET = 0x80000
            }

            public enum ProtectionConstants
            {
                PAGE_EXECUTE = 0x10,
                PAGE_EXECUTE_READ = 0x20,
                PAGE_EXECUTE_READWRITE = 0x40,
                PAGE_EXECUTE_WRITECOPY = 0x80,
                PAGE_NOACCESS = 1
            }
        }
    }
}
