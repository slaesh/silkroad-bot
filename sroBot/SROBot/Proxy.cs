using SilkroadSecurityApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using System.Collections.ObjectModel;

namespace sroBot.SROBot
{
    public class Proxy
    {
        private static ILog log = LogManager.GetLogger(typeof(Proxy));
        private static ILog packetLogger = LogManager.GetLogger("PacketLog");
        private static int PROXYHANDLE = 0;

        private static uint nextPortToUse = 19000;

        public bool AutomaticLogin = true;

        public bool LogPackets = false;
        public Func<Packet, bool> LogPacketCheck = null;

        public ObservableCollection<string> LoggedPackets { get; set; } = new ObservableCollection<string>();

        class Context
        {
            public Socket Socket { get; set; }
            public Security Security { get; set; }
            public TransferBuffer Buffer { get; set; }
            public Security RelaySecurity { get; set; }

            public Context()
            {
                Socket = null;
                Security = new Security();
                RelaySecurity = null;
                Buffer = new TransferBuffer(8192);
            }
        }

        private Thread gwThread;
        private Thread agThread;
        private String localIp;

        public uint GwPort;
        public uint AgPort;

        private Context local_context; // connection to sro server
        private Context remote_context; // connection to client

        private Bot bot;
        public bool IsGatewayCreated = false;
        public bool IsAgentCreated = false;
        public bool HasGatewayConnected = false;
        public bool HasAgentConnected = false;

        private System.Timers.Timer pingTimer;
        private bool m_connected = false;
        private int handle = ++PROXYHANDLE;

        private enum PACKETDIRECTION
        {
            BOT_TO_SERVER = 0,
            SERVER_TO_BOT = 1,
            BOT_TO_CLIENT = 2,
            CLIENT_TO_BOT = 3
        }

        private Dictionary<PACKETDIRECTION, string> packetDirection = new Dictionary<PACKETDIRECTION, string>
        {
            { PACKETDIRECTION.BOT_TO_SERVER, "B->S" },
            { PACKETDIRECTION.SERVER_TO_BOT, "S->B" },
            { PACKETDIRECTION.BOT_TO_CLIENT, "B->C" },
            { PACKETDIRECTION.CLIENT_TO_BOT, "C->B" },
        };

        private void connectTimeout(object handle)
        {
            try
            {
                var timer = 45;
                while (timer-- > 0 && !isDestroyed && !MainWindow.WillBeClosed && !exitThreads)
                {
                    Thread.Sleep(1000);
                }

                if (!m_connected)
                {
                    //bot.Log("proxy({0}): connection timeout!", this.handle);
                    Close();
                }
                else if (isDestroyed || MainWindow.WillBeClosed)
                {
                    bot.Log("proxy({0}): destroyed or app wants to be closed..", this.handle);
                    Close();
                }
            }
            catch { }
        }

        private Proxy(String localIp, String sroServerIp, uint sroServerPort, Bot bot)
        {
            LogPacketCheck = (p) => { return true; };

            this.localIp = localIp;
            this.bot = bot;

            gwThread = new Thread(new ParameterizedThreadStart(proxyThread));
            gwThread.Start(new { mode = "gateway", ip = sroServerIp, port = sroServerPort });

            new Thread(connectTimeout).Start();
        }

        public static Proxy Create(String gwIp, String sroServerIp, uint sroServerPort, Bot bot)
        {
            return new Proxy(gwIp, sroServerIp, sroServerPort, bot);
        }

        public void SendToSilkroadServer(Packet packet)
        {
            if (bot.Clientless)
            {
                remote_context.Security.Send(packet);
            }
            else
            {
                local_context.RelaySecurity.Send(packet);
            }
        }

        public void SendToSilkroadClient(Packet packet)
        {
            if (bot.Clientless) return;
            remote_context.RelaySecurity.Send(packet);
        }

        private bool exitThreads = false;

        public uint BindToFreePort(Socket server, String ip)
        {
            var port = nextPortToUse;

            while (true)
            {
                try
                {
                    server.Bind(new IPEndPoint(IPAddress.Parse(ip), (int)port));
                    server.Listen(1);
                }
                catch { ++port; continue; }

                nextPortToUse = port + 1;
                break;
            }

            return port;
        }

        //private void waitAndCheckConnectedClient(object modeN

        private void proxyThread(object mode)
        {
            String type = "";
            try
            {
                String gateway_host = localIp;

                String agent_host = localIp;

                dynamic dyn = (dynamic)mode;

                String remote_host = dyn.ip as String;
                var remote_port = (Int32)dyn.port;

                type = dyn.mode as String; //  "gateway"; // or "agent"

                //bot.Log("proxy({0}): thread {1} started", handle, type);

                if (type == "agent")
                {
                    IsAgentCreated = true;

                    if (pingTimer != null)
                    {
                        try
                        {
                            pingTimer.Stop();
                            pingTimer.Close();
                            pingTimer.Dispose();
                        }
                        catch { }
                    }

                    pingTimer = new System.Timers.Timer();
                    pingTimer.Interval = 4500;
                    pingTimer.Elapsed += (s, e) =>
                    {
                        if (bot.Clientless)
                        {
                            var response = new Packet(0x2002);
                            SendToSilkroadServer(response);
                        }
                    };
                    pingTimer.Start();
                }
                else
                {
                    IsGatewayCreated = true;
                }

                local_context = new Context();
                local_context.Security.GenerateSecurity(true, true, true);

                remote_context = new Context();

                remote_context.RelaySecurity = local_context.Security;
                local_context.RelaySecurity = remote_context.Security;

                List<Context> contexts = new List<Context>();
                if (!bot.Clientless)
                {
                    contexts.Add(local_context);
                }
                else
                {
                    local_context.Security.SetHandshakeAccepted(true); // need to process outgoing packets .. !!
                }
                contexts.Add(remote_context);

                if (!bot.Clientless)
                {
                    using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        if (type == "gateway")
                        {
                            GwPort = BindToFreePort(server, gateway_host);
                            bot.Debug("proxy({0}): {1} bound to port {2}", handle, type, GwPort);
                        }
                        else if (type == "agent")
                        {
                            AgPort = BindToFreePort(server, agent_host);
                            bot.Debug("proxy({0}): {1} bound to port {2}", handle, type, AgPort);
                        }

                        local_context.Socket = server.Accept();

                        bot.Debug("{0} connection accepted", type, GwPort);

                        if (type == "gateway")
                        {
                            HasGatewayConnected = true;
                        }
                        else if (type == "agent")
                        {
                            HasAgentConnected = true;
                        }
                    }
                }

                if (exitThreads || MainWindow.WillBeClosed) throw new Exception("thread should be closed!!!");

                using (local_context.Socket)
                {
                    using (remote_context.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        remote_context.Socket.Connect(remote_host, remote_port);
                        while (!exitThreads && !MainWindow.WillBeClosed)
                        {
                            foreach (Context context in contexts.ToArray()) // Network input event processing
                            {
                                if (context.Socket.Poll(0, SelectMode.SelectRead))
                                {
                                    try
                                    {
                                        int count = context.Socket.Receive(context.Buffer.Buffer);
                                        if (count == 0)
                                        {
                                            if (context == local_context)
                                            {
                                                bot.Log("Client CRASHED .. trying to keep connection alive!", handle);
                                                bot.KillClient();
                                                local_context.Security.SetHandshakeAccepted(true); // need to process outgoing packets .. !!
                                                
                                                contexts.Remove(context);
                                                continue;
                                            }
                                            throw new Exception("The remote connection has been lost.");
                                        }
                                        context.Security.Recv(context.Buffer.Buffer, 0, count);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (context == local_context)
                                        {
                                            bot.Log("Client CRASHED .. trying to keep connection alive!!", handle);
                                            bot.KillClient();
                                            local_context.Security.SetHandshakeAccepted(true); // need to process outgoing packets .. !!

                                            contexts.Remove(context);
                                            continue;
                                        }

                                        logEvent("proxy({0}): {1} | connection lost!!", handle, type);
                                        //bot.Debug("proxy({0}): {1} | connection lost!! => {2} {3}", handle, type, ex.Message, ex.StackTrace);
                                        throw new Exception("connection lost !");
                                    }
                                }
                            }

                            foreach (Context context in contexts) // Logic event processing
                            {
                                List<Packet> packets = context.Security.TransferIncoming();
                                if (packets != null)
                                {
                                    foreach (Packet packet in packets)
                                    {
                                        if (AutomaticLogin && packet.Opcode == 0x6103 && context == local_context)
                                        {
                                            //Console.WriteLine("login from client -- SKIP IT!! {0} => {1}", String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))), context == local_context);
                                            continue;
                                        }
                                        if (AutomaticLogin && packet.Opcode == 0x9001 && context == local_context)
                                        {
                                            Console.WriteLine("HWID from client -- SKIP IT!! {0} => {1}", String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))), context == local_context);
                                            continue;
                                        }

                                        // logpacket weiter unten erschlägt alles !?!
                                        if (bot.Clientless || context == local_context)
                                            logPacket(packet, type, context == remote_context);

                                        if (context == remote_context)
                                        {
                                            logPacketToWindow(packet, type, PACKETDIRECTION.SERVER_TO_BOT);
                                        }
                                        else if (context == local_context)
                                        {
                                            logPacketToWindow(packet, type, PACKETDIRECTION.CLIENT_TO_BOT);
                                        }

                                        if (context == remote_context) // rx from server !
                                        {
                                            if (bot.HandlePacket(type, packet)) continue;
                                        }
                                        
                                        if (bot.Loop.IsStoring() &&
                                            (packet.Opcode == 0xb046 ||
                                            packet.Opcode == 0xb045 ||
                                            packet.Opcode == 0xb250 ||
                                            packet.Opcode == 0xb251 ||
                                            packet.Opcode == 0xb252 ||
                                            packet.Opcode == 0xb03c ||
                                            packet.Opcode == 0xb04b) &&
                                            context == remote_context)
                                        {
                                            bot.Debug("SKIP {0} !!!", packet.Opcode.ToString("X4"));
                                            continue;
                                        }

                                        if (packet.Opcode == 0x5000 || packet.Opcode == 0x9000) // ignore always
                                        {
                                        }
                                        else if (packet.Opcode == 0x2001)
                                        {
                                            if (!bot.Clientless && context == remote_context) // ignore local to proxy only
                                            {
                                                context.RelaySecurity.Send(packet); // proxy to remote is handled by API
                                            }
                                            else if ((bot.Clientless || AutomaticLogin) && type == "agent")
                                            {
                                                //Console.WriteLine("AGENT: send login");

                                                Packet p = new Packet(0x6103);
                                                p.WriteUInt32((uint)dyn.loginId);
                                                p.WriteAscii(bot.Config.AccountName.ToLower());
                                                p.WriteAscii(bot.Config.AccountPass);
                                                p.WriteUInt8(bot.Server.LocaleVersion);

                                                var mac = new byte[6];
                                                Global.Random.NextBytes(mac);
                                                p.WriteInt8Array(mac.Select(b => b as object).ToArray(), 0, 6);

                                                SendToSilkroadServer(p);
                                            }

                                        }
                                        else if (bot.Clientless && packet.Opcode == 0xA103)
                                        {
                                            var err = packet.ReadUInt8();
                                            if (err == 2)
                                            {
                                                bot.Log("proxy({0}): 0xA103: Error: {1}", handle, packet.ReadUInt8());
                                            }
                                            else
                                            {
                                                Packet p = new Packet(0x7007);
                                                p.WriteUInt8(2);

                                                SendToSilkroadServer(p);
                                            }
                                        }
                                        else if (packet.Opcode == 0xA102)
                                        {
                                            byte result = packet.ReadUInt8();
                                            if (result == 1)
                                            {
                                                uint id = packet.ReadUInt32();
                                                string ip = packet.ReadAscii();
                                                ushort port = packet.ReadUInt16();

                                                agThread = new Thread(new ParameterizedThreadStart(proxyThread));
                                                agThread.Start(new { mode = "agent", ip, port, loginId = id });

                                                Thread.Sleep(250); // [war 250] Should be enough time, if not, increase, but too long and C9 timeout results

                                                Packet new_packet = new Packet(0xA102, true);
                                                new_packet.WriteUInt8(result);
                                                new_packet.WriteUInt32(id);
                                                new_packet.WriteAscii(agent_host);
                                                new_packet.WriteUInt16(AgPort);

                                                context.RelaySecurity.Send(new_packet);
                                            }
                                            else if (result == 2)
                                            {
                                                var errCode = packet.ReadUInt8();

                                                if (errCode == 1)
                                                {
                                                    var maxAttempts = packet.ReadUInt32();
                                                    var attempts = packet.ReadUInt32();

                                                    bot.Log($"Wrong Username/Pass ! {attempts}/{maxAttempts}");
                                                    bot.StopReconnecting();
                                                }
                                                else if (errCode == 2) // blocked ?!
                                                {
                                                    var errType = packet.ReadUInt8();

                                                    switch (errType)
                                                    {
                                                        case 1: // blocked
                                                            {
                                                                var reason = packet.ReadAscii();
                                                                var year = packet.ReadUInt16();
                                                                var month = packet.ReadUInt16();
                                                                var day = packet.ReadUInt16();
                                                                var hour = packet.ReadUInt16();
                                                                var minute = packet.ReadUInt16();
                                                                var second = packet.ReadUInt16();
                                                                var microSecond = packet.ReadUInt16();

                                                                bot.Log($"Blocked: '{reason}' --> {day:D2}.{month:D2}.{year:D4} -- {hour:D2}:{minute:D2}:{second:D2}");
                                                                bot.StopReconnecting();
                                                            }
                                                            break;

                                                        case 2:
                                                            {
                                                                bot.Log($"Blocked login for inspection?!");
                                                                bot.StopReconnecting();
                                                            }
                                                            break;

                                                        case 3:
                                                            {
                                                                bot.Log($"Blocked p2p trade?!");
                                                                bot.StopReconnecting();
                                                            }
                                                            break;

                                                        case 4:
                                                            {
                                                                bot.Log($"Blocked chat?!");
                                                                bot.StopReconnecting();
                                                            }
                                                            break;

                                                        default:
                                                            bot.Log("error during login !! => {1}", handle, String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                                                            break;
                                                    }
                                                }
                                                else if (errCode == 3)
                                                {
                                                    bot.Log($"This user is already connected.");
                                                }
                                                else if (errCode == 4)
                                                {
                                                    bot.Log($"The server is full, please try again later.");
                                                }
                                                else if (errCode == 8)
                                                {
                                                    bot.Log($"Faild to connect to server because access to the current IP has exceeded its limit.");
                                                }
                                                else
                                                {
                                                    bot.Log("error during login !! => {1}", handle, String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                                                }

                                                //      04 - "Faild to Connect to Server (C5)."
                                                //      05 - "The server is full, please try again later."
                                                //      06 - "Faild to Connect to Server (C7)."
                                                //      07 - "Faild to Connect to Server (C8)"
                                                //      08 - "Faild to connect to server because access to the current IP has exceeded its limit."
                                                //      09 - "0"
                                                //      10 - "Only adults over the age of 18 are allowed to connect to server."
                                                //      11 - "Only users over the age of 12 are allowed to connect to the server."
                                                //      12 - "Adults over the age of 18 are not allowed to connect to the Teen server."

                                                Close();
                                            }
                                            else if (result == 3)
                                            {
                                                var unknown = packet.ReadUInt8Array(2);
                                                var msg = packet.ReadAscii();

                                                bot.Log($"Cant login: '{msg}'");
                                                bot.StopReconnecting();

                                                Close();
                                            }
                                            else
                                            {
                                                bot.Log("error during login !! => {1}", handle, String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));

                                                Close();
                                            }
                                        }
                                        else if (packet.Opcode == 0xb034)
                                        {
                                            // catch incoming buy packet !
                                            if (context == remote_context) // SRO-Server --> "proxy" --> Client
                                            {
                                                if (bot.Loop.IsBuying() && packet.GetBytes()[0] == 1 && packet.GetBytes()[1] == 8) // SROClient can't handle buy response if bought via bot .. !!
                                                {
                                                    //bot.Log("---> PARSE ITEM MOVEMENT TWICE??");
                                                    //bot.Inventory.MovementUpdate(packet);
                                                }
                                                else // manual buying..
                                                {
                                                    context.RelaySecurity.Send(packet); // send to SROClient
                                                }
                                            }
                                            else // should never happen? SROClient to server?
                                            {
                                                context.RelaySecurity.Send(packet); // send to Server
                                            }
                                        }
                                        else if (context == local_context && packet.Opcode == (ushort)SROData.Opcodes.CLIENT.PETACTION && packet.GetBytes().Length == 9 && packet.GetBytes()[4] == 8)
                                        {
                                            // catch client pet actions
                                            bot.Debug("proxy({0}): catch client PET pick cmd..", handle);
                                        }
                                        else if (context == remote_context && packet.Opcode == 0xb025 && packet.GetBytes().Length >= 3 && packet.GetBytes()[2] > 250)
                                        {
                                            // do not send to client .. ! :)
                                        }
                                        else
                                        {
                                            context.RelaySecurity.Send(packet);
                                        }



                                        if (packet.Opcode == 0x7025)
                                        {
                                            bot.Chat.HandleOutgoingPacket(packet);
                                        }
                                        else if (packet.Opcode == 0xA103 && packet.GetBytes()[0] == 1)
                                        {
                                            bot.Debug("proxy({0}): 0xA103: OK", handle);
                                            connected();
                                        }
                                        else if (packet.Opcode == (ushort)SROData.Opcodes.CLIENT.TELEPORT)
                                        {
                                            bot.Debug("teleport: {0}", String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                                        }

                                        if (context == remote_context) // RX from Server
                                        {
                                            var packet_bytes = packet.GetBytes();
                                            switch (packet.Opcode)
                                            {
                                                case 0xb250:
                                                case 0xb251:
                                                case 0xb252:
                                                case 0xb034:
                                                case 0xb03c:
                                                case 0xb045:
                                                case 0xb046:
                                                case 0xb04b:
                                                    // debug storage stuff..
                                                    //bot.Debug("{7}  |  [{0}][{1:X4}][{2} bytes]{3}{4}{6}{5}{6}", true /*context == local_context*/ ? "S->C" : "C->S", packet.Opcode, packet_bytes.Length, packet.Encrypted ? "[Encrypted]" : "", packet.Massive ? "[Massive]" : "", Utility.HexDump(packet_bytes), Environment.NewLine, type);
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }

                            foreach (Context context in contexts) // Network output event processing
                            {
                                if (context.Socket.Poll(0, SelectMode.SelectWrite))
                                {
                                    //Console.WriteLine("transfer outgoing start");
                                    List<KeyValuePair<TransferBuffer, Packet>> buffers = context.Security.TransferOutgoing();
                                    //Console.WriteLine("transfer outgoing ende");
                                    if (buffers != null)
                                    {
                                        foreach (KeyValuePair<TransferBuffer, Packet> kvp in buffers)
                                        {
                                            TransferBuffer buffer = kvp.Key;
                                            Packet packet = kvp.Value;

                                            byte[] packet_bytes = packet.GetBytes();
                                            logPacket(packet, type, context == local_context);

                                            if (context == remote_context)
                                            {
                                                logPacketToWindow(packet, type, PACKETDIRECTION.BOT_TO_SERVER);
                                            }
                                            else if (context == local_context)
                                            {
                                                logPacketToWindow(packet, type, PACKETDIRECTION.BOT_TO_CLIENT);
                                            }

                                            while (true)
                                            {
                                                int count = context.Socket.Send(buffer.Buffer, buffer.Offset, buffer.Size, SocketFlags.None);
                                                buffer.Offset += count;
                                                if (buffer.Offset == buffer.Size)
                                                {
                                                    break;
                                                }
                                                Thread.Sleep(1);
                                            }

                                            // tx from client
                                            if (context != local_context)
                                            {
                                                switch (packet.Opcode)
                                                {
                                                    case 0x7250:
                                                    case 0x7251:
                                                    case 0x7252:

                                                    case 0x7034:
                                                    case 0x703c:

                                                    case 0x7045:
                                                    case 0x704b:
                                                        // debug storage stuff..
                                                        //bot.Debug("{7}  |  [{0}][{1:X4}][{2} bytes]{3}{4}{6}{5}{6}", false /*context == local_context*/ ? "S->C" : "C->S", packet.Opcode, packet_bytes.Length, packet.Encrypted ? "[Encrypted]" : "", packet.Massive ? "[Massive]" : "", Utility.HexDump(packet_bytes), Environment.NewLine, type);
                                                        break;

                                                    case (ushort)SROData.Opcodes.CLIENT.NPCSELECT:
                                                        //Console.WriteLine("CLIENT SELECTED AN NPC !!");
                                                        bot.Loop.CurrentNPCId = packet.ReadUInt32(); // to catch manual buying .. !

                                                        //bot.Debug("{7}  |  [{0}][{1:X4}][{2} bytes]{3}{4}{6}{5}{6}", false /*context == local_context*/ ? "S->C" : "C->S", packet.Opcode, packet_bytes.Length, packet.Encrypted ? "[Encrypted]" : "", packet.Massive ? "[Massive]" : "", Utility.HexDump(packet_bytes), Environment.NewLine, type);
                                                        break;

                                                    case 0x705B:
                                                        {
                                                            //bot.Log("TELEPORT CANCELED");

                                                            bot.IsUsingReturnScroll = false;
                                                            if (bot.Loop.IsStarted)
                                                            {
                                                                bot.Loop.Start();
                                                            }
                                                        }
                                                        break;

                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (bot.Clientless)
                            {
                                local_context.Security.TransferOutgoing();
                            }

                            Thread.Sleep(1); // Cycle complete, prevent 100% CPU usage
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                bot.Debug("proxy({0}): {1} => {2}", handle, ex.Message, ex.StackTrace);
            }

            //bot.Log("proxy({0}): thread {1} ended", handle, type);

            if (type.Equals("agent") || !IsAgentCreated)
            {
                if (pingTimer != null)
                {
                    try
                    {
                        pingTimer.Stop();
                        pingTimer.Close();
                        pingTimer.Dispose();
                    }
                    catch { }
                }

                //bot.Log("proxy({0}): disconnected", handle);
                destroyed();
            }
        }

        public delegate void logIt(String s, params object[] args);
        public event logIt LogIt;

        private void logEvent(String s, params object[] args)
        {
            LogIt?.Invoke(s, args);
        }

        private void logPacket(Packet packet, String serverType, bool serverToClient)
        {
            if (packet == null) return;
            if (!LogPackets) return;
            if (LogPacketCheck != null && !LogPacketCheck(packet)) return;

            byte[] packet_bytes = packet.GetBytes();
            packetLogger.DebugFormat("{7}  |  [{0}][{1:X4}][{2} bytes]{3}{4}{6}{5}{6}", serverToClient /*context == local_context*/ ? "S->C" : "C->S", packet.Opcode, packet_bytes.Length, packet.Encrypted ? "[Encrypted]" : "", packet.Massive ? "[Massive]" : "", Utility.HexDump(packet_bytes), Environment.NewLine, serverType);
        }

        private void logPacketToWindow(Packet packet, String serverType, PACKETDIRECTION pDirection)
        {
            if (packet == null || bot == null || bot.Config == null || bot.Config.PacketLogging == null) return;
            if (!bot.Config.PacketLogging.Enable) return;

            if (bot.Config.PacketLogging.UseIgnoreList && bot.Config.PacketLogging.IgnoredPackets.Any(p => string.Equals(packet.Opcode.ToString("X"), p, StringComparison.OrdinalIgnoreCase))) return;
            if (bot.Config.PacketLogging.ShowOnlyFiltered && !bot.Config.PacketLogging.FilteredPackets.Any(p => string.Equals(packet.Opcode.ToString("X"), p, StringComparison.OrdinalIgnoreCase))) return;

            if (!(
                (bot.Config.PacketLogging.ShowBotToClient && pDirection == PACKETDIRECTION.BOT_TO_CLIENT) ||
                (bot.Config.PacketLogging.ShowClientToBot && pDirection == PACKETDIRECTION.CLIENT_TO_BOT) ||
                (bot.Config.PacketLogging.ShowBotToServer && pDirection == PACKETDIRECTION.BOT_TO_SERVER) ||
                (bot.Config.PacketLogging.ShowServerToBot && pDirection == PACKETDIRECTION.SERVER_TO_BOT)
                )) return;

            byte[] packet_bytes = packet.GetBytes();
            var s = string.Format(DateTime.Now.ToString("HH:mm:ss.fff") + " >> {7}  |  [{0}][{1:X4}][{2} bytes]{3}{4}{6}{5}{6}", packetDirection[pDirection], packet.Opcode, packet_bytes.Length, packet.Encrypted ? "[Encrypted]" : "", packet.Massive ? "[Massive]" : "", Utility.HexDump(packet_bytes), Environment.NewLine, serverType);
            App.Current.Dispatcher.Invoke(() => LoggedPackets.Add(s));
            Console.WriteLine(s);
        }

        public void Close()
        {
            exitThreads = true;

            if (!bot.Clientless)
            {
                try
                {
                    if (GwPort != 0)
                    {
                        var tcpc = new TcpClient("localhost", (int)GwPort);
                        tcpc.SendTimeout = 100;
                        tcpc.ReceiveTimeout = 100;
                        tcpc.Connect("localhost", (int)GwPort);
                    }
                }
                catch { }

                try
                {
                    if (AgPort != 0)
                    {
                        var tcpc = new TcpClient("localhost", (int)AgPort);
                        tcpc.SendTimeout = 100;
                        tcpc.ReceiveTimeout = 100;
                        tcpc.Connect("localhost", (int)AgPort);
                    }
                }
                catch { }
            }
        }

        public delegate void _packetReceived(String type, Packet packet);
        public event _packetReceived PacketReceived;

        private void packetReceived(String type, Packet packet)
        {
            PacketReceived?.Invoke(type, packet);
        }

        public event EventHandler Destroyed;

        private bool isDestroyed = false;
        private void destroyed()
        {
            isDestroyed = true;
            m_connected = false;
            Destroyed?.Invoke(this, new EventArgs());
        }

        public EventHandler Connected;

        private void connected()
        {
            m_connected = true;
            Connected?.Invoke(this, new EventArgs());
        }

        public void PrintContextPacketCounts()
        {
            Console.WriteLine("  local.Security:");
            Console.WriteLine("     in: {0}", local_context.Security.GetIncomingCount());
            Console.WriteLine("    out: {0}", local_context.Security.GetOutgoingCount());
            Console.WriteLine("  local.RelaySecurity:");
            Console.WriteLine("     in: {0}", local_context.RelaySecurity.GetIncomingCount());
            Console.WriteLine("    out: {0}", local_context.RelaySecurity.GetOutgoingCount());
            Console.WriteLine();
            Console.WriteLine("  remote.Security:");
            Console.WriteLine("     in: {0}", remote_context.Security.GetIncomingCount());
            Console.WriteLine("    out: {0}", remote_context.Security.GetOutgoingCount());
            Console.WriteLine("  remote.RelaySecurity:");
            Console.WriteLine("     in: {0}", remote_context.RelaySecurity.GetIncomingCount());
            Console.WriteLine("    out: {0}", remote_context.RelaySecurity.GetOutgoingCount());
        }
    }
}
