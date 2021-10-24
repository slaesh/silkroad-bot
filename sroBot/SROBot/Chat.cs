using SilkroadSecurityApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace sroBot.SROBot
{
    public class Chat : MVVM.ViewModelBase
    {
        public bool ShowNormal
        {
            get { return GetValue(() => ShowNormal); }
            set { SetValue(() => ShowNormal, value); filterChanged(); }
        }
        public bool ShowPrivate
        {
            get { return GetValue(() => ShowPrivate); }
            set { SetValue(() => ShowPrivate, value); filterChanged(); }
        }
        public bool ShowParty
        {
            get { return GetValue(() => ShowParty); }
            set { SetValue(() => ShowParty, value); filterChanged(); }
        }
        public bool ShowGuild
        {
            get { return GetValue(() => ShowGuild); }
            set { SetValue(() => ShowGuild, value); filterChanged(); }
        }
        public bool ShowGlobal
        {
            get { return GetValue(() => ShowGlobal); }
            set { SetValue(() => ShowGlobal, value); filterChanged(); }
        }
        public bool ShowNotice
        {
            get { return GetValue(() => ShowNotice); }
            set { SetValue(() => ShowNotice, value); filterChanged(); }
        }
        public bool ShowUnion
        {
            get { return GetValue(() => ShowUnion); }
            set { SetValue(() => ShowUnion, value); filterChanged(); }
        }
        public bool ShowUnique
        {
            get { return GetValue(() => ShowUnique); }
            set { SetValue(() => ShowUnique, value); filterChanged(); }
        }
        public bool ShowStall
        {
            get { return GetValue(() => ShowStall); }
            set { SetValue(() => ShowStall, value); filterChanged(); }
        }
        public ObservableCollection<String> ChatTypes
        {
            get { return GetValue(() => ChatTypes); }
            set { SetValue(() => ChatTypes, value); }
        }
        private object chatTypesLock = new object();

        private byte chatCount = 251;

        public enum CHAT_TYPE
        {
            NORMAL = 0x01,
            PRIVATE = 0x02,
            GAMEMASTER = 0x03,
            PARTY = 0x04,
            GUILD = 0x05,
            GLOBAL = 0x06,
            NOTICE = 0x07,
            UNION = 0x0B,

            STALL = 0x09,

            ERROR = 0xfe,
            UNIQUE = 0xff
        }

        public class Message
        {
            public DateTime Time = DateTime.Now;
            public CHAT_TYPE Type
            {
                get; set;
            }
            public String Name = "";
            public String Text = "";
            public bool Incoming = true;

            public Message(CHAT_TYPE type, String name, String txt, bool incoming = true)
            {
                Type = type;
                Name = name;
                Text = txt;
                Incoming = incoming;
                //ShowText = String.Format("[{0}][{1}] {2}{3} : {4}", Time.ToString("dd.MM.yy HH:mm:ss"), Type, !incoming ? "(TO)" : "", Name, Text);
            }

            public String ShowText
            {
                get
                {
                    switch (Type)
                    {
                        case CHAT_TYPE.NORMAL:
                            return String.Format("[{0}] {1} : {2}", Time.ToString("dd.MM.yy HH:mm:ss"), Name, Text);

                        case CHAT_TYPE.GLOBAL:
                            return String.Format("[{0}] {1} : {2}", Time.ToString("dd.MM.yy HH:mm:ss"), Name, Text);

                        case CHAT_TYPE.GUILD:
                            return String.Format("[{0}] {1}(Guild) : {2}", Time.ToString("dd.MM.yy HH:mm:ss"), Name, Text);

                        case CHAT_TYPE.PARTY:
                            return String.Format("[{0}] {1}(Party) : {2}", Time.ToString("dd.MM.yy HH:mm:ss"), Name, Text);

                        case CHAT_TYPE.PRIVATE:
                            return String.Format("[{0}] {1}{2} : {3}", Time.ToString("dd.MM.yy HH:mm:ss"), Name, Incoming ? "(FROM)" : "(TO)", Text);

                        case CHAT_TYPE.UNION:
                            {
                                var guildname = Text.Split(new String[] { "):" }, StringSplitOptions.RemoveEmptyEntries)[0];
                                var txt = String.Join("):", Text.Split(new String[] { "):" }, StringSplitOptions.RemoveEmptyEntries).Skip(1));
                                return String.Format("[{0}] {1}(Union{2}) : {3}", Time.ToString("dd.MM.yy HH:mm:ss"), Name, guildname, txt);
                            }

                        case CHAT_TYPE.NOTICE:
                        case CHAT_TYPE.UNIQUE:
                        case CHAT_TYPE.ERROR:
                            return String.Format("[{0}] {1}", Time.ToString("dd.MM.yy HH:mm:ss"), Text); ;

                        default:
                            return String.Format("[{0}] {1} : {2}", Time.ToString("dd.MM.yy HH:mm:ss"), Name, Text);
                    }
                }
            }
        }

        private ObservableCollection<Message> messages = new ObservableCollection<Message>();
        private object messagesLock = new object();
        
        CollectionView msgView = null;

        private Bot bot;

        public Chat (Bot bot)
        {
            this.bot = bot;

            ChatTypes = new ObservableCollection<String>();
            BindingOperations.EnableCollectionSynchronization(ChatTypes, chatTypesLock);
            BindingOperations.EnableCollectionSynchronization(messages, messagesLock);
            
            AddChatType("NORMAL");
            AddChatType("PARTY");
            AddChatType("GUILD");
            AddChatType("GLOBAL");
            AddChatType("UNION");
            AddChatType("STALL");

            msgView = (CollectionView)CollectionViewSource.GetDefaultView(messages);

            //ShowAll();

            ShowNormal = true;
            ShowPrivate = true;
        }

        public void ShowAll()
        {
            ShowNormal = true;
            ShowPrivate = true;
            ShowParty = true;
            ShowGuild = true;
            ShowGlobal = true;
            ShowNotice = true;
            ShowUnion = true;
            ShowUnique = true;
        }

        public void ShowNone()
        {
            ShowNormal = false;
            ShowPrivate = false;
            ShowParty = false;
            ShowGuild = false;
            ShowGlobal = false;
            ShowNotice = false;
            ShowUnion = false;
            ShowUnique = false;
        }

        private void filterChanged()
        {
            msgView.Filter = (m) =>
            {
                var msg = m as Message;
                if (msg == null) return false;

                return (
                        (ShowNormal && msg.Type == CHAT_TYPE.NORMAL) ||
                        (ShowPrivate && msg.Type == CHAT_TYPE.PRIVATE) ||
                        (ShowParty && msg.Type == CHAT_TYPE.PARTY) ||
                        (ShowGuild && msg.Type == CHAT_TYPE.GUILD) ||
                        (ShowGlobal && msg.Type == CHAT_TYPE.GLOBAL) ||
                        (ShowNotice && msg.Type == CHAT_TYPE.NOTICE) ||
                        (ShowUnion && msg.Type == CHAT_TYPE.UNION) ||
                        (ShowUnique && msg.Type == CHAT_TYPE.UNIQUE) ||
                        (ShowStall && msg.Type == CHAT_TYPE.STALL) ||
                        msg.Type == CHAT_TYPE.ERROR
                    );
            };
        }

        public void HandleOutgoingPacket(Packet packet)
        {
            try
            {
                var type = (CHAT_TYPE)packet.ReadUInt8();
                var unk1 = packet.ReadUInt8();

                switch (type)
                {
                    case CHAT_TYPE.PRIVATE:
                        {
                            var toname = packet.ReadAscii();
                            var txt = packet.ReadAscii();

                            var msg = new Message(type, toname, txt, false);
                            lock (messagesLock)
                            {
                                while (messages.Count > 5000)
                                {
                                    messages.RemoveAt(0);
                                }
                                messages.Add(msg);
                            }
                        }
                        break;
                }

            }
            catch (Exception ex)
            {
                bot.Debug("could not parse outgoing chat! {0} => {1}", ex.Message, ex.StackTrace);
            }
        }

        public void HandleIncomingPacket(Packet packet)
        {
            try
            {
                string name = null;
                string text = null;
                var type = (CHAT_TYPE)packet.ReadUInt8();
                
                switch (type)
                {
                    case CHAT_TYPE.NORMAL:
                        {
                            uint id = packet.ReadUInt32();
                            if (id == bot.Char.CharId)
                            {
                                name = bot.CharName;
                                text = packet.ReadAscii();
                            }
                            else
                            {
                                var player = bot.Spawns.Player.Get(id);
                                name = player?.Name ?? "";

                                text = packet.ReadAscii();
                            }
                        }
                        break;

                    default:
                        if (type != CHAT_TYPE.NOTICE)
                        {
                            name = packet.ReadAscii();
                        }

                        text = packet.ReadAscii();

                        if (type == CHAT_TYPE.NOTICE)
                        {
                            if (text.Contains("Try out our message sys")) return;
                            if (text.Contains("The Hide and Seek Event is over")) return;
                            if (text.Contains(" has won this Hide and Seek")) return;
                            if (text.Contains("Please Note: Selling/Buying")) return;
                            if (text.Contains("Come and search me around")) return;
                            if (text.Contains("Hide and Seek Event round")) return;
                            if (text.Contains("The winner of each round will be ann")) return;
                            if (text.Contains("There will be") && text.Contains("Rounds!")) return;
                            if (text.Contains("I will hide only outside")) return;
                            if (text.Contains("Hide n Seek Event will start in")) return;
                            if (text.Contains("Also dont forget the AUTOM")) return;

                            if (text.Contains("Alchemy Event will start in")) return;
                            if (text.Contains("The Alchemy Event is over now")) return;
                            if (text.Contains("won round") && text.Contains("of the Alchemy Event")) return;
                            if (text.Contains("fused the event item to level")) return;
                            if (text.Contains("However, if someone succ")) return;
                            if (text.Contains("Note: The Event duration is")) return;
                            if (text.Contains("The next round will start in")) return;
                            if (text.Contains("Alchemy Event round") && text.Contains("starts now at")) return;
                            if (text.Contains("This round will be held with")) return;
                            if (text.Contains("How the Event Works")) return;
                            if (text.Contains("Pick as many dropped items as you like")) return;
                            if (text.Contains("The goal is to fuse one item to")) return;
                            if (text.Contains("Whoever fuses it first will receive")) return;
                        }
                        break;
                }

                var msg = new Message(type, name, text);
                lock (messagesLock)
                {
                    while (messages.Count > 5000)
                    {
                        messages.RemoveAt(0);
                    }
                    messages.Add(msg);
                }
            }
            catch (Exception ex)
            {
                bot.Debug("could not parse incoming chat! {0} => {1}", ex.Message, ex.StackTrace);
            }
        }

        public void HandleUniqueMessage(Packet packet)
        {
            var type = packet.ReadUInt8();
            switch (type)
            {
                case 0x05: //UNIQUE SPAWNED
                    {
                        packet.ReadUInt8();
                        var model = packet.ReadUInt32();
                        var unique = MobInfos.GetById(model);
                        if (unique == null) return;

                        lock (messagesLock)
                        {
                            messages.Add(new Message(CHAT_TYPE.UNIQUE, "", String.Format("{0} spawned", unique.Name)));
                        }
                    }
                    break;

                case 0x06: //UNIQUE KILLED
                    {
                        packet.ReadUInt8();
                        var model = packet.ReadUInt32();
                        var unique = MobInfos.GetById(model);
                        if (unique == null) return;

                        var killer = packet.ReadAscii();
                        lock (messagesLock)
                        {
                            messages.Add(new Message(CHAT_TYPE.UNIQUE, "", String.Format("{0} killed by {1}", unique.Name, killer)));
                        }
                    }
                    break;

                default:
                    //bot.Debug("globalmsg: {0}", String.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
                    break;
            }
        }

        public ObservableCollection<Message> GetMessages()
        {
            return messages;
        }

        public void AddChatType(String chatType)
        {
            lock (chatTypesLock)
            {
                if (!ChatTypes.Contains(chatType))
                {
                    ChatTypes.Add(chatType);
                }
            }
        }

        public void SendMessage(String type, String txt)
        {
            if (String.IsNullOrEmpty(type) || String.IsNullOrEmpty(txt)) return;

            switch (type)
            {
                case "NORMAL":
                    {
                        var pm = new Packet(0x7025);
                        pm.WriteUInt8(CHAT_TYPE.NORMAL);
                        pm.WriteUInt8(chatCount);
                        pm.WriteAscii(txt);

                        bot.SendToSilkroadServer(pm);
                    }
                    break;

                case "PARTY":
                    {
                        var pm = new Packet(0x7025);
                        pm.WriteUInt8(CHAT_TYPE.PARTY);
                        pm.WriteUInt8(chatCount);
                        pm.WriteAscii(txt);

                        bot.SendToSilkroadServer(pm);
                    }
                    break;

                case "GUILD":
                    {
                        var pm = new Packet(0x7025);
                        pm.WriteUInt8(CHAT_TYPE.GUILD);
                        pm.WriteUInt8(chatCount);
                        pm.WriteAscii(txt);

                        bot.SendToSilkroadServer(pm);
                    }
                    break;

                case "GLOBAL":
                    {
                        var globalChats = bot.Inventory.GetItems(i => i.Iteminfo.Name.Equals("Global chatting")).OrderBy(i => i.Count).FirstOrDefault();
                        if (globalChats != null)
                        {
                            var p = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYUSE, true);
                            p.WriteUInt8(globalChats.Slot);
                            p.WriteUInt8(0xec);
                            p.WriteUInt8(0x29);
                            p.WriteAscii(txt);

                            bot.SendToSilkroadServer(p);
                        }
                        else
                        {
                            var msg = new Message(CHAT_TYPE.ERROR, "notice", "No Globals found!");
                            lock (messagesLock)
                            {
                                messages.Add(msg);
                            }
                        }
                    }
                    break;

                case "UNION":
                    {
                        var pm = new Packet(0x7025);
                        pm.WriteUInt8(CHAT_TYPE.UNION);
                        pm.WriteUInt8(chatCount);
                        pm.WriteAscii(txt);

                        bot.SendToSilkroadServer(pm);
                    }
                    break;

                case "STALL":
                    {
                        var pm = new Packet(0x7025);
                        pm.WriteUInt8(CHAT_TYPE.STALL);
                        pm.WriteUInt8(chatCount);
                        pm.WriteAscii(txt);

                        bot.SendToSilkroadServer(pm);
                    }
                    break;

                default: // PMs
                    {
                        var pm = new Packet(0x7025);
                        pm.WriteUInt8(CHAT_TYPE.PRIVATE);
                        pm.WriteUInt8(chatCount);
                        pm.WriteAscii(type);
                        pm.WriteAscii(txt);

                        //bot.Debug("chat: send with chat-count: {0}", chatCount);

                        bot.SendToSilkroadServer(pm);
                        
                        var msg = new Message(CHAT_TYPE.PRIVATE, type, txt, false);
                        lock (messagesLock)
                        {
                            messages.Add(msg);
                        }
                    }
                    break;
            }
        }
        
        public void HandleChatCountPacket(Packet packet)
        {
            try
            {
                var succeed = packet.ReadUInt8();
                if (succeed == 1)
                {
                    var type = packet.ReadUInt8();
                    var chatcnt = packet.ReadUInt8();
                }
                else if (succeed == 2)
                {
                    // 02 03 00 02 FB
                    var unk1 = packet.ReadUInt16();
                    var type = packet.ReadUInt8();
                    var chatcnt = packet.ReadUInt8();

                    if (chatcnt > 250)
                    {
                        var msg = new Message(CHAT_TYPE.ERROR, "notice", "Error sending msg!");
                        lock (messagesLock)
                        {
                            messages.Add(msg);
                        }
                    }
                }

                //bot.Debug("chat: received chat-count: {0}", chatcnt);

                // BOT will send everytime with a chatCount > 250. Client will only send 0-250 ..
                //chatCount = (byte)((chatcnt + 1) % 256);
            }
            catch (Exception ex)
            {
                bot.Log("error chat-count: {0} => {1}", ex.Message, ex.StackTrace);
            }
        }

    }
}
