using SilkroadSecurityApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace sroBot
{
    public static class BinaryReaderExtension
    {
        public static String ReadAscii(this BinaryReader br)
        {
            var len = br.ReadUInt16();
            var buf = new byte[len];
            return Encoding.GetEncoding(0x4e4).GetString(br.ReadBytes(len));
        }
    }

    public class SpawnParsing
    {
        public static ILog log = LogManager.GetLogger(typeof(SpawnParsing));

        private enum SpawnType
        {
            SPAWNED,
            DESPWANED
        }

        private SpawnType groupSpawnType;
        private List<byte> groupSpawnData = new List<byte>();
        private int groupSpawnCnt = 0;

        public void HandleSpawnPacket(Packet packet, SROBot.Bot bot)
        {
            if (packet == null) return;

            switch (packet.Opcode)
            {
                case (ushort)SROData.Opcodes.SERVER.GROUPSPAWNB:
                    handleGroupSpawnBegin(packet);
                    break;
                case (ushort)SROData.Opcodes.SERVER.GROUPESPAWN:
                    handleGroupSpawnData(packet);
                    break;
                case (ushort)SROData.Opcodes.SERVER.GROUPSPAWNEND:
                    handleGroupSpawnEnd(bot);
                    break;
                case (ushort)SROData.Opcodes.SERVER.SINGLESPAWN:
                    handleSingleSpawn(packet, bot);
                    break;
                case (ushort)SROData.Opcodes.SERVER.SINGLEDESPAWN:
                    handleSingleDespawn(packet, bot);
                    break;
            }
        }

        private void handleGroupSpawnBegin(Packet packet)
        {
            var spawnType = packet.ReadUInt8();
            groupSpawnCnt = packet.ReadUInt16();
            groupSpawnType = spawnType == 1 ? SpawnType.SPAWNED : SpawnType.DESPWANED;
            groupSpawnData = new List<byte>();
        }

        private void handleGroupSpawnData(Packet packet)
        {
            groupSpawnData.AddRange(packet.GetBytes());
        }

        private void handleGroupSpawnEnd(SROBot.Bot bot)
        {
            if (groupSpawnCnt == 0 || groupSpawnData.Count == 0)
            {
                log.DebugFormat("group spawn end: error: count: {0}, data.count: {1}", groupSpawnCnt, groupSpawnData.Count);
                return;
            }

            using (var packet = new BinaryReader(new MemoryStream(groupSpawnData.ToArray())))
            {
                if (groupSpawnType == SpawnType.SPAWNED)
                {
                    var i = 0;
                    var err = false;
                    //for (; i < groupSpawnCnt; i++)
                    while (packet.BaseStream.Position + 4 < packet.BaseStream.Length)
                    {
                        if (!ParseSpawn(packet, bot))
                        {
                            //Console.WriteLine("groupspawn: error on curIdx: {0}/{1}", i, groupSpawnCnt);
                            err = true;
                        }
                        ++i;
                    }

                    //if (i < groupSpawnCnt)
                    //{
                    //    log.DebugFormat("group spawn: error: stop by {0} from {1}\r\n{2}", i, groupSpawnCnt, String.Join(", ", groupSpawnData.Select(b => b.ToString("X2"))));
                    //}

                    if (err)
                    {
                        packet.BaseStream.Position = 0;
                        packet.BaseStream.Seek(0, SeekOrigin.Begin);
                        var bytes = packet.ReadBytes((int)packet.BaseStream.Length);
                        bot.Debug("group spawn error: {0}/{1} => {2}", i, groupSpawnCnt, String.Join(", ", bytes.Select(b => "0x" + b.ToString("X2"))));
                    }
                }
                else
                {
                    for (int i = 0; i < groupSpawnCnt; i++)
                    {
                        uint id = packet.ReadUInt32();
                        bot.Spawns.Remove(id);
                    }
                }
            }
        }

        private static void handleSingleSpawn(Packet packet, SROBot.Bot bot)
        {
            using (var br = new BinaryReader(new MemoryStream(packet.GetBytes())))
                ParseSpawn(br, bot, true);
        }

        private static void handleSingleDespawn(Packet packet, SROBot.Bot bot)
        {
            uint id = packet.ReadUInt32();

            // check all other types (items, npc, ...)

            bot.Spawns.Remove(id);
        }

        public static bool ParseSpawn(BinaryReader packet, SROBot.Bot bot, bool singleSpawn = false)
        {
            try
            {
                uint model = packet.ReadUInt32();
                if (model == 0xffffffff)
                {
                    var circleData = packet.ReadBytes(26);
                    using (var br = new BinaryReader(new MemoryStream(circleData)))
                        ParseCircle(br);

                    return true;
                }
                else if (model == 19556) // INS_QUEST_... -> "teleportbuilding.txt"
                {
                    return ParseOther(packet);
                }

                var mobinfo = MobInfos.GetById(model);
                var item = ItemInfos.GetById(model);
                var portal = SROData.Portals.GetByModel(model);

                if (item != null)
                {
                    return ParseItem(packet, item, bot, singleSpawn);
                }
                else if (mobinfo != null)
                {
                    if (mobinfo.Type.StartsWith("MOB"))
                    {
                        return ParseMob(packet, mobinfo, bot);
                    }
                    else if (mobinfo.Type.StartsWith("COS"))
                    {
                        return ParsePets(packet, mobinfo, bot);
                    }
                    else if (mobinfo.Type.StartsWith("NPC"))
                    {
                        return ParseNPC(packet, mobinfo, bot);
                    }
                    else if (mobinfo.Type.StartsWith("CHAR"))
                    {
                        return ParseChar(packet, mobinfo, bot);
                    }
                    //else if (mobinfo.Type.Contains("_GATE"))
                    //{
                    //    Console.WriteLine("GATE DUE TO MOBINFO");
                    //    return ParsePortal(packet, mobinfo, bot);
                    //}
                    else
                    {
                        bot.Debug("ERROR");
                    }
                }
                else if (portal != null)
                {
                    return ParsePortal(packet, portal, bot);
                }
                else
                {
                    log.DebugFormat("spawn: error: unknown object: model: {0}", model);
                    //packet.ReadUInt32();
                    //packet.BaseStream.Seek(-3, SeekOrigin.Current);
                    packet.BaseStream.Position -= 3;
                    return false;
                }
            }
            catch { }

            return false;
        }

        public static bool ParseItem(Packet packet, ItemInfo item, SROBot.Bot bot, bool singleSpawn = false)
        {
            if (packet == null || item == null) return false;
            return ParseItem(new BinaryReader(new MemoryStream(packet.GetBytes().Skip(4).ToArray())), item, bot, singleSpawn);
        }

        public static bool ParseItem(BinaryReader br, ItemInfo iteminfo, SROBot.Bot bot, bool singleSpawn = false)
        {
            if (br == null || iteminfo == null) return false;
            
            try
            {
                if (iteminfo.Type.StartsWith("ITEM_ETC_GOLD"))
                {
                    var amount = br.ReadUInt32(); // Ammount
                }
                if (iteminfo.Type.StartsWith("ITEM_QSP") || iteminfo.Type.StartsWith("ITEM_ETC_TRADE") ||
                    iteminfo.Type.Contains("_HALLOWEEN_") ||
                    iteminfo.Type.StartsWith("ITEM_ETC_E110125") ||
                    (iteminfo.TypeId2 == 3 && iteminfo.TypeId3 == 9 && iteminfo.TypeId4 == 0))
                {
                    var name = br.ReadAscii(); // Name
                    //bot.Debug("QUEST/TRADE ({0}) item from: {1} -> {2}, {3}, {4}", iteminfo.Type, name, iteminfo.ClassA, iteminfo.ClassB, iteminfo.ClassC);
                }
                if (iteminfo.Type.StartsWith("ITEM_CH") || iteminfo.Type.StartsWith("ITEM_EU"))
                {
                    var plus = br.ReadByte(); // Plus
                }

                uint id = br.ReadUInt32(); // ID
                
                var xsec = br.ReadByte(); //XSEC
                var ysec = br.ReadByte(); //YSEC
                var xcoord = br.ReadSingle(); //X
                br.ReadSingle(); //Z
                var ycoord = br.ReadSingle(); //Y
                br.ReadUInt16(); //POS

                int xas = Movement.CalculatePositionX(xsec, xcoord);
                int yas = Movement.CalculatePositionY(ysec, ycoord);
                int dist = Movement.GetDistance(xas, bot.Char.CurPosition.X, yas, bot.Char.CurPosition.Y);

                var item = new Item(iteminfo, id) { X = xas, Y = yas, Distance = dist };

                var itemOwner = br.ReadByte();
                if (itemOwner == 1) // Owner exist
                {
                    var itemOwnerId = br.ReadUInt32();
                    if (itemOwnerId == bot.Char.AccountId || itemOwnerId == bot.Char.CharId) // Owner ID
                    {
                        item.Pickable = true;
                    }
                    //else log.DebugFormat("another owner? {0}", itemOwnerId);
                }
                else // 71, 72 ?
                {
                    item.Pickable = true;
                }
                
                var hasBlues = br.ReadByte(); //Item Blued

                switch (hasBlues)
                {
                    case 0: // None
                        break;
                    case 1: // hasMagicOptions
                        break;
                    case 2: // RARE_A, RARE_B, RARE_C
                        //Console.WriteLine("RARE DROP!! {0}", iteminfo.Type);
                        break;
                    case 3: // SET
                        break;
                    case 6: // ROC_SET
                        break;
                    case 7: // Legend
                        break;
                }

                if (singleSpawn)
                {
                    var dropSource = br.ReadByte();
                    switch (dropSource)
                    {
                        case 5: //Dropped by Monster
                            var mobId = br.ReadUInt32();
                            break;

                        case 6: //Dropped by Player
                            var playerId = br.ReadUInt32();

                            //if (playerId != bot.Char.CharId) Console.WriteLine("dropped by player: {0}", playerId);
                            //else Console.WriteLine("item dropped by ME !! oO ");

                            item.Pickable = (playerId != bot.Char.CharId); // dont let bot pick it up again !

                            break;
                    }
                }

                if ((item.Iteminfo.Type.StartsWith("ITEM_ETC_E110125_HALLOWEEN") || item.Iteminfo.Type.StartsWith("ITEM_ETC_E110125")) && item.Pickable)
                {
                    bot.Loop.CheckPickup(true);
                }

                bot.Spawns.Items.Add(item);
            }
            catch (Exception ex)
            {
                log.DebugFormat("error parsing ITEM: {0} ==> {1}", ex.Message, ex.StackTrace);
                return false;
            }

            return true;
        }

        public static bool ParseMob(Packet packet, MobInfo mob, SROBot.Bot bot)
        {
            if (packet == null || mob == null) return false;
            return ParseMob(new BinaryReader(new MemoryStream(packet.GetBytes().Skip(4).ToArray())), mob, bot);
        }

        public static bool ParseMob(BinaryReader packet, MobInfo mobinfo, SROBot.Bot bot)
        {
            try
            {
                uint id = packet.ReadUInt32(); // MOB ID
                
                byte xsec = packet.ReadByte();
                byte ysec = packet.ReadByte();
                float xcoord = packet.ReadSingle();
                packet.ReadSingle();
                float ycoord = packet.ReadSingle();
                packet.ReadUInt16(); // Position
                byte move = packet.ReadByte(); // Moving
                packet.ReadByte(); // Running
                if (move == 1)
                {
                    xsec = packet.ReadByte();
                    ysec = packet.ReadByte();
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
                }
                else
                {
                    packet.ReadByte(); // Unknown
                    packet.ReadUInt16(); // Unknwon
                }

                int xas = Movement.CalculatePositionX(xsec, xcoord);
                int yas = Movement.CalculatePositionY(ysec, ycoord);
                int dist = Movement.GetDistance(xas, bot.Char.CurPosition.X, yas, bot.Char.CurPosition.Y);

                byte alive = packet.ReadByte(); // Alive
                var uk1 = packet.ReadByte(); // Unknown
                var uk2 = packet.ReadByte(); // Unknown
                packet.ReadByte(); // Zerk Active
                packet.ReadSingle(); // Walk Speed
                packet.ReadSingle(); // Run Speed
                packet.ReadSingle(); // Zerk Speed
                  
                byte NumOfBuffs = packet.ReadByte();
                for (int n = 0; n < NumOfBuffs; n++)
                {
                    var unkn1 = packet.ReadUInt32();
                    var unkn2 = packet.ReadUInt32();
                }

                packet.ReadByte(); // Nametype
                packet.ReadByte();
                packet.ReadByte();

                byte type = packet.ReadByte();

                var mob = new Mob(mobinfo, id)
                {
                    X = xas,
                    Y = yas,
                    Distance = dist,
                    Type = type
                };

                switch (type)
                {
                    case 0: // normal
                        break;
                    case 1: // champion
                        mob.Mobinfo.Hp *= 2;
                        break;
                    case 3: // unique
                        break;
                    case 4: // giant
                        mob.Mobinfo.Hp *= 20;
                        break;
                    case 5: // titan
                        break;
                    case 6: // elite
                        break;

                        // party

                    case 0x10: // normal
                        mob.Mobinfo.Hp *= 10;
                        break;
                    case 0x11: // champion
                        mob.Mobinfo.Hp *= 20;
                        break;
                    case 0x13: // unique
                        break;
                    case 0x14: // giant
                        mob.Mobinfo.Hp *= 200;
                        break;
                    case 0x15: // titan
                        break;
                    case 0x16: // elite
                        break;

                }

                mob.CurHP = (long)mob.Mobinfo.Hp;

                if (mobinfo.Type.StartsWith("MOB_HUNTER") || mobinfo.Type.StartsWith("MOB_THIEF"))
                {
                    //bot.Debug("i am a thief, or hunter? model: {0} --> read one byte more!", mobinfo.Type);
                    packet.ReadByte();
                }

                if (alive == 1)
                {
                    bot.Spawns.Mobs.Add(mob);
                    //if (type == (byte)Globals.enumMobType.Unique && Globals.MainWindow.alert_unique.Checked)
                    //{
                    //    Alert.StartAlert();
                    //}
                }
                // (alive == 2) => dead

                //log.DebugFormat("{0} | parsed MOB: {1} =>", DateTime.Now.ToString("HH:mm:ss.fff"), mob.Type);
            }
            catch (Exception ex)
            {
                bot.Debug("error parsing mob !! {0} => {1}", ex.Message, ex.StackTrace);
                return false;
            }

            return true;
        }

        public static bool ParsePets(Packet packet, MobInfo mobinfo, SROBot.Bot bot)
        {
            if (packet == null || mobinfo == null) return false;
            return ParsePets(new BinaryReader(new MemoryStream(packet.GetBytes().Skip(4).ToArray())), mobinfo, bot);
        }

        public static bool ParsePets(BinaryReader packet, MobInfo mobinfo, SROBot.Bot bot)
        {
            uint pet_id = 0;
            try
            {
                pet_id = packet.ReadUInt32(); // PET ID

                byte xsec = packet.ReadByte();
                byte ysec = packet.ReadByte();
                float xcoord = packet.ReadSingle();
                packet.ReadSingle();
                float ycoord = packet.ReadSingle();

                packet.ReadUInt16(); // Position
                byte move = packet.ReadByte(); // Moving
                packet.ReadByte(); // Running

                if (move == 1)
                {
                    xsec = packet.ReadByte();
                    ysec = packet.ReadByte();
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
                }
                else
                {
                    packet.ReadByte(); // Unknown
                    packet.ReadUInt16(); // Unknwon
                }

                int xas = Movement.CalculatePositionX(xsec, xcoord);
                int yas = Movement.CalculatePositionY(ysec, ycoord);
                int dist = Movement.GetDistance(xas, bot.Char.CurPosition.X, yas, bot.Char.CurPosition.Y);

                packet.ReadByte(); // death flag
                packet.ReadByte(); // movement
                packet.ReadByte(); // berzerk
                packet.ReadByte(); //
                packet.ReadSingle(); // speed1
                var petSpeed = packet.ReadSingle(); // speed2
                packet.ReadSingle(); // speed3
                
                byte NumOfBuffs = packet.ReadByte();
                for (int n = 0; n < NumOfBuffs; n++)
                {
                    packet.ReadUInt32();
                    packet.ReadUInt32();

                    // extra byte for special skills ?!
                }

                packet.ReadByte(); // Nametype

                if (mobinfo == null) throw new Exception("cant find mobinfo");
                
                var petName = "";
                var ownerName = "";
                
                if (mobinfo.Type.StartsWith("COS_C"))
                {
                    
                }
                else if (mobinfo.Type.StartsWith("COS_T_DHORSE"))
                {
                    ownerName = packet.ReadAscii();

                    packet.ReadUInt32(); // idk what..
                    packet.ReadByte(); // idk what..
                    packet.ReadByte(); // idk what..
                }
                else
                {
                    if (mobinfo.Type.StartsWith("COS_U_UNKNOWN"))
                    {
                        packet.ReadUInt16();
                        packet.ReadByte();
                    }
                    else
                    {

                        if (mobinfo.Type.StartsWith("COS_P_SCARY_GORILLA"))
                        {
                            // muss nicht !
                        }

                        petName = packet.ReadAscii();


                        if (mobinfo.Type.StartsWith("COS_P_SCARY_GORILLA"))
                        {
                            // muss nicht !
                        }

                        //if (ascii1.Length != 0) Console.WriteLine("pet ascii1: --> transport PET: owner: {0}", ascii1);
                        //else Console.WriteLine("pet ascii1 empty --> pet has no name..");

                        if (mobinfo.Type.StartsWith("COS_T_COW") ||
                            mobinfo.Type == "COS_T_DONKEY" ||
                            mobinfo.Type.StartsWith("COS_T_HORSE") ||
                            mobinfo.Type.StartsWith("COS_T_CAMEL") ||
                            mobinfo.Type.StartsWith("COS_T_DHORSE") ||
                            mobinfo.Type.StartsWith("COS_T_BUFFALO") ||
                            mobinfo.Type.StartsWith("COS_T_WHITEELEPHANT") ||
                            mobinfo.Type.StartsWith("COS_T_RHINOCEROS") ||
                            mobinfo.Type.StartsWith("COS_T_") /* more global.. idk if it works????? */)
                        {
                            if (mobinfo.Type.StartsWith("COS_T_BUFFALO")
                                /*mobinfo.Type.StartsWith("COS_T_WHITEELEPHANT") ||*/
                                /*mobinfo.Type.StartsWith("COS_T_RHINOCEROS")*/
                                )
                            {
                                //log.DebugFormat("read an extra byte? dunno why..");
                                packet.ReadByte();
                            }
                            if (mobinfo.Type.StartsWith("COS_T_WHITEELEPHANT") ||
                                mobinfo.Type.StartsWith("COS_T_RHINOCEROS"))
                            {
                                //log.DebugFormat("DONT read an extra byte!!!! => {0}", mobinfo.Type);
                            }
                            packet.ReadUInt16();
                            packet.ReadUInt32();

                            //log.DebugFormat("{0} | parsed TPET: {1} from {2}", DateTime.Now.ToString("HH:mm:ss.fff"), mobinfo.Type, ascii1);
                        }
                        else
                        {
                            ownerName = packet.ReadAscii();
                            //Console.WriteLine("pet ascii2: --> PET: owner: {0}", ascii2);

#if true
                            var splitted = mobinfo.Type.Split('_');
                            var dummy = 0;
                            if (int.TryParse(splitted.Last(), out dummy))
                            {
                                var extraByte = packet.ReadByte();
                                //Console.WriteLine("numeric end.. -> {0} --> read one extra byte !! ==> {1}", mobinfo.Type, extraByte);
                            }
                            
                            packet.ReadUInt32();
                            packet.ReadByte();

                            //log.DebugFormat("{0} | parsed PET: {1} from {3} with name: <{2}>", DateTime.Now.ToString("HH:mm:ss.fff"), mobinfo.Type, ascii1, ascii2);
#else
                            if (mobinfo.Type.StartsWith("COS_P_RAVEN"))
                            {
                                Console.WriteLine("raven");
                                packet.ReadByte();
                            }
                            if (mobinfo.Type.StartsWith("COS_P_WOLF"))
                            {
                                Console.WriteLine("wolf");
                                packet.ReadByte();
                            }
                            if (mobinfo.Type.StartsWith("COS_P_SCARY"))
                            {
                                Console.WriteLine("scary ...");
                                packet.ReadByte();
                            }
                            if (mobinfo.Type.StartsWith("COS_P_BROWNIE"))
                            {
                                Console.WriteLine("brownie need more ??");
                                packet.ReadUInt32();
                                packet.ReadByte();
                            }
                            else
                            {
                                Console.WriteLine("no brownie?!");
                                if (mobinfo.Type.StartsWith("COS_P_JINN") || mobinfo.Type.StartsWith("COS_P_KANGAROO") || mobinfo.Type.StartsWith("COS_P_BEAR") || mobinfo.Type.StartsWith("COS_P_FOX") || mobinfo.Type.StartsWith("COS_P_PENGUIN"))
                                {
                                    Console.WriteLine("extra byte?");
                                    packet.ReadByte();
                                }
                                packet.ReadByte();
                                packet.ReadUInt32();
                            }
#endif
                        }
                    }
                }

                bot.Spawns.Pets.Add(new Pet(mobinfo, pet_id) { X = xas, Y = yas, Distance = dist, Speed = petSpeed, Name = petName, Owner = ownerName });
            }
            catch (Exception ex)
            {
                packet.BaseStream.Seek(0, SeekOrigin.Begin);
                var bytes = packet.ReadBytes((int)packet.BaseStream.Length);
                log.ErrorFormat("error parsing pet({0}/{1}): {2} => {3} ==> {4}", pet_id, mobinfo.Type, String.Join(", ", bytes.Select(b => "0x" + b.ToString("X2"))), ex.Message, ex.StackTrace);
                return false;
            }

            return true;
        }

        public static bool ParseNPC(Packet packet, MobInfo mobinfo, SROBot.Bot bot)
        {
            if (packet == null || mobinfo == null) return false;
            return ParseNPC(new BinaryReader(new MemoryStream(packet.GetBytes().Skip(4).ToArray())), mobinfo, bot);
        }

        public static bool ParseNPC(BinaryReader packet, MobInfo mobinfo, SROBot.Bot bot)
        {
            try
            {
                uint id = packet.ReadUInt32();

                byte xsec = packet.ReadByte();
                byte ysec = packet.ReadByte();
                float xcoord = packet.ReadSingle();
                packet.ReadSingle();
                float ycoord = packet.ReadSingle();

                packet.ReadUInt16(); // Position
                byte move = packet.ReadByte(); // Moving
                packet.ReadByte(); // Running

                if (move == 1)
                {
                    xsec = packet.ReadByte();
                    ysec = packet.ReadByte();
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
                }
                else
                {
                    packet.ReadByte(); // Unknown
                    packet.ReadUInt16(); // Unknwon
                }

                packet.ReadUInt64(); //Unknown
                packet.ReadUInt64(); //Unknown
                ushort check = packet.ReadUInt16();
                if (check != 0)
                {
                    byte count = packet.ReadByte();
                    for (byte i = 0; i < count; i++)
                    {
                        packet.ReadByte();
                    }
                }

                bot.Spawns.Shops.Add(new Mob(mobinfo, id));

                //log.DebugFormat("{0} | parsed NPC: {1}", DateTime.Now.ToString("HH:mm:ss.fff"), mobinfo.Type);
            }
            catch (Exception ex)
            {
                bot.Debug("error parsing npc: {0} => {1}", ex.Message, ex.StackTrace);
                return false;
            }

            return true;
        }

        //public static bool ParsePortal(Packet packet, MobInfo mobinfo, SROBot.Bot bot)
        //{
        //    if (packet == null) return false;
        //    return ParsePortal(new BinaryReader(new MemoryStream(packet.GetBytes().Skip(4).ToArray())), mobinfo, bot);
        //}
        
        public static bool ParsePortal(BinaryReader packet, SROData.Portal portal, SROBot.Bot bot)
        {
            try
            {
                uint id = packet.ReadUInt32();

                byte xsec = packet.ReadByte();
                byte ysec = packet.ReadByte();
                float xcoord = packet.ReadSingle();
                packet.ReadSingle();
                float ycoord = packet.ReadSingle();
                packet.ReadUInt16(); // Position
                packet.ReadUInt32();
                packet.ReadUInt64();

                //log.DebugFormat("{0} | parsed PORTAL", DateTime.Now.ToString("HH:mm:ss.fff"));

                portal.IngameId = id;
                bot.Spawns.Gates.Add(portal);
            }
            catch (Exception ex)
            {
                bot.Debug("error parsing portal: {0} => {1}", ex.Message, ex.StackTrace);
                return false;
            }

            return true;
        }

        public static bool ParseChar(Packet packet, MobInfo mobinfo, SROBot.Bot bot)
        {
            if (packet == null || mobinfo == null) return false;
            return ParseChar(new BinaryReader(new MemoryStream(packet.GetBytes().Skip(4).ToArray())), mobinfo, bot);
        }

        public static bool ParseChar(BinaryReader packet, MobInfo mobinfo, SROBot.Bot bot)
        {
            try
            {
                int trade = 0;
                int stall = 0;
                packet.ReadByte(); // Volume/Height
                packet.ReadByte(); // Rank
                packet.ReadByte(); // Icons
                packet.ReadByte(); // Unknown
                packet.ReadByte(); // Max Slots
                int items_count = packet.ReadByte();
                var items = new List<SROBot.InventoryItem>();
                for (int a = 0; a < items_count; a++)
                {
                    uint itemid = packet.ReadUInt32();
                    var iteminfo = ItemInfos.GetById(itemid);
                    byte plus = 0;

                    if (iteminfo == null)
                    {
                        bot.Log($"itemid: {itemid} ==> iteminfo == null !!");
                        return false;
                    }

                    if (iteminfo.Type == null)
                    {
                        bot.Log($"itemid: {itemid} ==> iteminfo.Type == null !!");
                        return false;
                    }
                    
                    if (iteminfo.Type.StartsWith("ITEM_CH") || iteminfo.Type.StartsWith("ITEM_EU") || iteminfo.Type.StartsWith("ITEM_FORT") || iteminfo.Type.StartsWith("ITEM_ROC_CH") || iteminfo.Type.StartsWith("ITEM_ROC_EU"))
                    {
                        plus = packet.ReadByte(); // Item Plus
                    }
                    if (iteminfo.Type.StartsWith("ITEM_EU_M_TRADE") || iteminfo.Type.StartsWith("ITEM_EU_F_TRADE") || iteminfo.Type.StartsWith("ITEM_EU_W_TRADE") ||
                        iteminfo.Type.StartsWith("ITEM_CH_M_TRADE") || iteminfo.Type.StartsWith("ITEM_CH_F_TRADE") || iteminfo.Type.StartsWith("ITEM_CH_W_TRADE"))
                    {
                        trade = 1;
                    }
                    //Console.WriteLine("slot {0}: {1}/{2} (+{3})", a, iteminfo.Type, iteminfo.Name, plus);

                    iteminfo.Plus = plus;

                    var item = new SROBot.InventoryItem((byte)a, itemid, iteminfo, 1);
                    items.Add(item);
                }
                packet.ReadByte(); // Max Avatars Slot
                int avatar_count = packet.ReadByte();
                for (int a = 0; a < avatar_count; a++)
                {
                    uint avatarid = packet.ReadUInt32();
                    
                    var avatar = ItemInfos.GetById(avatarid);
                    byte plus = packet.ReadByte(); // Avatar Plus
                    if (avatar == null)
                    {
                        bot.Debug("cant find avatar!!");
                    }
                }
                int mask = packet.ReadByte();
                if (mask == 1)
                {
                    uint id = packet.ReadUInt32();
                    var _mobinfo = MobInfos.GetById(id);
                    if (_mobinfo.Type.StartsWith("CHAR"))
                    {
                        packet.ReadByte();
                        byte count = packet.ReadByte();
                        for (int i = 0; i < count; i++)
                        {
                            packet.ReadUInt32();
                        }
                    }
                }
                var charId = packet.ReadUInt32();
                //Console.WriteLine("char with id {0} -- trading: {1}", charId, trade);

                byte xsec = packet.ReadByte();
                byte ysec = packet.ReadByte();
                float xcoord = packet.ReadSingle();
                packet.ReadSingle();
                float ycoord = packet.ReadSingle();

                packet.ReadUInt16(); // Position
                byte move = packet.ReadByte(); // Moving
                packet.ReadByte(); // Running

                if (move == 1)
                {
                    xsec = packet.ReadByte();
                    ysec = packet.ReadByte();
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
                }
                else
                {
                    packet.ReadByte(); // No Destination
                    packet.ReadUInt16(); // Angle
                }
                
                int xas = Movement.CalculatePositionX(xsec, xcoord);
                int yas = Movement.CalculatePositionY(ysec, ycoord);
                int dist = Movement.GetDistance(xas, bot.Char.CurPosition.X, yas, bot.Char.CurPosition.Y);
                
                var isCharAlive = packet.ReadByte(); // Alive
                packet.ReadByte(); // Unknown
                packet.ReadByte(); // Unknown
                packet.ReadByte(); // Unknown

                packet.ReadUInt32(); // Walking speed
                packet.ReadUInt32(); // Running speed
                packet.ReadUInt32(); // Berserk speed

                int active_skills = packet.ReadByte(); // Buffs count
                //Console.WriteLine("char active buffs: {0}", active_skills);
                
                for (int a = 0; a < active_skills; a++)
                {
                    uint skillid = packet.ReadUInt32();
                    var skillinfo = SkillInfos.GetByModel(skillid);
                    if (skillinfo == null)
                    {
                        throw new Exception(String.Format("unknown skill/buff: {0}", skillid));
                    }

                    var type = skillinfo.Type ?? "";
                    var duration = packet.ReadUInt32(); // Temp ID

                    //Console.WriteLine("buf: {0}/{1}, duration: {2}", skillid, skillinfo.Name, duration);
                    if (type.StartsWith("SKILL_EU_CLERIC_RECOVERYA_GROUP") || type.StartsWith("SKILL_EU_BARD_BATTLAA_GUARD") || type.StartsWith("SKILL_EU_BARD_DANCEA") || type.StartsWith("SKILL_EU_BARD_SPEEDUPA_HITRATE"))
                    {
                        var caster = packet.ReadByte();
                        //Console.WriteLine("read an additional byte?! => {0}", caster);
                        switch (caster)
                        {
                            case 1: // caster
                                break;
                            case 2: // not the caster
                                break;
                        }
                    }
                }
                string name = packet.ReadAscii();
                
                packet.ReadByte(); // Unknown
                packet.ReadByte(); // Job type
                packet.ReadByte(); // Job level
                int cnt = packet.ReadByte();
                packet.ReadByte();
                if (cnt == 1)
                {
                    packet.ReadUInt32();
                }
                packet.ReadByte(); // Unknown
                stall = packet.ReadByte(); // Stall flag
                packet.ReadByte(); // Unknown
                string guild = packet.ReadAscii(); // Guild
                string grantname = "";
                var stalling = false;
                var stalltitle = "";

                if (trade == 1)
                {
                    packet.ReadUInt16();
                }
                else
                {
                    packet.ReadUInt32(); // Guild ID
                    grantname = packet.ReadAscii(); // Grant Name
                    packet.ReadUInt32();
                    packet.ReadUInt32();
                    packet.ReadUInt32();
                    packet.ReadUInt16();
                    if (stall == 4)
                    {
                        stalling = true;
                        stalltitle = packet.ReadAscii();
                        packet.ReadUInt32();
                        packet.ReadUInt16();
                    }
                    else
                    {
                        packet.ReadUInt16();
                    }
                }

                bot.Spawns.Player.Add(new SROBot.Spawn.Player(charId, name)
                {
                    X = xas,
                    Y = yas,
                    Distance = dist,
                    Guild = guild,
                    IsAlive = isCharAlive == 1,
                    Items = items.ToArray(),
                    Job = trade == 1,
                    Nick = grantname,
                    IsStalling = stalling,
                    StallTitle = stalltitle
                });

                if (items.Any(i => i.Iteminfo.Plus >= 35)) // its allways with ADV included..
                {
                    items.Where(i => i.Iteminfo.Plus >= 35).ToList().ForEach(i => bot.Debug("{0}{1} has a nice item: {2} => {3}", trade == 1 ? "*" : "", name, i.Iteminfo.Type, i.Iteminfo.Plus));
                }

                //log.DebugFormat("{0} | parsed CHAR: {1}/{2}{3} from guild: <{4}>", DateTime.Now.ToString("HH:mm:ss.fff"), mobinfo.Type, trade == 1 ? "*" : "", name, guild);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("error parsing player: {0} => {1}", ex.Message, ex.StackTrace);
                return false;
            }

            return true;
        }

        public static bool ParseOther(Packet packet)
        {
            if (packet == null) return false;
            return ParseOther(new BinaryReader(new MemoryStream(packet.GetBytes().Skip(4).ToArray())));
        }

        public static bool ParseOther(BinaryReader packet)
        {
            try
            {
                packet.ReadUInt32(); // MOB ID
                packet.ReadByte();
                packet.ReadByte();
                packet.ReadSingle();
                packet.ReadSingle();
                packet.ReadSingle();
                packet.ReadUInt16(); // Position
                packet.ReadByte(); // Unknwon
                packet.ReadByte(); // Unknwon
                packet.ReadUInt16(); // Unknwon
                packet.ReadAscii();
                packet.ReadUInt32();
            }
            catch (Exception ex)
            {
                //sroBot.("error parsing other");
                return false;
            }

            return true;
        }

        public static void ParseCircle(BinaryReader packet)
        {
            var unk1 = packet.ReadUInt16();
            var skillModel = packet.ReadUInt32();

            var skill = SkillInfos.GetByModel(skillModel);
            if (skill == null) return;
            
            var id = packet.ReadUInt32();
            var xsec = packet.ReadByte();
            var ysec = packet.ReadByte();
            var x = packet.ReadSingle();
            var z = packet.ReadSingle();
            var y = packet.ReadSingle();

            // 2 byte unknown here

            //Console.WriteLine("{0} | circle: {1} @ {2}/{3} -- id: {4}", DateTime.Now.ToString("HH:mm:ss.fff"), skill.Name, Movement.CalculatePositionX(xsec, x), Movement.CalculatePositionY(ysec, y), id);
        }
    }
}
