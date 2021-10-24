using SilkroadSecurityApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public class PartyMember : MVVM.ViewModelBase
    {
        public int Hp
        {
            get { return GetValue(() => Hp); }
            set { SetValue(() => Hp, value); }
        }

        public int Mp
        {
            get { return GetValue(() => Hp); }
            set { SetValue(() => Hp, value); }
        }

        public String Name
        {
            get { return GetValue(() => Name); }
            set { SetValue(() => Name, value); }
        }

        public Point Position
        {
            get { return GetValue(() => Position); }
            set { SetValue(() => Position, value); }
        }

        public bool ItsMe
        {
            get { return GetValue(() => ItsMe); }
            set { SetValue(() => ItsMe, value); }
        }

        public uint PartyId = 0;
        public uint PlayerId = 0;
    }

    public enum PARTY_TYPE
    {
        NONE = 0,
        EXPFREE_ITEMFREE = 4,
        EXPSHARE_ITEMFREE,
        EXPFREE_ITEMSHARE,
        EXPSHARE_ITEMSHARE
    }

    public class Party : MVVM.ViewModelBase
    {
        public static String PartyLeader = "";

        private Bot bot;
        private System.Timers.Timer timer;

        public PARTY_TYPE Type;
        public uint MyPartyId = 0;
        public uint MasterId = 0;
        public ObservableCollection<PartyMember> Members { get; set; } = new ObservableCollection<PartyMember>();

        public static Party Create(Bot bot)
        {
            Party p = null;
            App.Current.Dispatcher.Invoke(() => p = new Party(bot));
            return p;
        }

        private Party(Bot bot)
        {
            this.bot = bot;

            if (PartyLeader == "")
            {
                PartyLeader = bot.CharName;
                bot.Log("YEAH, i am the PartyLeader !!");
            }

            Type = bot.Config.Party.Type;

            if (timer != null)
            {
                timer.Stop();
                timer.Close();
                timer.Dispose();
            }

            timer = new System.Timers.Timer();
            timer.Interval = 5000;
            timer.AutoReset = false;
            timer.Elapsed += (s, e) =>
            {
                try
                {
                    partyTimer();
                }
                catch { }

                timer.Start();
            };
            timer.Start();
        }

        private void partyTimer()
        {
            //if (!bot.Loop.IsStarted) return;

            if (IsInAParty())
            {
                if (bot.Config.HalloweenEventSpecial)
                {
                    if (bot.Char.IsAlive)
                    {
                        Leave();
                        return;
                    }
                }

                var master = Members.FirstOrDefault(pm => pm.PartyId == MasterId);
                if (master != null)
                {
                    if (master.Name != PartyLeader)
                    {
                        bot.Log("party leader: {0} != {1} -- leave !", master.Name, PartyLeader);
                        Leave();
                    }
                }
            }

            // invite others
            if (PartyLeader != bot.CharName && !IsInAParty()) return; // dont create party then..

            var playerToInvite = bot.Spawns.Player.GetAll().Select(p => p.Name).Intersect(bot.Config.Party.Members).FirstOrDefault(p => !Members.Any(pm => pm.Name == p));
            if (playerToInvite == null) return;

            var player = bot.Spawns.Player.Get(playerToInvite);
            if (player == null) return;

            Actions.InviteToParty(player.UID, (byte)Type, Members.Count == 0, bot);

            //bot.Debug("invite player: {0} to party!", player.Name);
        }

        public void HandleRequest(Packet packet)
        {
            var playerId = packet.ReadUInt32();
            var partyType = packet.ReadUInt8();

            var player = bot.Spawns.Player.Get(playerId);
            if (player == null)
            {
                bot.Debug("party invite -- dont know the player ?!?!");
                return;
            }

            var acceptParty = bot.Config.Party.AcceptInvite && (byte)bot.Config.Party.Type == partyType && (bot.Config.Party.Members.Contains(player.Name) || PartyLeader == player.Name || !bot.Config.Party.AcceptOnlyFromListedMembers);

            bot.Debug("party invite from: {0}, type: {1} ==> {2}", player.Name, partyType, acceptParty);

            if (DateTime.Now.Subtract(bot.ConnectionTimes.Last(c => c.Type == Bot.ConnectionInfo.CONNECTION_TYPE.CONNECTED).Time) <= new TimeSpan(0, 0, 30))
            {
                bot.Debug("--> do not accept in first 30 seconds..");
                return;
            }

            if (bot.Config.HalloweenEventSpecial)
            {
                if (!bot.Char.IsAlive)
                {
                    Actions.AcceptPartyRequest(bot, acceptParty);
                }
                return;
            }

            Actions.AcceptPartyRequest(bot, acceptParty);
        }

        public void AddMember(PartyMember pMember)
        {
            try
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Members.Add(pMember);
                });
            }
            catch { }
        }

        public void RemoveMember(PartyMember pMember)
        {
            try
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Members.Remove(pMember);
                });
            }
            catch { }
        }

        public void ClearMembers()
        {
            try
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Members.Clear();
                });
            }
            catch { }
        }

        public void HandlePartyAction(Packet packet)
        {
            var partyChangedType = packet.ReadUInt8();

            switch (partyChangedType)
            {
                case 1: // left or get banned..
                    bot.Log("party: i left the party!!");
                    Left();
                    break;

                case 2: // some1 joined
                        // 17:44:34.010 | party changes? => 02, FF, 1D, 01, 00, 00, 04, 00, 42, 79, 74, 65, 78, 07, 00, 00, 28, AA, 87, 5C, 7D, 04, F4, 00, B1, 00, 01, 00, 01, 00, 00, 00, 04, 11, 01, 00, 00, 12, 01, 00, 00 ---> player joined (level 40)
                    {
                        packet.ReadUInt8(); // SPLITTER

                        var partymember = new PartyMember();

                        partymember.PartyId = packet.ReadUInt32();
                        partymember.Name = packet.ReadAscii();

                        var player = bot.Spawns.Player.Get(partymember.Name);
                        if (player != null) partymember.PlayerId = player.UID;

                        var model = packet.ReadUInt32();
                        var level = packet.ReadUInt8();

                        var hpmp = packet.ReadUInt8();
                        partymember.Hp = (hpmp & 0xf) * 100 / 0x0b;
                        partymember.Mp = ((hpmp >> 4) & 0xf) * 100 / 0x0b;

                        // POSITION
                        var xsec = packet.ReadUInt8();
                        var ysec = packet.ReadUInt8();
                        var xcoord = 0;
                        var ycoord = 0;
                        if (ysec == 0x80)
                        {
                            xcoord = packet.ReadUInt16() - packet.ReadUInt16();
                            packet.ReadUInt16();
                            packet.ReadUInt16();
                            ycoord = packet.ReadUInt16() - packet.ReadUInt16();
                        }
                        else
                        {
                            xcoord = packet.ReadUInt16();
                            packet.ReadUInt16();
                            ycoord = packet.ReadUInt16();
                        }

                        partymember.Position = new Point(Movement.CalculatePositionX(xsec, xcoord), Movement.CalculatePositionY(ysec, ycoord));

                        packet.ReadUInt32();

                        var guild = packet.ReadAscii();

                        packet.ReadUInt8();

                        packet.ReadUInt32(); // SKILL TREE
                        packet.ReadUInt32(); // SKILL TREE

                        AddMember(partymember);

                        //bot.Debug("player {0} joined our party..", partymember.Name);
                    }
                    break;

                case 3: // some1 left the party..
                    {
                        // 17:30:22.695 | party changes? => 03, 1D, 01, 00, 00, 01 ---> some1 left the party
                        // 17:37:28.801 | party changes? => 03, F8, 00, 00, 00, 02 ---> some1 left(party leader ? !)
                        var partyId = packet.ReadUInt32();
                        var leftType = packet.ReadUInt8(); // ?!

                        switch (leftType)
                        {
                            case 0x01: // disconnected
                                break;

                            case 0x02:
                                break; // NORMAL LEFT...

                            case 0x04: // kicked by master
                                break;

                            default:
                                break;
                        };

                        if (partyId == MyPartyId)
                        {
                            Left();
                        }
                        else
                        {
                            var partymember = Members.FirstOrDefault(pm => pm.PartyId == partyId);
                            if (partymember != null)
                            {
                                //bot.Debug("player {0} left our party..", partymember.Name);

                                if (PartyLeader == partymember.Name)
                                {
                                    bot.Log("PartyLeader left -> reset it!");
                                    PartyLeader = "";
                                }

                                RemoveMember(partymember);
                            }
                        }
                    }
                    break;

                case 6: // party member update
                    {
                        var partyId = packet.ReadUInt32();
                        var changeType = packet.ReadUInt8();

                        var partymember = Members.FirstOrDefault(pm => pm.PartyId == partyId);
                        if (partymember == null) return;

                        switch (changeType)
                        {
                            case 2: // level up
                                {
                                    var lvl = packet.ReadUInt8();
                                }
                                break;

                            case 4: // hp or mp?
                                {
                                    var hpmp = packet.ReadUInt8();
                                    var hp = (hpmp & 0xf) * 100 / 0x0b;
                                    var mp = ((hpmp >> 4) & 0xf) * 100 / 0x0b;

                                    partymember.Hp = hp;
                                    partymember.Mp = mp;
                                }
                                break;

                            case 8: // mastery changed/update
                                {
                                    var mastery1 = packet.ReadUInt16();
                                    packet.ReadUInt16(); // ?? maybe mastery is 4 bytes long?
                                    var mastery2 = packet.ReadUInt16();
                                    packet.ReadUInt16(); // ?? maybe mastery is 4 bytes long?
                                }
                                break;

                            case 0x20: // moving !?
                                {
                                    var xsec = packet.ReadUInt8();
                                    var ysec = packet.ReadUInt8();
                                    var xcoord = 0;
                                    var ycoord = 0;
                                    if (ysec == 0x80)
                                    {
                                        xcoord = packet.ReadUInt16() - packet.ReadUInt16();
                                        packet.ReadUInt16();
                                        packet.ReadUInt16();
                                        ycoord = packet.ReadUInt16() - packet.ReadUInt16();
                                    }
                                    else
                                    {
                                        xcoord = packet.ReadUInt16();
                                        packet.ReadUInt16();
                                        ycoord = packet.ReadUInt16();
                                    }

                                    int x = Movement.CalculatePositionX(xsec, xcoord);
                                    int y = Movement.CalculatePositionY(ysec, ycoord);

                                    partymember.Position = new Point(x, y);

                                    packet.ReadUInt8(); // ? alive ?
                                    packet.ReadUInt8();
                                    packet.ReadUInt8();
                                    packet.ReadUInt8();
                                }
                                break;

                            default:
                                bot.Debug("party member update? => {0}", String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                                break;
                        }

                    }
                    break;

                case 9: // new party leader
                    {
                        var newLeaderPartyId = packet.ReadUInt32();

                        MasterId = newLeaderPartyId;
                        var partymember = Members.FirstOrDefault(pm => pm.PartyId == MasterId);

                        if (PartyLeader == "" && partymember != null)
                        {
                            PartyLeader = partymember.Name;
                        }

                        bot.Log("party: new party leader.. {0}", PartyLeader);
                    }
                    break;

                default:
                    bot.Debug("party changes? => {0}", String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                    break;
            }
        }

        public void HandlePartyJoined(Packet packet)
        {
            if (packet.ReadUInt8() == 1)
            {
                var myPartyId = packet.ReadUInt32();

                MyPartyId = myPartyId;
            }
        }

        public void HandlePartyInfo(Packet packet)
        {
            //bot.Debug("party info? => {0}", String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));

            ClearMembers();

            packet.ReadUInt8();
            var unk1 = packet.ReadUInt32(); // ?? unique party id ??
            var masterid = packet.ReadUInt32();
            var partytype = packet.ReadUInt8();

            byte count = packet.ReadUInt8();
            for (int i = 0; i < count; i++)
            {
                var partymember = new PartyMember();

                packet.ReadUInt8(); // SPLITTER

                partymember.PartyId = packet.ReadUInt32();
                partymember.Name = packet.ReadAscii();

                var player = bot.Spawns.Player.Get(partymember.Name);
                if (player != null) partymember.PlayerId = player.UID;

                var model = packet.ReadUInt32();
                var level = packet.ReadUInt8();

                var hpmp = packet.ReadUInt8();
                partymember.Hp = (hpmp & 0xf) * 100 / 0x0b;
                partymember.Mp = ((hpmp >> 4) & 0xf) * 100 / 0x0b;

                // POSITION
                var xsec = packet.ReadUInt8();
                var ysec = packet.ReadUInt8();
                var xcoord = 0;
                var ycoord = 0;
                if (ysec == 0x80)
                {
                    xcoord = packet.ReadUInt16() - packet.ReadUInt16();
                    packet.ReadUInt16();
                    packet.ReadUInt16();
                    ycoord = packet.ReadUInt16() - packet.ReadUInt16();
                }
                else
                {
                    xcoord = packet.ReadUInt16();
                    packet.ReadUInt16();
                    ycoord = packet.ReadUInt16();
                }

                int x = Movement.CalculatePositionX(xsec, xcoord);
                int y = Movement.CalculatePositionY(ysec, ycoord);

                partymember.Position = new Point(x, y);

                packet.ReadUInt32();

                var guild = packet.ReadAscii();

                packet.ReadUInt8();

                packet.ReadUInt32(); // SKILL TREE
                packet.ReadUInt32(); // SKILL TREE

                partymember.ItsMe = partymember.PartyId == bot.Char.AccountId;
                AddMember(partymember);
            }

            MasterId = masterid;

            var master = Members.FirstOrDefault(pm => pm.PartyId == MasterId);
            if (master == null) return;

            if (master.Name != PartyLeader)
            {
                bot.Log("party leader: {0} != {1} -- leave !", master.Name, PartyLeader);
                Leave();
            }
        }

        public void Left()
        {
            ClearMembers();
        }

        public bool IsInAParty()
        {
            return Members.Count != 0;
        }

        public void Leave()
        {
            Actions.LeaveParty(bot);
        }
    }
}
