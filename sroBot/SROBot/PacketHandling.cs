using SilkroadSecurityApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Bot
    {
        private Packet charDataPacket;
        private Packet storageDataPacket;
        private Packet guildStorageDataPacket;
        private SpawnParsing spawnparser = new SpawnParsing();

        public void SendToSilkroadServer(Packet packet)
        {
            if (packet == null || Proxy == null) return;

            Proxy.SendToSilkroadServer(packet);
        }

        public void SendToSilkroadClient(Packet packet)
        {
            if (packet == null || Proxy == null) return;

            Proxy.SendToSilkroadClient(packet);
        }

        public int halloweenWords = 7143;
        public bool HandlePacket(String type, Packet packet)
        {
            try
            {
                switch (packet.Opcode)
                {

                    #region clientless

                    case 0x34b5: // accept teleport data ?!
                        if (Clientless)
                        {
                            Packet response = new Packet(0x34b6, true, false);
                            SendToSilkroadServer(response);
                        }
                        break;

                    case 0x2001:
                        if (Clientless)
                        {
                            var readAscii = packet.ReadAscii();
                            if (readAscii == "GatewayServer")
                            {
                                Packet response = new Packet(0x6100, true, false);
                                response.WriteUInt8(Server.LocaleVersion);
                                response.WriteAscii("SR_Client");
                                response.WriteUInt32(Server.ClientVersion);
                                SendToSilkroadServer(response);

                                return true;
                            }
                        }
                        break;

                    case 0xA100:
                        if (Clientless)
                        {
                            byte result = packet.ReadUInt8();
                            if (result == 1)
                            {
                                Packet response = new Packet(0x6106, true);
                                SendToSilkroadServer(response);
                                response = new Packet(0x6101, true);
                                SendToSilkroadServer(response);
                            }
                            else
                            {
                                var errCode = packet.ReadUInt8();
                            }

                            return true;
                        }
                        break;

                    #endregion

                    #region login

                    case 0xb001:
                        charSelected(CharName);
                        break;

                    case 0xB007:
                        handleCharList(packet);
                        break;

                    case 0x2322:
                        handleCaptcha(packet);
                        if (Clientless) return true;
                        break;

                    case 0xa101:
                        handleServerList(packet);
                        break;

                    #endregion

                    #region Character

                    case 0x34a5:
                        charDataPacket = new Packet(0x3013);
                        break;

                    case 0x34a6:
                        charDataPacket.Lock();
                        handleChardata(charDataPacket);
                        break;

                    case 0x3041:
                        Debug("cooldown start? => {0}", String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                        {
                            var player = Spawns.Player.Get(packet.ReadUInt32());
                            if (player != null) Debug("cooldown from player: {0}", player.Name);
                        }
                        break;

                    case 0x3042:
                        Debug("cooldown end? => {0}", String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                        {
                            var player = Spawns.Player.Get(packet.ReadUInt32());
                            if (player != null) Debug("cooldown from player: {0}", player.Name);
                        }
                        break;

                    case 0xB050:
                    case 0xB051:
                        handleStatsUpped(packet);
                        break;

                    case 0x3020:
                        //Console.WriteLine("char spawn, new AccountId: {0} ???", packet.ReadUInt32());
                        if (Clientless)
                        {
                            Packet p = new Packet(0x3012); // confirm spawn
                            SendToSilkroadServer(p);

                            //Debug($"get my consignment list.. {ConnectionTimes.Count} .. {Loop.LoopState} .. {Loop.TrainState}");
                            Actions.ConsignmentGetMyItemList(this);

                            return true;
                        }
                        break;

                    case (ushort)SROData.Opcodes.SERVER.CHARDATA:
                        charDataPacket.WriteUInt8Array(packet.GetBytes());
                        break;

                    case (ushort)SROData.Opcodes.SERVER.CHARACTERINFO:
                        handleCharInfo(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.LVLUP:
                        handleLevelUp(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.SPEEDUPDATE:
                        handleSpeedUpdate(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.STUFFUPDATE:
                        handleStuffUpdate(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.HPMPUPDATE:
                        handleHpMpUpdate(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.EXPSPUPDATE:
                        handleExpSpUpdate(packet);
                        break;

                    #endregion

                    #region pet

                    case (ushort)SROData.Opcodes.SERVER.HORSEACTION:
                        handleHorseAction(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.PETINFO:
                        handlePetInfo(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.PETSTATS:
                        {
                            uint pet_id = packet.ReadUInt32();

                            var pet = Spawns.Pets.Get(pet_id);

                            byte _type = packet.ReadUInt8();
                            switch (_type)
                            {
                                case 0x01: // despawn
                                    //if (pet_id == Char_Data.char_attackpetid)
                                    //{
                                    //    Char_Data.char_attackpetid = 0;
                                    //}
                                    if (Char.Pickpet != null && Char.Pickpet.UID == pet_id)
                                    {
                                        Char.Pickpet.Inventory.Clear();
                                        Char.Pickpet = null;
                                        Debug("PICK PET DISAPPEARED");
                                    }

                                    Spawns.Remove(pet_id);
                                    break;

                                default:
                                    Debug("PET STATS: {0} -> {1}", _type, String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                                    break;
                            }
                        }
                        break;

                    #endregion

                    #region Spawns

                    case (ushort)SROData.Opcodes.SERVER.GROUPSPAWNB:
                    case (ushort)SROData.Opcodes.SERVER.GROUPESPAWN:
                    case (ushort)SROData.Opcodes.SERVER.GROUPSPAWNEND:
                    case (ushort)SROData.Opcodes.SERVER.SINGLESPAWN:
                    case (ushort)SROData.Opcodes.SERVER.SINGLEDESPAWN:
                        spawnparser.HandleSpawnPacket(packet, this);
                        break;

                    #endregion

                    #region Training

                    case 0x30D1:
                        {
                            var uk1 = packet.ReadUInt32();
                            var mobid = packet.ReadUInt32();
                            //if (CurSelected != null && CurSelected.UID == mobid)
                            //    Console.WriteLine("0x30D1: {0}", String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                        }
                        break;

                    case 0x3028:
                        {
                            // 0x3028: 57, 59, 71, 75, E3, 44, DD, 42, 28, 41, 1A, 91, 83, 43, 65, 38, 61, EB, 74, 03
                            var uk = packet.ReadUInt8Array(16);
                            var mobid = packet.ReadUInt32();
                            //if (CurSelected != null && CurSelected.UID == mobid)
                            //    Console.WriteLine("0x3028: {0}", String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                        }
                        break;

                    case (ushort)SROData.Opcodes.SERVER.OBJECTDIE:
                        handleObjectDied(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.OBJECTACTION:
                        if (packet.ReadUInt8() == 3 && packet.ReadUInt8() == 1 && packet.ReadUInt8() == 4)
                        {
                            // TODO: only if we selected a new MOB --> we need to store last mob, too !
                            if (CurSelected != null && CurSelected != LastSelected && Loop.IsAttacking())
                            {
                                //Debug($"this strange 'wating' happened.. just walk to the mob! {CurSelected?.UID} / {LastSelected?.UID} ==> {CurSelected.X};{CurSelected.Y}");

                                Movement.WalkTo(this, CurSelected.X, CurSelected.Y);
                                Movement.WalkTo(this, CurSelected.X, CurSelected.Y);

                                SaveLastMob(CurSelected);
                            }
                        }
                        break;

                    #endregion

                    #region Movement

                    case (ushort)SROData.Opcodes.SERVER.STUCK:
                        //Console.WriteLine("STUCKED?");
                        {
                            var id = packet.ReadUInt32();
                            //if (CurSelected != null && CurSelected.UID == id)
                            //    Console.WriteLine("0xB023 (STUCK): {0}", String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                        }
                        break;

                    case (ushort)SROData.Opcodes.SERVER.MOVE:
                        Movement.Move(packet, this);
                        break;

                    #endregion

                    #region Items

                    case 0x3040: // server item used
                        {
                            var slot = packet.ReadUInt8();
                            var _type = packet.ReadUInt8();
                            switch (_type)
                            {
                                case 8:
                                    {
                                        var count = packet.ReadUInt16();
                                        var item = Inventory.GetItem(slot);
                                        if (SROBot.Inventory.IsItemNotEmpty(item))
                                        {
                                            if (item.Iteminfo.Type.Contains("_HALLOWEEN_") && item.Iteminfo.Type.EndsWith("_H"))
                                            {
                                                ++halloweenWords;
                                                Debug("changed {0} halloween words..", halloweenWords);
                                            }

                                            if (count > 0)
                                            {
                                                item.Count = count;
                                            }
                                            else
                                            {
                                                Inventory.Remove(item);
                                            }
                                        }
                                    }
                                    break;

                                default:
                                    Debug("0x3040: {0}", String.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
                                    break;
                            }
                        }
                        break;

                    case 0x304D:
                        {
                            uint id = packet.ReadUInt32();
                            var item = Spawns.Items.Get(id);
                            if (item != null)
                            {
                                item.Pickable = true;
                            }
                        }
                        break;

                    case (ushort)SROData.Opcodes.SERVER.ITEMFIXED:
                        break;

                    case (ushort)SROData.Opcodes.SERVER.DURABILITYCHANGE:
                        {
                            var slot = packet.ReadUInt8();
                            var new_durability = packet.ReadUInt32();
                            Inventory.ChangeDurability(slot, new_durability);
                        }
                        break;

                    case (ushort)SROData.Opcodes.SERVER.INVENTORYMOVEMENT:
                        Inventory.MovementUpdate(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.INVENTORYUSE:
                        Inventory.ItemUsed(packet);
                        break;

                    case 0x3201:
                        Inventory.ArrowUpdate(packet);
                        break;

                    #endregion

                    #region Skills

                    case (ushort)SROData.Opcodes.SERVER.BUFFINFO:
                        handleBufInfo(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.BUFFDELL:
                        handleDeleteBuf(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.SKILLADD:
                        handleSkillCasted(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.SKILLCASTED:
                        handleSkillCasted(packet, true);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.SKILLUPDATE:
                        if (packet.ReadUInt8() == 0x01)
                        {
                            uint new_skill_id = packet.ReadUInt32();
                            var newSkill = SkillInfos.GetByModel(new_skill_id);
                            AddSkill(newSkill);
                            Loop.CheckMastery(true, Loop.SkillingType.Skill);
                        }
                        break;

                    case (ushort)SROData.Opcodes.SERVER.MASTERYUPDATE:
                        if (packet.ReadUInt8() == 0x01)
                        {
                            var masteryId = packet.ReadUInt32();
                            var masteryLevel = packet.ReadUInt8();
                            Char.Masteries.Update(masteryId, masteryLevel);
                            Loop.CheckMastery(true, Loop.SkillingType.Mastery);
                        }
                        else
                        {
                            Console.WriteLine("cannot learn due to mastery limit..");
                            Loop.CannotLearnDueToMasteryLimit(true);
                        }
                        break;

                    #endregion

                    #region exchange

                    case 0x3086: // accepted step 2
                        Exchange.AcceptedStep1(packet);
                        break;

                    case 0xb082: // accepted step 2
                        Exchange.AcceptedStep2(packet);
                        break;

                    case 0xb083: // accept step 2 ??
                    case 0xb081: // ??
                    case 0xb084: // close?
                        Log("0x{0:X4}: {1}", packet.Opcode, String.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
                        break;

                    case (ushort)SROData.Opcodes.SERVER.EXCHANGESTARTED:
                        Exchange.Started(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.EXCHANGEGOLDCHANGED:
                        Exchange.GoldChanged(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.EXCHANGEITEMSGAINED:
                        Exchange.ItemsGained(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.EXCHANGEDONE:
                        Exchange.Done(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.EXCHANGECANCELED:
                        Exchange.Canceled(packet);
                        break;

                    #endregion

                    #region party

#if false
SERVER_PARTY_REQUEST			= 0x3080
SERVER_PARTY_JOIN_FORMED		= 0x706D
SERVER_PARTY_CHANGENAME			= 0xB06A
SERVER_PARTY_MEMBER			= 0xB067
SERVER_PARTY_OWNER			= 0xB060
#endif

                    case (ushort)SROData.Opcodes.SERVER.PARTYINVITATION:
                        {
                            byte requesttype = packet.ReadUInt8();

                            switch (requesttype)
                            {
                                case 0x01: // exchange request
                                    Exchange.Request(packet);
                                    break;

                                case 0x02:
                                case 0x03:
                                    {
                                        // we are not in any party, otherwise there couldnt be this request .. ?!

                                        Party.HandleRequest(packet);
                                    }
                                    break;

                                case 0x04:
                                case 0x08:
                                    {
                                        Debug("party invite type 4/8?!");

                                        //Main.cPt.AcceptRequest(true);

                                        //Main.Buffs.Clear();

                                        //if (Main.botStarted)
                                        //{
                                        //    System.Threading.Thread.Sleep(4000);
                                        //    Main.guiEvents.doUpdateGui(null, UpdateType.StartShopping);
                                        //}
                                    }
                                    break;
                            };
                        }
                        break;

                    case 0xB067:
                        Party.HandlePartyJoined(packet);
                        break;

                    case 0x3065:
                        Party.HandlePartyInfo(packet);
                        break;

                    case 0x3864:
                        Party.HandlePartyAction(packet);
                        break;


                    #endregion

                    #region alchemy

                    case 0xB151:
                    case (ushort)SROData.Opcodes.SERVER.ALCHEMYRESULT:
                        Alchemy.HandleAlchemyResult(packet);
                        break;

                    #endregion

                    case (ushort)SROData.Opcodes.SERVER.NPCSELECT:
                        {
                            var success = packet.ReadUInt8() == 1;

                            Loop.StorageOpened(success);
                            Loop.ConsignmentOpened(success);
                            Loop.NpcOpened(success);

                            return Loop.IsBuying() || Loop.IsSelling() || Loop.IsStoring() || Loop.IsUsingConsignment();
                        }

                    case (ushort)SROData.Opcodes.SERVER.NPCDESELECT:
                        {
                            var success = packet.ReadUInt8() == 1;

                            Loop.NpcDeselected(success);

                            return Loop.IsBuying() || Loop.IsSelling() || Loop.IsStoring() || Loop.IsUsingConsignment();
                        }

                    case (ushort)SROData.Opcodes.SERVER.OBJECTSELECT:
                        {
                            var success = packet.ReadUInt8() == 1;

                            Loop.NpcSelected(success);

                            return Loop.IsBuying() || Loop.IsSelling() || Loop.IsStoring() || Loop.IsUsingConsignment();
                        }

                    #region storage

                    case 0xB03C:
                        {
                            var failed = packet.ReadUInt8() == 2;

                            // SOMETHING GONE WRONG??
                            if (failed)
                            {
                                Loop.GotStorageList(false);
                            }

                            if (Loop.IsStoring()) return true;
                        }
                        break;

                    case (ushort)SROData.Opcodes.SERVER.STORAGEGOLD:
                        storageDataPacket = new Packet((ushort)SROData.Opcodes.SERVER.STORAGEITEMS);
                        //if (Loop.IsStoring()) return true; --> client needs to know, cause of inv movements..
                        break;

                    case (ushort)SROData.Opcodes.SERVER.STORAGEITEMS:
                        storageDataPacket.WriteUInt8Array(packet.GetBytes());
                        //if (Loop.IsStoring()) return true; --> client needs to know, cause of inv movements..
                        break;

                    case (ushort)SROData.Opcodes.SERVER.STORAGEOK:
                        storageDataPacket.Lock();
                        {
                            var storageMax = storageDataPacket.ReadUInt8();
                            var storageItems = storageDataPacket.ReadUInt8();

                            Storage.Size = storageMax;
                            Storage.Clear();

                            for (byte i = 0; i < storageItems; ++i)
                            {
                                var slot = storageDataPacket.ReadUInt8();
                                var item = Inventory.ParseItem(storageDataPacket, this, slot);
                                if (item != null)
                                {
                                    Storage.Add(item);
                                }
                            }

                            Loop.GotStorageList(true);

                            //if (Loop.IsStoring()) return true; --> client needs to know, cause of inv movements..
                        }
                        break;

                    #endregion storage

                    #region guild storage

                    case (ushort)SROData.Opcodes.SERVER.GUILDSTORAGEGOLD:
                        guildStorageDataPacket = new Packet((ushort)SROData.Opcodes.SERVER.GUILDSTORAGEITEMS);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.GUILDSTORAGEITEMS:
                        guildStorageDataPacket.WriteUInt8Array(packet.GetBytes());
                        break;

                    case (ushort)SROData.Opcodes.SERVER.GUILDSTORAGEOK:
                        guildStorageDataPacket.Lock();
                        {
                            var storageMax = guildStorageDataPacket.ReadUInt8();
                            var storageItems = guildStorageDataPacket.ReadUInt8();

                            GuildStorage.Size = storageMax;
                            GuildStorage.Clear();

                            for (byte i = 0; i < storageItems; ++i)
                            {
                                var slot = guildStorageDataPacket.ReadUInt8();
                                var item = Inventory.ParseItem(guildStorageDataPacket, this, slot);
                                if (item != null)
                                {
                                    //Debug("guild storage: slot: {0}: {1} => {2}", item.Slot, item.Iteminfo.Type, item.Count);
                                    GuildStorage.Add(item);
                                }
                            }
                        }
                        break;

                    #endregion guild storage

                    case 0x3153:
                        {
                            var silks = packet.ReadUInt32();
                            Char.Silk = silks;
                        }
                        break;

                    #region chat

                    case (ushort)SROData.Opcodes.SERVER.CHAT:
                        Chat.HandleIncomingPacket(packet);
                        break;

                    case (ushort)SROData.Opcodes.SERVER.CHATCOUNT:
                        Chat.HandleChatCountPacket(packet);
                        break;

                    case 0x300c:
                        Chat.HandleUniqueMessage(packet);
                        if (!Clientless) return true; // dont send to client ..
                        break;

                    #endregion chat

                    #region stall

                    case 0x30b7:
                        {
                            byte stallUpdateCmd = packet.ReadUInt8();

                            switch (stallUpdateCmd)
                            {
                                case 2: // player joined
                                    {
                                        var playerId = packet.ReadUInt32();
                                        var player = Spawns.Player.Get(playerId);

                                        Log($"{player?.Name ?? "--UNKNOWN--"} has joined the stall..");
                                    }
                                    break;

                                case 1: // player left
                                    {
                                        var playerId = packet.ReadUInt32();
                                        var player = Spawns.Player.Get(playerId);

                                        Log($"{player?.Name ?? "--UNKNOWN--"} has left the stall..");
                                    }
                                    break;

                                case 0x03: // sold
                                case 0x04: // sold
                                    {
                                        var itemSlot = packet.ReadUInt8();
                                        var playerName = packet.ReadAscii();

                                        Stall.ItemSold(itemSlot, playerName);
                                    }
                                    break;

                            }
                        }
                        break;

                    #endregion stall

                    #region consignment

                    case 0x350d:
                        //Debug("consignment update?: {0}", string.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
                        // 0x01, 0x45, 0x01, 0x00, 0x00, 0x9E, 0x98, 0x00, 0x00, 0x01, 0x33, 0x6A, 0x9D, 0x58
                        handleConsignmentUpdate(packet);
                        break;

                    case 0xB507: // closed
                        return handleConsignmentClosed(packet);

                    case 0xB50B: // settled ..
                        return handleConsignmentSettled(packet);

                    case 0xB508: // item registered
                        return handleConsignmentItemRegistered(packet);

                    case 0xB509: // item aborted ..
                        return handleConsignmentItemAborted(packet);

                    case 0xB50E:
                        handleConsignmentInfo(packet);
                        break;

                    case 0xB50C:
                        return handleConsignmentSearch(packet);

                    case 0xb50a: // bought
                        Debug("consignment item bought: {0}", string.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
                        break;

                        #endregion
                }
            }
            catch (Exception ex)
            {
                Debug("handlePacket(): {0} => {1}: {2}", ex.Message, ex.StackTrace, string.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
            }

            return false;
        }

        private void createChar()
        {
            //CHAR_CH_MAN
            //CHAR_CH_WOMAN
            //CHAR_EU_MAN
            //CHAR_EU_WOMAN

            var charModels = MobInfos.MobList.Where(m => m.Type.StartsWith("CHAR_CH_MAN"));
            var targetChar = charModels.First();

            var gender = targetChar.Type.Contains("WOMAN") ? "_W_" : "_M_";
            var race = targetChar.Type.Contains("_CH_") ? "_CH_" : "_EU_";

            var possibleItems = ItemInfos.ItemList.Where(i => i.Type.EndsWith("_DEF") && i.Type.Contains(race));

            // _BA_ => CHEST
            // _LA_ => LEGS
            // _FA_ => Foots

            var chestItem = possibleItems.FirstOrDefault(i => i.Type.Contains(gender) && i.Type.Contains("_LIGHT_") && i.Type.Contains("_BA_"));
            var legsItem = possibleItems.FirstOrDefault(i => i.Type.Contains(gender) && i.Type.Contains("_LIGHT_") && i.Type.Contains("_LA_"));
            var footItem = possibleItems.FirstOrDefault(i => i.Type.Contains(gender) && i.Type.Contains("_LIGHT_") && i.Type.Contains("_FA_"));
            var weapItem = possibleItems.FirstOrDefault(i => i.Type.Contains("_BOW_"));

            Actions.CreateCharacter(this, createCharName, targetChar.Model, 0, 0, chestItem.Model, legsItem.Model, footItem.Model, weapItem.Model);
        }

        private string createCharName = "";
        private void handleCharList(Packet packet)
        {
            var _type = packet.ReadUInt8();
            if (_type == 1) // create character
            {
                if (packet.ReadUInt8() == 1 && Clientless) // result
                {
                    var listChars = new Packet(0x7007);
                    listChars.WriteInt8(2); // list chars
                    SendToSilkroadServer(listChars);
                }
                else
                {
                    Log($"could not create char.. 0x{packet.ReadUInt16():X2}");

                    Thread.Sleep(1500);

                    createCharName = "_" + createCharName + "_";
                    createChar();
                }
            }
            else if (_type == 2) // character listening
            {
                if (packet.ReadUInt8() == 1) // result
                {
                    var chars = new List<String>();
                    byte charCount = packet.ReadUInt8();
                    for (int i = 0; i < charCount; i++)
                    {
                        uint CharID = packet.ReadUInt32();
                        string CharName = packet.ReadAscii();
                        packet.ReadUInt8();
                        packet.ReadUInt8();
                        packet.ReadUInt64();
                        packet.ReadUInt16();
                        packet.ReadUInt16();
                        packet.ReadUInt16();
                        packet.ReadUInt32();
                        packet.ReadUInt32();

                        byte doDelete = packet.ReadUInt8();
                        if (doDelete == 1)
                            packet.ReadUInt32();

                        packet.ReadUInt16();
                        packet.ReadUInt8();
                        byte itemCount = packet.ReadUInt8();

                        for (int y = 0; y < itemCount; y++)
                        {
                            UInt32 item_id = packet.ReadUInt32();
                            byte item_plus = packet.ReadUInt8();
                        }

                        byte Avatars_count = packet.ReadUInt8();
                        for (int y = 0; y < Avatars_count; y++)
                        {
                            UInt32 item_id = packet.ReadUInt32();
                            byte item_plus = packet.ReadUInt8();
                        }

                        chars.Add(CharName);
                    }

                    if (charCount == 0)
                    {
                        Log("NO CHAR FOUND !! --> create one ..");

                        createCharName = CharName;
                        createChar();
                    }
                    else
                    {
                        Log("chars: " + string.Join(", ", chars));

                        if (chars.Contains(CharName))
                        {
                            Log("select char: {0}", CharName);

                            var p = new Packet(0x7001);
                            p.WriteAscii(CharName);
                            SendToSilkroadServer(p);
                        }
                        else
                        {
                            Log("char \"{0}\" NOT found", CharName);
                        }
                    }
                }
            }
        }

        private void handleCaptcha(Packet packet)
        {
            Debug("GATEWAY: getting captcha ..");
            //UInt32[] pixels = Captcha.GeneratePacketCaptcha(packet);
            //Random rnd = new Random();
            //Captcha.SaveCaptchaToBMP(pixels, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + rnd.Next(000000,999999) + ".bmp");

            var p = new Packet(0x6323);
            if (Server.Captcha == "resolve")
            {
                Debug("we need to resolve it !!!!");
            }
            else
            {
                p.WriteAscii(Server.Captcha);
                SendToSilkroadServer(p);
            }
        }

        public static byte[] GenerateHwId(string guid)
        {
            var crypt = new[]
            {
            new[] {0x61, 0x73, 0x7b},
            new[] {0x60, 0x72, 0x7a},
            new[] {0x63, 0x71, 0x79},
            new[] {0x62, 0x70, 0x78},
            new[] {0x65, 0x77, 0x7f},
            new[] {0x64, 0x76, 0x7e},
            new[] {0x67, 0x75, 0x7d},
            new[] {0x66, 0x74, 0x7c},
            new[] {0x69, 0x7b, 0x73},
            new[] {0x68, 0x7a, 0x72},
            new[] {0x30, 0x22, 0x2a},
            new[] {0x33, 0x21, 0x29},
            new[] {0x32, 0x20, 0x28},
            new[] {0x35, 0x27, 0x2f},
            new[] {0x34, 0x26, 0x2e},
            new[] {0x37, 0x25, 0x2d},
            new[] {0x7c, 0x6e, 0x66}
         };

            var hwid = new byte[44];
            var cryptIdx = 2;
            var idx = 0;

            hwid[idx++] = 0;
            hwid[idx++] = 0x24;

            foreach (var val in guid.Select(c => c == '-' ? 16 : Convert.ToUInt16(c + "", 16)))
            {
                var b = (byte)crypt[val][cryptIdx--];
                if (b == 0)
                {
                    Console.WriteLine($"ERROR! {val}, {cryptIdx + 1} ==> {guid}");
                }

                hwid[idx++] = b;
                if (cryptIdx < 0) cryptIdx = 2;
            }

            idx -= 1; // last char from GUID will be 0 - everytime!
            for (var cnt = 0; cnt < 7; ++cnt)
            {
                hwid[idx++] = 0;
            }

            //Console.WriteLine($"{guid} -> {string.Join(" ", hwid.Select(b => b.ToString("X2")))}");

            return hwid;
        }

        private void handleServerList(Packet packet)
        {
            //  1   byte    GlobalOperationFlag   [0x00 = done, 0x01 = NextGlobalOperation]
            //  while(OperationFlag == 0x01)
            //  {
            //      1   byte    GlobalOperation.Type*
            //      2   ushort  GlobalOperation.Name.Length
            //      *   string  GlobalOperation.Name
            //      
            //      1   byte    OperationFlag [0x00 = done, 0x01 = NextOperation]
            //  }

            var globalOperatingFlag = packet.ReadInt8();
            while (globalOperatingFlag == 1)
            {
                var _type = packet.ReadInt8();
                var name = packet.ReadAscii();

                globalOperatingFlag = packet.ReadInt8();
            }

            //  1   byte    ShardFlag   [0x00 = done, 0x01 = NextShard]
            //  while(ShardFlag == 0x01)
            //  {
            //      2   ushort  Shard.ID
            //      2   ushort  Shard.Name.Length
            //      *   string  Shard.Name
            //      2   ushort  Shard.Current
            //      2   ushort  Shard.Capacity
            //      1   byte    Status  [0x00 = Online, 0x01 = Checked] // anders rum ?!?! !!
            //      1   byte    GlobalOperationID
            //
            //      1   byte    ShardFlag   [0x00 = done, 0x01 = NextShard]
            //  }
            var shardFlag = packet.ReadInt8();
            //while (shardFlag == 1)
            if (shardFlag == 1)
            {
                var id = packet.ReadUInt16();
                var shardName = packet.ReadAscii();
                var curUser = packet.ReadUInt16();
                var maxUser = packet.ReadUInt16();
                var status = packet.ReadUInt8();
                var globalOperationId = packet.ReadInt8();

                Debug("[{0}] => {1} with {2}/{3} -- {4}({5})", "", shardName, curUser, maxUser, status == 1 ? "online" : "check", status);

                if (Proxy.AutomaticLogin)
                {
                    if (curUser + 1 < maxUser && status == 1)
                    {
                        Debug("free slots: {0} ==> connect", maxUser - curUser);

                        {
                            Packet response = new Packet(0x9001);

#if false
                            var hwid = new byte[44]
                            //{ 0x00, 0x24, 0x79, 0x72, 0x62, 0x2e, 0x20, 0x64, 0x78, 0x74, 0x7c, 0x7e, 0x72, 0x60, 0x7a, 0x6e, 0x65, 0x72, 0x21, 0x68, 0x66, 0x7b, 0x34, 0x7a, 0x73, 0x7c, 0x7a, 0x20, 0x63, 0x7a, 0x70, 0x66, 0x7f, 0x74, 0x60, 0x7a, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }; // GEHT !
                            // 00 24 7E 7A 67 2E 22 63 7B 21 7C 28 26 34 7A 6E 65 7B 72 66 66 22 66 2F 26 7C 2D 70 33 2E 7A 61 7D 21 61 7C 26 00 00 00 00 00 00 00 // GEHT
                              { 0x00, 0x24, 0x79, 0x72, 0x62, 0x2e, 0x20, 0x64, 0x78, 0x74, 0x7c, 0x7e, 0x72, 0x60, 0x7a, 0x6e, 0x65, 0x72, 0x21, 0x68, 0x66, 0x7b, 0x34, 0x7a, 0x73, 0x7c, 0x7a, 0x20, 0x63, 0x7a, 0x70, 0x66, 0x7f, 0x74, 0x60, 0x7a, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
#elif false
                            var hwid = new byte[44];
                            Global.Random.NextBytes(hwid);
                            hwid[0] = 0;
                            hwid[1] = 0x24;

                            hwid[37] = 0;
                            hwid[38] = 0;
                            hwid[39] = 0;
                            hwid[40] = 0;
                            hwid[41] = 0;
                            hwid[42] = 0;
                            hwid[43] = 0;
#else
                            var hwid = GenerateHwId(this.Config.GUID);
                            this.Debug($"Use GUID: {this.Config.GUID} -> {string.Join(" ", hwid.Select(b => b.ToString("X2")))}");
#endif
                            response.WriteUInt8Array(hwid);
                            SendToSilkroadServer(response);
                        }

                        var p = new Packet(0x6102, true);
                        p.WriteUInt8(22);
                        p.WriteAscii(Config.AccountName.ToLower());
                        p.WriteAscii(Config.AccountPass);
                        p.WriteUInt16(64);
                        SendToSilkroadServer(p);
                    }
                    else
                    {
                        Thread.Sleep(1500);
                        var p = new Packet(0x6101, true);
                        SendToSilkroadServer(p);
                    }
                }

                shardFlag = packet.ReadInt8();
            }
        }

        private void handleStatsUpped(Packet packet)
        {
            if (Char.RemainStatPoints > 0)
                --Char.RemainStatPoints;

            Loop.CheckStats(true);
        }

        public void handleChardata(Packet packet)
        {
            if (packet == null) return;

            Char.IsParsed = false;
            Char.BadStatus = false;

            try
            {
                Spawns.Clear();
                ConsignmentItems.Clear();

                packet.ReadUInt32();
                Char.Model = packet.ReadUInt32(); //Model
                packet.ReadUInt8(); //Volume and Height
                Char.Level = packet.ReadUInt8();
                Char.MaxLevel = packet.ReadUInt8();

                Char.EXP = packet.ReadUInt64();
                var spexp = packet.ReadUInt32(); //SP bar
                Char.Gold = packet.ReadUInt64(); // gold
                Char.SP = packet.ReadUInt32();
                Char.RemainStatPoints = packet.ReadUInt16(); // remain stat points
                Char.Zerk = packet.ReadUInt8();//zerk
                Char.ZerkInUse = false;
                Char.EXP += packet.ReadUInt32(); // exp ?!
                Char.CurHP = packet.ReadUInt32();
                Char.CurMP = packet.ReadUInt32();
                packet.ReadUInt8(); //AutoInverstExp(1 = Beginner Icon, 2 = Helpful, 3 = Beginner&Helpful)
                packet.ReadUInt8(); //DailyPK
                packet.ReadUInt16(); //	2	ushort	TotalPK
                packet.ReadUInt32(); //	4	uint	PKPenaltyPoint
                packet.ReadUInt8(); //	1	byte	HwanLevel
                packet.ReadUInt8(); //	1	byte	*unk01 -> Check for != 0

                #region Items

                Inventory.Size = packet.ReadUInt8(); //	1	byte	Inventory.Size
                var itemscount = packet.ReadUInt8();

                Inventory.DoNotRefreshViews = true;

                Inventory.Clear();

                for (int y = 0; y < itemscount; y++)
                {
                    var slot = packet.ReadUInt8();

                    var item = SROBot.Inventory.ParseItem(packet, this, slot);
                    if (item != null)
                    {
                        Inventory.Add(item);
                    }
                    else
                    {
                        Debug("NULL\r\n");
                    }
                }

                Inventory.DoNotRefreshViews = false;
                Inventory.RefreshInventoryViews();

                #endregion

                #region Avatars

                var avatarMax = packet.ReadUInt8(); // Avatars Max
                int avatarcount = packet.ReadUInt8();
                for (int i = 0; i < avatarcount; i++)
                {
                    packet.ReadUInt8(); //Slot
                    packet.ReadUInt32(); // rent
                    var avatar_id = packet.ReadUInt32();

                    var item = ItemInfos.GetById(avatar_id);
                    //string type = item.Type;
                    var item_plus = packet.ReadUInt8();
                    packet.ReadUInt64();
                    var dura = packet.ReadUInt32();
                    //Char_Data.inventorydurability.Add(dura);
                    var blueamm = packet.ReadUInt8();
                    for (int j = 0; j < blueamm; j++)
                    {
                        packet.ReadUInt32();
                        packet.ReadUInt32();
                    }
                    packet.ReadUInt8(); //OptType (1 => Socket)
                    var optCnt = packet.ReadUInt8(); //OptCount
                                                     //			ForEach(Option)
                                                     //			{
                                                     //				1	byte	Option.Slot
                                                     //				4	uint	Option.ID
                                                     //				4	uint	Option.nParam1 (=> Reference to Socket)
                                                     //			}
                    for (int j = 0; j < optCnt; ++j)
                    {
                        packet.ReadUInt8();
                        packet.ReadUInt32();
                        packet.ReadUInt32();
                    }

                    packet.ReadUInt8(); //OptType (2 => Advanced elixir)
                    optCnt = packet.ReadUInt8(); //OptCount
                                                 //			ForEach(Option)
                                                 //			{
                                                 //				1	byte	Option.Slot
                                                 //				4	uint	Option.ID
                                                 //				4	uint	Option.OptValue (=> "Advanced elixir in effect [+OptValue]")
                                                 //			}
                    for (int j = 0; j < optCnt; ++j)
                    {
                        packet.ReadUInt8();
                        packet.ReadUInt32();
                        packet.ReadUInt32();
                    }
                }
                #endregion

                var uk1 = packet.ReadUInt8(); //Avatars End != 0 ?

                int mastery = packet.ReadUInt8(); // Mastery Start
                while (mastery == 1)
                {
                    var masteryId = packet.ReadUInt32(); // Mastery ID
                    var masteryLvl = packet.ReadUInt8();  // Mastery LV

                    Char.Masteries.Update(masteryId, masteryLvl);
                    //Log("Mastery: {0} has level: {1}", Mastery.GetName(masteryId), masteryLvl);

                    mastery = packet.ReadUInt8(); // New Mastery Start / List End
                }
                var uk2 = packet.ReadUInt8(); //mastery End != 0 ?

                ClearSkills();

                int skilllist = packet.ReadUInt8(); // Skill List Start
                while (skilllist == 1)
                {
                    uint skillid = packet.ReadUInt32(); // Skill ID
                    var tmp = packet.ReadUInt8();
                    skilllist = packet.ReadUInt8(); // New Skill Start / List End

                    var skill = SkillInfos.GetByModel(skillid);

                    if (skill == null)
                    {
                        Log("unknown skill: {0}", skillid);
                    }

                    AddSkill(skill);
                }

                #region Skipping Quest Part
#if false
			sbyte[] tempe = new sbyte[4];
			while (true)
			{
				tempe[0] = tempe[1];
				tempe[1] = tempe[2];
				tempe[2] = tempe[3];
				tempe[3] = packet.ReadUInt8();
				if ((tempe[0] == skip_charid[0]) && (tempe[1] == skip_charid[1]) && (tempe[2] == skip_charid[2]) && (tempe[3] == skip_charid[3]))
				{
					Console.Beep();
					packet.SeekRead(4, System.IO.SeekOrigin.Current);//packet.data.pointer -= 4;
					break;
				}
			}
#else
                var questCnt = packet.ReadUInt16();
                //	foreach(CompletedQuet)
                //	{
                //		4	uint	RefQuestID
                //	}
                for (int i = 0; i < questCnt; ++i)
                {
                    packet.ReadUInt32();
                }
                var activeQuestcnt = packet.ReadUInt8();

                //	foreach(ActiveQuest)
                //	{
                //		4	uint	Quest.ID
                //		1	byte	Quest.AchievementCount (Repetition Amount = Bit && Completetion Amount = Bit)
                //		1	byte	Quest.*unk04 -> Check for != 0
                //		1	byte	Quest.Type (8 = , 24 = , 88 = )
                //		1	byte	Quest.Status (1 = Untouched, 7 = Started, 8 = Complete)
                //		1	byte	Quest.ObjectiveCount
                //		foreach(Objective)
                //		{
                //			1	byte	Objective.ID
                //			1	byte	Objective.Status (00 = done, 01 = incomplete)
                //			2	ushort	Objective.Name.Length
                //			*	string	Objective.Name
                //			1	byte	Objective.TaskCount
                //			foreach(ObjectiveTask)
                //			{
                //				4	uint	Task.Value (=> Killed monsters; Collected items)
                //			}
                //		}
                //		if(Quest.Type == 88)
                //		{
                //			1	byte	Quest.TaskCount
                //			foreach(QuestTask)
                //			{
                //				4	uint	RefObjID (=> NPCs to deliver to, when complete you get reward)
                //			}
                //		}
                //	}

                for (int i = 0; i < activeQuestcnt; ++i)
                {
                    packet.ReadUInt32();
                    packet.ReadUInt8();
                    packet.ReadUInt8();
                    var questType = packet.ReadUInt8();
                    packet.ReadUInt8();
                    var questObjectiveCnt = packet.ReadUInt8();
                    for (int j = 0; j < questObjectiveCnt; ++j)
                    {
                        packet.ReadUInt8();
                        packet.ReadUInt8();
                        packet.ReadAscii();
                        var questTaskCnt = packet.ReadUInt8();
                        for (int k = 0; k < questTaskCnt; ++k)
                        {
                            packet.ReadUInt32();
                        }
                    }
                    if (questType == 88)
                    {
                        var questTaskCnt = packet.ReadUInt8();
                        for (int k = 0; k < questTaskCnt; ++k)
                        {
                            packet.ReadUInt32();
                        }
                    }
                }

#endif
                #endregion

                //	1	byte	*unk05 -> Check for != 0
                packet.ReadUInt8();
                //	4	ushort	*unk06 -> Check for != 0
                packet.ReadUInt32();

                Char.CharId = packet.ReadUInt32(); // unique id

                var XSector = packet.ReadUInt8(); // war 16
                var YSector = packet.ReadUInt8(); // war 16

                float xcoord = packet.ReadSingle();
                float zcoord = packet.ReadSingle();
                float ycoord = packet.ReadSingle();
                packet.ReadUInt16(); // POS - war sonst nicht hier, von item spawn abgeguckt..

                Char.CurPosition.X = Movement.CalculatePositionX(XSector, xcoord);
                Char.CurPosition.Y = Movement.CalculatePositionY(YSector, ycoord);

                Log("spawned @ {0}/{1}", Char.CurPosition.X, Char.CurPosition.Y);

                //	1	byte	DestinationFlag
                //	1	byte	MovementType(0 = Walking, 1 = Running)
                //	if(DestinationFlag)
                //	{
                //		1	byte	DestXSec
                //		1	byte	DestYSec
                //		2	ushort	DestX
                //		2	ushort	DestZ
                //		2	ushort	DestY
                //	}
                //	else
                //	{
                //		1	byte	SourceFlag (1 = Sky-/ArrowKey-walking)
                //		2	ushort	Angle
                //	}

                var destinationFlag = packet.ReadUInt8();
                packet.ReadUInt8();
                if (destinationFlag != 0)
                {
                    packet.ReadUInt8();
                    packet.ReadUInt8();
                    packet.ReadUInt16();
                    packet.ReadUInt16();
                    packet.ReadUInt16();
                }
                else
                {
                    packet.ReadUInt8();
                    packet.ReadUInt16();
                }

                //	1	byte	StateFlag(1 = Alive, 2 = Dead)
                //	1	byte	*unk07 -> Check for != 0
                //	1	byte	Action (0 = None, 2 = Walking, 3 = Running, 4 = Sitting)
                //	1	byte	Status(0 = None,2 = ??*@GrowthPet*, 3 = Invincible, 4 = Invisible)
                //	4	float	WalkSpeed
                //	4	float	RunSpeed
                //	4	float	HwanSpeed
                //	1	byte	ActiveBuffCount
                //	foreach(ActiveBuff)
                //	{
                //		RefSkillID
                //		TimedJobID
                //		if(RefSkill.Param2 is 1701213281 -> atfe -> "auto transfer effect" like Recovery Division)
                //		{
                //			1	byte	Creator
                //		}
                //	}
                //

                packet.ReadUInt8();
                packet.ReadUInt8();
                packet.ReadUInt8();
                packet.ReadUInt8();

                var walkingSpeed = packet.ReadSingle();
                Char.Speed = packet.ReadSingle();
                var BerserkerSpeed = packet.ReadSingle();

                var activeBuffCnt = packet.ReadUInt8();
                ClearActiveBuffs();
                for (int i = 0; i < activeBuffCnt; ++i)
                {
                    var refskillid = packet.ReadUInt32(); // RefSkillID
                    var timedjobid = packet.ReadUInt32(); // TimedJobID
                    Debug("buf refskillid: {0} - timedjobid: {1}", refskillid, timedjobid);

                    //		if(RefSkill.Param2 is 1701213281 -> atfe -> "auto transfer effect" like Recovery Division)
                    //		{
                    //			1	byte	Creator
                    //		}

                    var activeBuff = SkillInfos.GetByModel(refskillid);
                    var type = activeBuff.Type ?? "";
                    if (type.StartsWith("SKILL_EU_CLERIC_RECOVERYA_GROUP") || type.StartsWith("SKILL_EU_BARD_BATTLAA_GUARD") || type.StartsWith("SKILL_EU_BARD_DANCEA") || type.StartsWith("SKILL_EU_BARD_SPEEDUPA_HITRATE"))
                    {
                        var caster = packet.ReadUInt8();
                        //Console.WriteLine("read an additional byte?! => {0}", caster);
                        switch (caster)
                        {
                            case 1: // caster
                                break;
                            case 2: // not the caster
                                break;
                        }
                    }

                    activeBuff = activeBuff.Copy();
                    activeBuff.IngameId = timedjobid;
                    AddActiveBuff(activeBuff);
                }

                //	2	ushort	Name.Length
                //	*	string	Name
                //	2	ushort	JobName.Length
                //	*	string	JobName
                //	1	byte	JobType (0 = None, 1 = Trader, 2 = Tief, 3 = Hunter)	
                //	1	byte	JobLevel
                //	4	uint	JobExp
                //	4	uint	JobContribution
                //	4	uint	JobReward
                //	1	byte	*unk08 -> Check for != 0	(According to Spawn structure => MurderFlag?)
                //	1	byte	*unk09 -> Check for != 0	(According to Spawn structure => RideFlag or AttackFlag?)
                //	1	byte	*unk10 -> Check for != 0	(According to Spawn structure => EquipmentCountdown?)
                //	1	byte	PK Flag(255 = Disable, 34 = Enable)
                //	8	ulong	*unk11
                //	4	uint	JID (=> GameAccountID)

                Char.Name = packet.ReadAscii();
                packet.ReadAscii(); // ALIAS

                packet.ReadUInt8(); // Job Level
                packet.ReadUInt8(); // Job Type
                packet.ReadUInt32(); // Trader Exp
                packet.ReadUInt32(); // Thief Exp
                packet.ReadUInt32(); // Hunter Exp
                packet.ReadUInt8(); // Trader LV
                packet.ReadUInt8(); // Thief LV
                packet.ReadUInt8(); // Hunter LV
                packet.ReadUInt8(); // PK Flag
                packet.ReadUInt16(); // Unknown
                packet.ReadUInt32(); // Unknown
                packet.ReadUInt16(); // Unknown

                Char.AccountId = packet.ReadUInt32(); // Account ID

                Debug("my acc id: {0} / {1}", Char.AccountId, Char.AccountId.ToString("X4"));
                Debug("my char id: {0} / {1}", Char.CharId, Char.CharId.ToString("X4"));
                Debug("my gold: {0}", Char.Gold);

                Char.LastPositions.Clear();
                Loop.BackInTown();
                IsUsingReturnScroll = false;
                Loop.ItemMovedFromPetToInvetory(false, false);
                Loop.CannotLearnDueToMasteryLimit(false); // reset, try again .. normally "onLevelUp" is enough, but assume ingame commads like "reset mastery" !!

                Char.IsParsed = true;

                Config.ReloadSkills();
            }
            catch (Exception ex)
            {
                Log($"Error during handling char-data: {ex.Message}: {ex.StackTrace}");
            }

            if (firstSpawn)
            {
                //firstSpawn = true;

                if (ConnectionTimes.Any()) // its a reconnect !
                {
                    reconnected();
                }
                else
                {
                    connectedFirstTime();
                }

                App.Current.Dispatcher.Invoke(() => ConnectionTimes.Add(new ConnectionInfo(ConnectionInfo.CONNECTION_TYPE.CONNECTED)));
            }
        }

        private void handleCharInfo(Packet packet)
        {
            try
            {
                //	4	uint	PhyAtkMin
                //	4	uint	PhyAtkMax
                //	4	uint	MagAtkMin
                //	4	uint	MagAtkMax
                //	2	ushort	PhyDef
                //	2	ushort	MagDef
                //	2	ushort	HitRate
                //	2	ushort	ParryRate
                //	4	uint	MaxHP
                //	4	uint	MaxMP
                //	2	ushort	STR
                //	2	ushort	INT

                packet.ReadUInt64();
                packet.ReadUInt64();
                packet.ReadUInt16();
                packet.ReadUInt16();
                packet.ReadUInt16();
                packet.ReadUInt16();
                Char.MaxHP = packet.ReadUInt32();
                Char.MaxMP = packet.ReadUInt32();

                // calc % .. !
                Char.CurHP = Char.CurHP;
                Char.CurMP = Char.CurMP;

                Char.STR = packet.ReadUInt16();
                Char.INT = packet.ReadUInt16();

                if (Char.CurMP > Char.MaxMP)
                {
                    Char.CurMP = Char.MaxMP;
                }
                if (Char.CurHP > Char.MaxHP)
                {
                    Char.CurHP = Char.MaxHP;
                }

                if (Char.CurHP > 0)
                    Char.IsAlive = true;
                else
                    Char.IsAlive = false;
            }
            catch (Exception ex)
            {
                Debug("CharInfo: {0} => {1}", ex.Message, ex.StackTrace);
            }
        }

        private void handleLevelUp(Packet packet)
        {
            //Console.WriteLine("IGNORE LEVEL UP EFFECT PACKET !!");
            return;
        }

        private void handleSpeedUpdate(Packet packet)
        {
            try
            {
                uint id = packet.ReadUInt32(); // Char ID
                if (Char.CharId == id)
                {
                    packet.ReadSingle(); // Walk Speed
                    float speed = packet.ReadSingle(); // Run Speed
                                                       //Console.WriteLine("got new speed: {0} -- old: {1}", speed, CharData.Speed);
                    Char.Speed = speed;
                }
                else if (CurSelected != null && CurSelected.UID == id)
                {
                    packet.ReadSingle(); // Walk Speed
                    float speed = packet.ReadSingle(); // Run Speed
                    //Console.WriteLine("spped update cur mob: {0} => {1}", speed, String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                }
            }
            catch (Exception ex)
            {
                Debug("SpeedUpdate: {0} => {1}", ex.Message, ex.StackTrace);
            }
        }

        private void handleStuffUpdate(Packet packet)
        {
            try
            {
                byte code = packet.ReadUInt8();
                switch (code)
                {
                    case 1: // gold
                        Char.Gold = packet.ReadUInt64();
                        goldAmountChanged(Char.Gold);
                        break;

                    case 2: // SP
                        Char.SP = packet.ReadUInt32();
                        break;

                    case 4: // ZERK
                        Char.Zerk = packet.ReadUInt8();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug("StuffUpdate: {0} ==> {1}", ex.Message, ex.StackTrace);
            }
        }

        private void handleHpMpUpdate(Packet packet)
        {
            try
            {
                uint id = packet.ReadUInt32();
                if (id == Char.CharId)
                {
                    packet.ReadUInt8();
                    packet.ReadUInt8(); // 0x00
                    byte type2 = packet.ReadUInt8();
                    switch (type2)
                    {
                        case 0x01:
                            Char.CurHP = packet.ReadUInt32();
                            break;
                        case 0x02:
                            Char.CurMP = packet.ReadUInt32();
                            break;
                        case 0x03:
                            Char.CurHP = packet.ReadUInt32();
                            Char.CurMP = packet.ReadUInt32();
                            break;
                        case 0x04:
                            if (packet.ReadUInt32() == 0)
                            {
                                Char.BadStatus = false;
                            }
                            else
                            {
                                Char.BadStatus = true;
                            }
                            break;
                    }

                    if (Char.CurHP > 0)
                    {
                        Char.IsAlive = true;
                    }
                    else
                    {
                        Char.IsAlive = false;
                        Log("i am dead (hp <= 0)");
                    }
                }
#if false
                                                            else if (id == Char_Data.char_attackpetid)
                                                            {
                                                                packet.ReadUInt8();
                                                                packet.ReadUInt8();
                                                                byte type = packet.ReadUInt8();
                                                                int pet_index = 0;
                                                                switch (type)
                                                                {
                                                                    case 0x05:
                                                                        for (int i = 0; i < Char_Data.pets.Length; i++)
                                                                        {
                                                                            if (Char_Data.pets[i].id == id)
                                                                            {
                                                                                pet_index = i;
                                                                                break;
                                                                            }
                                                                        }
                                                                        Char_Data.pets[pet_index].curhp = packet.ReadUInt32();
                                                                        if (Globals.MainWindow.attackpet_use.Checked == true)
                                                                        {
                                                                            if (Char_Data.pets[pet_index].curhp < Convert.ToUInt32(Globals.MainWindow.attackpet_hp.Text))
                                                                            {
                                                                                Autopot.UsePetHP(Char_Data.pets[pet_index].id);
                                                                            }
                                                                        }
                                                                        break;
                                                                    case 0x04:
                                                                        if (packet.ReadUInt32() == 0)
                                                                        {
                                                                            pet_status = 0;
                                                                        }
                                                                        else
                                                                        {
                                                                            pet_status = 1;
                                                                        }
                                                                        break;
                                                                }
                                                                if (Globals.MainWindow.attackpet_bad.Checked == true && pet_status == 1)
                                                                {
                                                                    Autopot.UsePetUni(Char_Data.pets[pet_index].id);
                                                                }
                                                            }
#endif
                else if (Char.Ridepet != null && id == Char.Ridepet.UID)
                {
                    packet.ReadUInt8();
                    packet.ReadUInt8();
                    byte type = packet.ReadUInt8();

                    switch (type)
                    {
                        case 0x05:
                            Char.Ridepet.CurHP = packet.ReadUInt32();
                            break;

                        case 0x04:
                            {
                                var badstatus = packet.ReadUInt32();
                                Char.Ridepet.BadStatus = badstatus;
                            }
                            break;
                    }
                }
                else
                {
                    packet.ReadUInt8();
                    packet.ReadUInt8();
                    byte _type = packet.ReadUInt8();
                    switch (_type)
                    {
                        case 0x05:
                            {
                                uint hp = packet.ReadUInt32();
                                Loop.MobHpUpdate(id, hp);
                            }
                            break;

                        case 0x04:
                            {
                                var badstatus = packet.ReadUInt32();
                                var mob = Spawns.Mobs.Get(id);
                                if (mob != null)
                                {
                                    mob.BadStatus = badstatus;
                                }

                                //if (CurSelected != null && CurSelected.UID == id)
                                //    Console.WriteLine("mob has ad status: {0} => {1}", badstatus, String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                            }
                            break;

                        default:
                            //if (CurSelected != null && CurSelected.UID == id)
                            Debug("HPMPUpdate: type: {0} => {1}", _type, String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug("HPMP: {0} => {1}", ex.Message, ex.StackTrace);
            }
        }

        private void handleObjectDied(Packet packet)
        {
            uint id = packet.ReadUInt32();
            var type = packet.ReadUInt8();
            var status = packet.ReadUInt8();

            switch (type)
            {
                case 0: // dead
                    if (status == 2)
                    {
                        if (id != Char.CharId)
                        {
                            Loop.ModDied(id);
                        }
                        else
                        {
                            Char.IsAlive = false;
                            Char.CurHP = 0;
                            ClearActiveBuffs();

                            Log("i died");
                        }
                    }
                    else if (status == 1)
                    {
                        if (id == Char.CharId)
                        {
                            Char.IsAlive = true;
                            Log("I AM BACK ALIVE !!");
                        }
                    }
                    break;

                case 4:
                    if (status == 0x07)
                    {
                        if (Char.CharId != id)
                        {
                            var player = Spawns.Player.Get(id);
                            if (player != null)
                            {
                                Debug("Invisible Player detected." + "[" + player.Name + "]");
                            }
                            else
                            {
                                Debug("Invisible Player detected.");
                            }
                        }
                    }
                    else if (status == 0x01 && Char.CharId == id) // ZERK ON
                    {
                        //Console.WriteLine("ZERK STARTED");
                        Char.ZerkInUse = true;
                    }
                    else if (status == 0x00 && Char.CharId == id) // ZERK OFF
                    {
                        //Console.WriteLine("ZERK STOPED");
                        Char.ZerkInUse = false;
                    }
                    break;

                case 7: // PINK / RED NAME

                    if (Char.CharId == id)
                    {
                        switch (status)
                        {
                            case 0: // NORMAL
                                break;

                            case 1: // PINK
                                Debug("pink name detected!");
                                break;

                            case 2: // RED
                                Debug("red name detected");
                                break;
                        }
                    }
                    break;
            }
        }

        private void handleDeleteBuf(Packet packet)
        {
            var flag = packet.ReadUInt8();

            for (var cnt = 0; cnt < flag; ++cnt)
            {
                var id = packet.ReadUInt32();
                RemoveActiveBuff(id);
            }

            Loop.checkBuffingTimer = 300; // need some time before recast, maybe the skill was updated !!!!!
        }

        private void handleBufInfo(Packet packet)
        {
            var target = packet.ReadUInt32();
            var skillId = packet.ReadUInt32();
            var ingameId = packet.ReadUInt32();
            //Console.WriteLine("{0} | buff info! target: {1}, skillId: {2}, idkwhatitis?: {3}", DateTime.Now.ToString("HH:mm:ss.fff"), target, skillId, ingameId);

            if (target == Char.CharId)
            {
                var buff = SkillInfos.GetByModel(skillId);
                if (buff != null)
                {
                    buff = buff.Copy();
                    buff.IngameId = ingameId;
                    AddActiveBuff(buff);

                    Loop.checkBuffingTimer = 200;
                }
            }
        }

        private void handleSkillCasted(Packet packet, bool singleAction = false)
        {
            if (packet.ReadUInt8() == 0x01)
            {
                var skillId = (uint)0;
                var attackerId = (uint)0;

                if (!singleAction)
                {
                    packet.ReadUInt8(); // 0x02
                    packet.ReadUInt8(); // 0x30

                    skillId = packet.ReadUInt32();
                    attackerId = packet.ReadUInt32();
                }

                var skillUniqueId = packet.ReadUInt32();
                var targetId = packet.ReadUInt32();

                if (!singleAction)
                {
                    if (attackerId == Char.CharId)
                    {
                        var skill = SkillInfos.GetByModel(skillId);
                        if (skill != null)
                        {
                            SkillCastedUniqueIds[skillUniqueId] = skill.Model;

                            var mob = Spawns.Mobs.Get(targetId);
                            if (mob != null)
                            {
                                if (mob.IsPartyMemberAttacking())
                                {
                                    //Debug(".. and I still attacked it .. ?!");
                                }
                                mob.InvalidTarget = 0;
                            }
                            //Console.WriteLine("{0} | I attack with skill: {1} --> {2}", DateTime.Now.ToString("HH:mm:ss.fff"), skill.Name, String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));

                            Config.SetCooldown(skill);
                            Config.NextSkill();

                            Loop.CheckAttacking(true);
                            Loop.CheckSkills(true);
                        }
                    }
                    else
                    {
                        var attackerPlayer = Spawns.Player.Get(attackerId);
                        var attackerMob = Spawns.Mobs.Get(attackerId);

                        if (targetId == Char.CharId)
                        {
                            if (attackerMob != null)
                            {
                                attackerMob.IsAttackingMe = true;
                            }

                            if (attackerPlayer != null)
                            {
                                Log("PLAYER {0} ATTACKED ME !!", attackerPlayer.Name);
                            }
                            //Console.WriteLine("some attacks me? attacker: {0}, target: {1}[{3}|{4}], u1: {2}", attacker_id, obj_id, unk1, CharData.AccountId, CharData.CharId);
                            //if ((BotData.CurSelected?.UID ?? 0) == attacker_id)
                            //{
                            //    //Console.WriteLine("i am attacking this mob!");
                            //}
                            //else
                            //{
                            //    var mob = BotData.GetMob(attacker_id);
                            //    if (mob == null)
                            //    {
                            //        log.DebugFormat("ERROR: attacker not listed?! {0}", attacker_id);
                            //    }
                            //}
                        }
                        else
                        {
                            if (attackerMob != null && attackerMob.IsAttackingMe)
                            {
                                attackerMob.IsAttackingMe = false;
                            }

                            if (attackerPlayer != null)
                            {
                                var partymember = Party.Members.FirstOrDefault(pm => !pm.ItsMe && pm.Name == attackerPlayer.Name);
                                if (partymember != null)
                                {
                                    var targetMob = Spawns.Mobs.Get(targetId);
                                    if (targetMob != null)
                                    {
                                        if (targetMob.Type <= 0) // normal
                                        {
                                            targetMob.SetPartyMemberAttacking(partymember.Name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                var continueType = packet.ReadUInt8();
                if (continueType != 0x00 && (singleAction || (continueType != 2 && continueType != 8)))
                {
                    byte hitCount = packet.ReadUInt8();
                    byte targetCount = packet.ReadUInt8();

                    for (int i = 0; i < targetCount; i++)
                    {
                        targetId = packet.ReadUInt32();

                        for (int l = 0; l < hitCount; l++)
                        {
                            var info = packet.ReadUInt8();
                            var aliveStatus = (info >> 4) & 0xf;
                            var actionStatus = info & 0xf;

                            if (aliveStatus == 8)
                            {
                                Loop.ModDied(targetId);
                            }

                            if (actionStatus != 0x02 && actionStatus != 0x08)
                            {
                                byte critStatus = packet.ReadUInt8(); // 01 = NORMAL ; 02 = CRIT 
                                byte[] dmg = { packet.ReadUInt8(), packet.ReadUInt8(), packet.ReadUInt8(), 0x00 };
                                var damageCount = System.BitConverter.ToUInt32(dmg, 0); // DAMAGE
                                var mob = Spawns.Mobs.Get(targetId);
                                if (mob != null)
                                {
                                    mob.CurHP -= damageCount;
                                }

                                var itWasMySkill = SkillCastedUniqueIds.ContainsKey(skillUniqueId);
                                if (itWasMySkill)
                                {
                                    if (mob != null)
                                    {
                                        if (CurSelected == mob)
                                            mob.DirectDmgDidByMe += damageCount;
                                        else
                                            mob.SplashDmgDidByMe += damageCount;

                                        if (CurSelected != null && CurSelected.UID == mob.UID)
                                            MobHpChanged();
                                    }

                                    //Main.MyDamage.doneDamage = Main.MyDamage.doneDamage + int.Parse(damageCount.ToString());
                                    //Main.MyDamage.doneDamageCounts++;
                                    //Console.WriteLine("{0} | 0x{1}: i did a dmg of: {2} to target: {3} [{4}]", DateTime.Now.ToString("HH:mm:ss.fff"), packet.Opcode.ToString("X4"), damageCount, targetId.ToString("X4"), (CurSelected != null && targetId == CurSelected.UID));

                                    if (critStatus == 0x02)
                                    {
                                        //Console.WriteLine("0x{0}: i did a crit !!", packet.Opcode.ToString("X4"));
                                    }
                                    else
                                    {
                                        //if (!Main.SkillDamge.ContainsKey(objPacket.skillid))
                                        //{
                                        //    Main.SkillDamge.Add(objPacket.skillid, Main.zerkInUse ? uint.Parse(damageCount.ToString()) / 2 : uint.Parse(damageCount.ToString()));
                                        //}
                                        //else
                                        //{
                                        //    Main.SkillDamge[objPacket.skillid] = (Main.SkillDamge[objPacket.skillid] + (Main.zerkInUse ? uint.Parse(damageCount.ToString()) / 2 : uint.Parse(damageCount.ToString()))) / 2;
                                        //}
                                    }

                                    if (damageCount > Char.HighestDmg)
                                    {
                                        Char.HighestDmg = damageCount;

                                        //Debug("0x{0}: new highest dmg: {1}", packet.Opcode.ToString("X4"), Char.HighestDmg);
                                    }
                                }
                                else if (targetId == Char.CharId)
                                {
                                    if (critStatus == 0x02)
                                    {
                                        //Console.WriteLine("0x{0}: got a crit =/", packet.Opcode.ToString("X4"));
                                    }
                                    //if (int.Parse(damageCount.ToString()) > Main.MyDamage.gotHighestDamage)
                                    //{
                                    //    Main.MyDamage.gotHighestDamage = int.Parse(damageCount.ToString());
                                    //}
                                }

                                packet.ReadUInt32();

                                //objPacket.status = actionStatus;

                                switch (actionStatus)
                                {
                                    case 0: // nothing ?!
                                        break;

                                    case 4:
                                    case 5:
                                    case 9:
                                        {
                                            packet.ReadUInt8(); // REGION
                                            packet.ReadUInt8();// REGION
                                            packet.ReadSingle(); // X
                                            packet.ReadSingle(); // Z
                                            packet.ReadSingle(); // Y

                                            if (actionStatus == 0x04) // KNOCK DOWN
                                            {
                                                if (mob != null)
                                                {
                                                    mob.KnockedDown = true;
                                                    //Log("KNOCK DOWN DETECTED !!");
                                                    var t_standup = new System.Threading.Thread(WaitStandup);
                                                    t_standup.Start(new { bot = this, id = mob.UID });
                                                }
                                            }
                                            else if (actionStatus == 0x09) // ATTACKER IS MOVING (SPRINT ASSOULT)
                                            { }
                                        }
                                        break;

                                    default:
                                        Debug($"actionStatus: {actionStatus}");
                                        break;
                                }
                            }
                        }
                    }

                    SkillCastedUniqueIds.Remove(skillUniqueId);
                }
            }
            else
            {
                if (!singleAction)
                {
                    var _type = packet.ReadUInt8();
                    switch (_type)
                    {
                        case 4:
                            //Log("NOT ENOUGH MANA??");
                            break;

                        case 5:
                            //Debug("was das hier? next skill? - COOLDOWN NOT OVER?");
                            //Config.NextSkill(); // war nur wegen dem "tiger thunder..." ?!
                            break;

                        case 6: // invalid target
                            {
                                var mob = CurSelected; // to get no raise condition !
                                if (mob == null || mob.KnockedDown || (mob.BadStatus & 8) == 0) return;

                                mob.InvalidTarget += 1;

                                //Debug($"INCED InvalidTarget({mob.UID}): {mob.InvalidTarget} (0b{Convert.ToString(mob.BadStatus, 2).PadLeft(32, '_').Replace("0", "_")}) .. HP: {mob.CurHP} ==> {string.Join(", ", packet.GetBytes().Select(b => b.ToString("X2")))}");

                                Movement.WalkTo(this, mob.X, mob.Y); // kann nicht schaden..

                                if (mob.InvalidTarget >= 5)
                                {
                                    //Debug($"ignore cur mob.. KnockedDown: {mob.KnockedDown}");
                                    mob.Ignore = 5;

                                    SaveLastMob(CurSelected);
                                    CurSelected = null;
                                }
                                else
                                {
                                    return; // DO NOT FORCE ATTACKING .. maybe a small delay will help ;)
                                }
                            }
                            break;

                        case 9: // cant attack cause of .. knocked down, or stunned or what ever ..
                            //Debug("i am KNOCKED down..");
                            break;

                        case 12: // cant use this skill cause already in use... ODER SO ^^
                            break;

                        case 14: // no arrows
                            {
                                Log("NO ARROWS!");

                                var arrows = Inventory.GetLowestStackByType("ITEM_ETC_AMMO_ARROW_01_DEF");
                                if (Inventory.IsItemEmpty(arrows))
                                {
                                    arrows = Inventory.GetLowestStackByType("ITEM_ETC_AMMO_ARROW_01");
                                }

                                if (Inventory.IsItemNotEmpty(arrows))
                                {
                                    Log("NO arrows equipped! => move them to Slot 7 !!");
                                    Actions.SwapItems(arrows.Slot, 7, this);
                                }
                                else
                                {
                                    UseReturnScroll();
                                }
                            }
                            break;

                        case 16: // cannot attack due to obstacle
                            //Debug("CANNOT ATTACK DUE TO OBSTACLE !!");
                            Loop.Obstacle();
                            break;

                        default:
                            Log("cant attack cause: {0} => {1}", _type, String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                            break;
                    }

                    // force attacking now !!
                    Loop.CheckAttacking(true);
                    Loop.CheckSkills(true);
                }
                else
                {
                    var uk = packet.ReadUInt8Array(2);
                    var id = packet.ReadUInt32();
                    var mob = Spawns.Mobs.Get(id);
                    if (mob != null)
                    {
                        Debug("{0} | 0xB071: fail? something todo with this mob: {1} / {2} --> i am attacking: {3}", DateTime.Now.ToString("HH:mm:ss.fff"), mob.UID, mob.Mobinfo.Type, CurSelected == mob);
                    }
                    else
                    {
                        var player = Spawns.Player.Get(id);
                        if (player != null)
                        {
                            Debug("{0} | 0xB071: fail? something todo with this player: {1} / {2}", DateTime.Now.ToString("HH:mm:ss.fff"), player.UID, player.Name);
                        }
                    }
                }
            }
        }

        private static void WaitStandup(object botNmobid)
        {
            try
            {
                var bot = (SROBot.Bot)(((dynamic)botNmobid).bot);
                var mobid = (uint)(((dynamic)botNmobid).id);

                System.Threading.Thread.Sleep(5000);
                try
                {
                    var mob = bot.Spawns.Mobs.Get(mobid);
                    if (mob != null) // STILL ALIVE?
                    {
                        mob.KnockedDown = false;
                        //bot.Log("KNOCK DOWN RELEASED !!");
                    }
                }
                catch { }
            }
            catch { }
        }

        private void handleExpSpUpdate(Packet packet)
        {
            try
            {
                packet.ReadUInt32();
                ulong exp = packet.ReadUInt64();
                var sp = packet.ReadUInt64(); //SP XP

                var newExp = Char.EXP + exp;
                if (newExp < SROData.ExpPoints.AtLevel[Char.Level])
                {
                    Char.EXP = newExp;
                }
                else
                {
                    while (newExp >= SROData.ExpPoints.AtLevel[Char.Level])
                    {
                        newExp -= SROData.ExpPoints.AtLevel[Char.Level];

                        Char.Level += 1;
                        Char.EXP = newExp;
                        if (Char.Level > Char.MaxLevel)
                        {
                            Char.RemainStatPoints += 3;
                            Char.MaxLevel += 1;

                            Loop.CannotLearnDueToMasteryLimit(false);
                        }

                        Log("LEVEL UP !! ==> {0} with EXP: {1:F2}%", Char.Level, Char.EXPPercentage);
                    }

                    if (Config.Training.UseLevelDependentTrainplace && Config.TrainPlace.LevelUp(Char.Level))
                    {
                        Log($"trainplace changed!{(Loop.IsStarted ? " -> teleport in 10 seconds.." : "")}");

                        Config.Save();

                        if (!Loop.IsStarted) return;

                        new Thread(() =>
                        {
                            Thread.Sleep(10000);
                            UseReturnScroll();
                            UseReturnScroll();

                        }).Start();
                    }

                }
            }
            catch (Exception ex)
            {
                Debug("EXPSP Update: {0} => {1}", ex.Message, ex.StackTrace);
            }
        }

        private void handleHorseAction(Packet packet)
        {
            if (packet.ReadUInt8() == 0x01)
            {
                uint char_id = packet.ReadUInt32();
                if (char_id == Char.CharId)
                {
                    byte action = packet.ReadUInt8();
                    uint pet_id = packet.ReadUInt32();

                    var ridepet = Spawns.Pets.Get(pet_id);
                    if (ridepet == null) return;

                    switch (action)
                    {
                        case 0x00:
                            Debug("{0} | ridepet DISmounted", DateTime.Now.ToString("HH:mm:ss.fff"));
                            Char.Ridepet = null;
                            break;

                        case 0x01:
                            Char.Ridepet = ridepet;
                            Debug("{0} | ridepet mounted: {1}", DateTime.Now.ToString("HH:mm:ss.fff"), Char.Ridepet.Mobinfo.Type);
                            break;

                        default:
                            Debug("HORSEACTION: {0} -> unknown action", action);
                            break;
                    }
                }
            }
        }

        private void handlePetInfo(Packet packet)
        {
            try
            {
                uint pet_id = packet.ReadUInt32();
                uint pet_model = packet.ReadUInt32();
                var petInfo = MobInfos.GetById(pet_model);
                if (petInfo == null) return;

                var pet = new Pet(petInfo, pet_id);

                /*
                 * TYPEID3: 3 for all PETS
                 * TYPEID4:
                 * 1: RIDE
                 * 2: Transport
                 * 3: Attack
                 * 4: Pick
                 * 5: COS_GUILD_XX_SOLDIER_..
                 * 6: MOB_QT_..
                 * 7: NPC_CH_QT_..
                 * 8: NPC_CH_QT_..
                */

                if (petInfo.TypeId3 != 3)
                {
                    Log($"PET: TypeId3 != 3 !! {petInfo.Type} / {petInfo.TypeId3}");
                }

                string pet_type = pet.Mobinfo.Type;
                //if (pet_type.StartsWith("COS_C_HORSE") || pet_type.StartsWith("COS_C_DHORSE"))
                if (petInfo.TypeId4 == 1) // RIDE
                {
                    pet.CurHP = packet.ReadUInt32();
                    pet.Mobinfo.Hp = packet.ReadUInt32();
                    Debug("horse summoned !?");
                }
                //else if (pet_type.StartsWith("COS_P_WOLF") || pet_type.StartsWith("COS_P_WOLF_WHITE") || pet_type.StartsWith("COS_P_BEAR") || pet_type.StartsWith("COS_P_KANGAROO") || pet_type.StartsWith("COS_P_PENGUIN") || pet_type.StartsWith("COS_P_RAVEN") || pet_type.StartsWith("COS_P_FOX") || pet_type.StartsWith("COS_P_JINN"))
                else if (petInfo.TypeId4 == 3)
                {
                    pet.CurHP = packet.ReadUInt32();
                    packet.ReadUInt32(); // Unknown
                    packet.ReadUInt64(); // EXP
                    packet.ReadUInt8(); // Level
                    pet.HGP = packet.ReadUInt16(); // HGP
                    packet.ReadUInt32(); // Unknown
                    pet.Name = packet.ReadAscii();
                    packet.ReadUInt8(); // Unknown
                    pet.OwnerId = packet.ReadUInt32(); // Char ID
                    packet.ReadUInt8(); // Unknown

                    Char.Attackpet = pet;
                    Debug("Found Attack Pet: " + pet.Name);
                }
                //else if (true /*grabpet_spawn_types.Any(p => p.Contains(pet.Mobinfo.Type))*/)
                else if (petInfo.TypeId4 == 4) // PICK
                {
                    pet.Inventory = Inventory.Create(this);
                    pet.Inventory.Clear();

                    Char.Pickpet = pet;

                    packet.ReadUInt64(); // Unknown
                    packet.ReadUInt32(); // Unknown
                    pet.Name = packet.ReadAscii(); // Petname

                    pet.Inventory.Size = packet.ReadUInt8();
                    byte items_count = packet.ReadUInt8();

                    pet.Inventory.Clear();

                    for (int i = 0; i < items_count; i++)
                    {
                        byte slot = packet.ReadUInt8();
                        var item = Inventory.ParseItem(packet, this, slot);
                        if (item == null) continue;

                        pet.Inventory.Add(item);
                    }

                    Debug("Found Grab Pet: {0} with {1} free slots", pet.Name, pet.Inventory.FreeSlots(0));
                }
                else
                {
                    Debug("UNKNOWN PET?!?!?!?! => {0} / {1}", pet.Mobinfo.Type, petInfo.TypeId4);
                }
            }
            catch (Exception ex)
            {
                Debug("PetData: {0} => {1}", ex.Message, ex.StackTrace);
            }
        }

        public static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp);
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        private void handleConsignmentInfo(Packet packet)
        {
            var cmd = packet.ReadUInt8();
            if (cmd != 1)
            {
                Log("handleConsignmentInfo(): cmd != 1");
                return;
            }

            ConsignmentItems.Clear();

            var itemCnt = packet.ReadUInt8();
            if (itemCnt == 0)
            {
                Debug("consignment: no items registered..");
            }

            while (itemCnt-- > 0)
            {
                ConsignmentItems.Add(Consignment.ConsignmentItem.ParseMyItem(packet, this));
            }
        }

        private bool handleConsignmentSearch(Packet packet)
        {
            if (Consig != null)
            {
                Consig.HandlePacket(packet);
                return Consig.SkipFromClient;
            }

            return false;
        }

        private bool handleConsignmentSettled(Packet packet)
        {
            var settledSlots = new List<UInt32>();
            var success = packet.ReadUInt8() == 1;
            var itemCnt = packet.ReadInt8();

            while (success && itemCnt-- > 0)
            {
                var slot = packet.ReadUInt32();
                settledSlots.Add(slot);
            }

            if (success)
            {
                //ConsignmentItems = ConsignmentItems.Except(ConsignmentItems.Where(ci => settledSlots.Contains(ci.ConsigId))).ToList();
                ConsignmentItems.Where(ci => settledSlots.Contains(ci.ConsigId)).ToList().ForEach(ci => ConsignmentItems.Remove(ci));

                if (ConsignmentItems.Any())
                {
                    Debug("consignment items left:");
                    foreach (var ci in ConsignmentItems)
                    {
                        Debug($"consignment({ci.ConsigId}): {ci.Item?.Type ?? "-UNKNOWN-"} .. COUNT: {ci.Count} -- {(ci.Expired ? "expired" : "sold")} = {(ci.Expired ? "YES" : ci.Sold ? "YES" : "NO")} -- price: {ci.Price:N0} // EXPIRING: {ci.ExpiringAt.ToString("dd.MM.yy HH:mm:ss")}");
                    }
                }
                else
                {
                    Debug("no consignment items left!");
                }
            }
            else
            {
                Debug($"settle fails: {string.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2")))}");
            }

            Loop.ConsignmentSettled(success);

            return Loop.IsUsingConsignment();
        }

        private bool handleConsignmentItemAborted(Packet packet)
        {
            if (!Loop.IsUsingConsignment()) return false;

            var success = packet.ReadUInt8() == 1;
            var itemCnt = packet.ReadInt8();

            while (success && itemCnt-- > 0)
            {
                var consigId = packet.ReadUInt32();
                var invSlot = packet.ReadUInt8();
                var invItem = Inventory.ParseItem(packet, this, invSlot);

                Inventory.Add(invItem);
                Actions.FakeItemPickUp(this, invItem);

                Debug($"consignment: item aborted: {invItem.Iteminfo.Type}({consigId}) to inv-slot: {invSlot}");

                ConsignmentItems.Where(ci => ci.ConsigId == consigId).ToList().ForEach(ci => ConsignmentItems.Remove(ci));
            }

            Loop.ConsignmentItemAborted(success);

            return Loop.IsUsingConsignment();
        }

        private bool handleConsignmentItemRegistered(Packet packet)
        {
            if (!Loop.IsUsingConsignment()) return false;

            var successByte = packet.ReadUInt8();
            var success = successByte == 1;
            var itemCnt = packet.ReadUInt8();

            if (successByte != 1)
            {
                Console.WriteLine($"successByte: {successByte}");
            }

            while (success && itemCnt-- > 0)
            {
                var invSlot = packet.ReadUInt8();
                packet.ReadUInt8();
                var consigId = packet.ReadUInt32();
                var itemModel = packet.ReadUInt32();
                var deposit = packet.ReadUInt64();
                var comission = packet.ReadUInt64();
                var seconds = packet.ReadUInt32();

                Log($"consignment: item registered: {ItemInfos.GetById(itemModel).Type}({consigId}) from inv-slot: {invSlot} .. expiring at: {new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds).ToString("dd.MM.yy HH:mm:ss")}");

                var invItem = Inventory.GetItem(invSlot);
                Inventory.Remove(invSlot);

                Actions.FakeItemDrop(this, invItem.Slot);

                var ci = Consignment.ConsignmentItem.Create(consigId, Consignment.ConsignmentItem.CONSIG_ITEM_STATE.RUNNING, itemModel, invItem.Count);
                ConsignmentItems.Add(ci);
            }

            Loop.ConsignmentItemRegistered(success);

            return Loop.IsUsingConsignment();
        }

        private bool handleConsignmentClosed(Packet packet)
        {
            Loop.ConsignmentClosed(packet.ReadUInt8() == 1);

            return Loop.IsUsingConsignment();
        }

        public void handleConsignmentUpdate(Packet packet)
        {
            // 0x01, 0x45, 0x01, 0x00, 0x00, 0x9E, 0x98, 0x00, 0x00, 0x01, 0x33, 0x6A, 0x9D, 0x58
            var itemCnt = packet.ReadUInt8();
            while (itemCnt-- > 0)
            {
                var consigId = packet.ReadUInt32();
                var itemModel = packet.ReadUInt32();
                var status = packet.ReadUInt8();
                var secondsLeft = packet.ReadUInt32();
                var consigItem = ConsignmentItems.FirstOrDefault(ci => ci.ConsigId == consigId);
                var action = "";

                switch (status)
                {
                    case 1: // sold
                        action = "sold!";
                        if (consigItem != null) consigItem.State = Consignment.ConsignmentItem.CONSIG_ITEM_STATE.SOLD;
                        break;

                    case 2: // expired
                        action = "expired";
                        if (consigItem != null) consigItem.State = Consignment.ConsignmentItem.CONSIG_ITEM_STATE.EXPIRED;
                        break;

                    case 0xff: // deleted
                        action = "deleted!";
                        if (consigItem != null) ConsignmentItems.Remove(consigItem);
                        break;

                    default:
                        action = $"unknown({status})";
                        break;
                }

                Log($"consignment: update: {ItemInfos.GetById(itemModel).Type}({consigId}) {action}");
            }
        }
    }
}
