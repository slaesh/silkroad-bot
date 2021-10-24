using SilkroadSecurityApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    class Actions
    {
        public static void CreateCharacter(Bot bot, string name, UInt32 charModel, byte height, byte volume, UInt32 chestModel, UInt32 legsModel, UInt32 bootsModel, UInt32 weapModel)
        {
            var packet = new Packet(0x7007, false);

            packet.WriteUInt8(1); // CREATE CHAR
            if (name.Length > 12)
            {
                name = name.Substring(0, 12);
            }
            packet.WriteAscii(name);
            packet.WriteUInt32(charModel);
            packet.WriteUInt8(0); // HEIGHT AND VOLUME ?! IDK HOW ..
            packet.WriteUInt32(chestModel);
            packet.WriteUInt32(legsModel);
            packet.WriteUInt32(bootsModel);
            packet.WriteUInt32(weapModel);

            bot.SendToSilkroadServer(packet);
        }

        public static void IncreaseSTR(SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INCSTR, false);
            bot.SendToSilkroadServer(packet);
        }

        public static void IncreaseINT(SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INCINT, false);
            bot.SendToSilkroadServer(packet);
        }

        public static void UseReturnScroll(byte slot, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYUSE, true);
            packet.WriteUInt8(slot);
            packet.WriteUInt8(0xEC);
            packet.WriteUInt8(0x09);
            bot.SendToSilkroadServer(packet);
        }

        public static void UseReverseReturnScroll(byte slot, byte destination, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYUSE, true);
            packet.WriteUInt8(slot);
            packet.WriteUInt8(0xEC);
            packet.WriteUInt8(0x19);
            packet.WriteUInt8(destination);
            bot.SendToSilkroadServer(packet);
        }

        public static void UseDmgScroll(InventoryItem item, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYUSE, true);
            packet.WriteUInt8(item.Slot);
            if (item.Iteminfo.Type.StartsWith("ITEM_EVENT")) packet.WriteUInt8(0xec);
            else packet.WriteUInt8(0xed);
            packet.WriteUInt8(0x0e);
            bot.SendToSilkroadServer(packet);
        }

        public static void UseTriggerScroll(byte slot, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYUSE, true);
            packet.WriteUInt8(slot);
            packet.WriteUInt8(0xec);
            packet.WriteUInt8(0x0e);
            bot.SendToSilkroadServer(packet);
        }

        public static void UseResurrectionScroll(Bot bot, byte slot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYUSE, true);
            packet.WriteUInt8(slot);
            packet.WriteUInt8(0xec);
            packet.WriteUInt8(0x36);
            bot.SendToSilkroadServer(packet);
        }

        public static void PickUp(uint itemId, SROBot.Bot bot, bool pickWithPet = false)
        {
            Packet packet;
            if (pickWithPet && bot.Char.Pickpet != null)
            {
                packet = new Packet((ushort)SROData.Opcodes.CLIENT.PETACTION, false);
                packet.WriteUInt32(bot.Char.Pickpet.UID);
                packet.WriteUInt8(0x08); //Pickup
                packet.WriteUInt32(itemId);
            }
            else
            {
                packet = new Packet((ushort)SROData.Opcodes.CLIENT.OBJECTACTION, false);
                packet.WriteInt8(0x01);
                packet.WriteInt8(0x02);
                packet.WriteInt8(0x01);
                packet.WriteUInt32(itemId);
            }
            bot.SendToSilkroadServer(packet);
        }

        public static void UseZerk(SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.ZERK, false);
            packet.WriteUInt8(0x01);
            bot.SendToSilkroadServer(packet);
        }

        public static void Select(uint id, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.OBJECTSELECT, false);
            packet.WriteUInt32(id);
            bot.SendToSilkroadServer(packet);
        }

        public static void Attack(uint? id, SROBot.Bot bot)
        {
            if (id == 0 || id == null) return;
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.OBJECTACTION, false);
            packet.WriteUInt8(0x01);
            packet.WriteUInt8(0x01);
            packet.WriteUInt8(0x01);
            packet.WriteUInt32(id);
            bot.SendToSilkroadServer(packet);
        }

        public static void CastImbue(uint model, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.OBJECTACTION, false);
            packet.WriteUInt8(0x01);
            packet.WriteUInt8(0x04);
            packet.WriteUInt32(model);
            packet.WriteUInt8(0x00);
            bot.SendToSilkroadServer(packet);
        }

        public static bool CastSkill(uint model, uint target, SROBot.Bot bot)
        {
            //if (BotData.GetMob(mob_id) != null)
            {
                Packet packet = new Packet((ushort)SROData.Opcodes.CLIENT.OBJECTACTION, false);
                packet.WriteUInt8(0x01);
                packet.WriteUInt8(0x04);
                packet.WriteUInt32(model);
                packet.WriteUInt8(0x01);
                packet.WriteUInt32(target);
                bot.SendToSilkroadServer(packet);
                return true;
            }
            return false;
        }

        public static void CastBuff(uint model, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.OBJECTACTION, false);
            packet.WriteUInt8(1);
            packet.WriteUInt8(4);
            packet.WriteUInt32(model);
            packet.WriteUInt8(0);

            bot.SendToSilkroadServer(packet);
        }

        public static bool BuyItem(uint npcId, ItemInfo iteminfo, ushort count, SROBot.Bot bot)
        {
            var npc = bot.Spawns.Shops.Get(npcId);
            if (npc == null) return false;
            var shop = SROData.NPCs.GetByModel(npc.Mobinfo.Model);
            if (shop == null) return false;
            var tab = shop.Tabs.FirstOrDefault(t => t.ItemModels.Any(i => i.Model == iteminfo.Model));
            if (tab == null) return false;

            var tabItem = tab.ItemModels.FirstOrDefault(i => i.Model == iteminfo.Model);

            if (count > iteminfo.StackSize)
                count = (ushort)iteminfo.StackSize;

            bot.Log("buy {0} from tab {1} and item idx {2} -> {3} pcs", iteminfo.Type, (byte)Array.IndexOf(shop.Tabs, tab), tabItem.IndexOfTab, count);

            BuyItem(npcId, (byte)Array.IndexOf(shop.Tabs, tab), tabItem.IndexOfTab, count, bot);
            return true;
        }

        public static void BuyItem(uint npcId, byte tab, byte slot, ushort count, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYMOVEMENT, false);
            packet.WriteUInt8(0x08);
            packet.WriteUInt8(tab);
            packet.WriteUInt8(slot);
            packet.WriteUInt16(count);
            packet.WriteUInt32(npcId);

            bot.SendToSilkroadServer(packet);
        }

        public static void SellItem(byte slot, ushort count, uint npcId, SROBot.Bot bot)
        {
            //Console.WriteLine("{0} | try sell slot {1} with count: {2} to npc: {3}", DateTime.Now.ToString("HH:mm:ss.fff"), slot, count, npcId);

            Packet packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYMOVEMENT, false);
            packet.WriteUInt8(0x09); //Sell
            packet.WriteUInt8(slot); //That says everything
            packet.WriteUInt16(count); //Hmmm ?
            packet.WriteUInt32(npcId); //NPC ID

            bot.SendToSilkroadServer(packet);
        }

        public static void RepairAll(uint npcId, SROBot.Bot bot)
        {
            Packet packet = new Packet((ushort)SROData.Opcodes.CLIENT.REPAIR, false);
            packet.WriteUInt32(npcId); //NPC ID
            packet.WriteUInt8(2); //

            bot.SendToSilkroadServer(packet);
        }

        public static void NPCSelect(uint npcId, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.OBJECTSELECT, false);
            packet.WriteUInt32(npcId);

            bot.SendToSilkroadServer(packet);
        }

        public static void GetStorageList(uint npcId, Bot bot)
        {
            var packet = new Packet(0x703c, false);
            packet.WriteUInt32(npcId);
            packet.WriteUInt8(0);

            bot.SendToSilkroadServer(packet);
        }

        public static void OpenGuildStorage(uint npcId, Bot bot)
        {
            var packet = new Packet(0x7250, false);
            packet.WriteUInt32(npcId);

            bot.SendToSilkroadServer(packet);
        }

        public static void GetGuildStorageList(uint npcId, Bot bot)
        {
            var packet = new Packet(0x7252, false);
            packet.WriteUInt32(npcId);

            bot.SendToSilkroadServer(packet);
        }

        public static void CloseGuildStorage(uint npcId, Bot bot)
        {
            var packet = new Packet(0x7251, false);
            packet.WriteUInt32(npcId);

            bot.SendToSilkroadServer(packet);
        }

        public static void PutItemToGuildStorage(uint npcId, byte inventorySlot, Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYMOVEMENT, false);
            packet.WriteUInt8(0x1e);
            packet.WriteUInt8(inventorySlot);
            packet.WriteUInt8(1 /* target storage slot */);
            packet.WriteUInt32(npcId);

            bot.SendToSilkroadServer(packet);
        }

        public static void PutItemToStorage(uint npcId, byte inventorySlot, Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYMOVEMENT, false);
            packet.WriteUInt8(2);
            packet.WriteUInt8(inventorySlot);
            packet.WriteUInt8(1 /* target storage slot */);
            packet.WriteUInt32(npcId);

            bot.SendToSilkroadServer(packet);
        }

        public static void GuildStorageOpen(uint npcId, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.NPCSELECT, false);
            packet.WriteUInt32(npcId); //NPC ID
            packet.WriteUInt8(0xf);

            bot.SendToSilkroadServer(packet);
        }

        public static void StorageOpen(uint npcId, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.NPCSELECT, false);
            packet.WriteUInt32(npcId); //NPC ID
            packet.WriteUInt8(3);

            bot.SendToSilkroadServer(packet);
        }

        public static void ConsignmentGetMyItemList(Bot bot)
        {
            var packet = new Packet(0x750E);
            bot.SendToSilkroadServer(packet);
        }

        public static void ConsignmentOpen(Bot bot, UInt32 npcId)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.NPCSELECT, false);
            packet.WriteUInt32(npcId);
            packet.WriteUInt8(0x23);

            bot.SendToSilkroadServer(packet);
        }

        public static void ConsignmentClose(Bot bot)
        {
            var packet = new Packet(0x7507);
            bot.SendToSilkroadServer(packet);
        }

        public static void ConsignmentSettle(Bot bot)
        {
            if (!bot.ConsignmentItems.Any(ci => ci.Sold)) return;

            var itemsToSettle = bot.ConsignmentItems.Where(ci => ci.Sold).ToArray();
            var packet = new Packet(0x750B);

            packet.WriteUInt8(itemsToSettle.Length);

            foreach (var item in itemsToSettle)
            {
                bot.Debug($"settle {item.ConsigId} with count: {item.Count}");

                packet.WriteUInt32(item.ConsigId);
            }

            bot.SendToSilkroadServer(packet);

            bot.Debug($"settle cmd   : {string.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2")))}");
        }

        public static void ConsignmentAbortItem(Bot bot, Consignment.ConsignmentItem item)
        {
            if (item == null) return;

            var packet = new Packet(0x7509);
            
            packet.WriteUInt8(1); // FIX OR ABORT-ITEM CNT?! --> MAYBE WE CAN ABORT MULTIPLE ITEMS..
            packet.WriteUInt32(item.ConsigId);

            bot.SendToSilkroadServer(packet);
        }

        public static void ConsignmentRegisterItem(Bot bot, InventoryItem item, UInt64 price)
        {
            if (item == null) return;

            var packet = new Packet(0x7508);

            packet.WriteUInt8(1); // FIX OR ITEM CNT?! --> MAYBE WE CAN ABORT MULTIPLE ITEMS..

            packet.WriteUInt8(item.Slot);
            packet.WriteUInt16(item.Count);
            packet.WriteUInt16(0); // ???
            packet.WriteUInt32(item.Iteminfo.Model);
            packet.WriteUInt8(item.Iteminfo.TypeIdGroup); // --> 4 bytes big?
            packet.WriteUInt8(0); // ???
            packet.WriteUInt8(0); // ???
            packet.WriteUInt8(0); // ???
            packet.WriteUInt64(price);

            bot.SendToSilkroadServer(packet);
        }

        public static void FakeItemPickUp(Bot bot, InventoryItem item)
        {
            if (bot.Clientless) return;
            if (item == null) return;

            var fakePickUp = new Packet((ushort)SROData.Opcodes.SERVER.INVENTORYMOVEMENT, false);

            fakePickUp.WriteUInt8(0x01);
            fakePickUp.WriteUInt8(0x06);
            fakePickUp.WriteUInt8(item.Slot);
            fakePickUp.WriteUInt32(0x00000000);
            fakePickUp.WriteUInt32(item.Iteminfo.Model);
            if (item.Iteminfo.Type.StartsWith("ITEM_CH") == false && item.Iteminfo.Type.StartsWith("ITEM_EU") == false)
            {
                fakePickUp.WriteUInt16(item.Count);
            }
            else
            {
                fakePickUp.WriteUInt8(0x00);
                fakePickUp.WriteUInt64(0x0000000000000000);
                fakePickUp.WriteUInt32(item.Durability);
                fakePickUp.WriteUInt8(0x00);
                fakePickUp.WriteUInt16(1);
                fakePickUp.WriteUInt16(2);
            }

            bot.SendToSilkroadClient(fakePickUp);
        }

        public static void FakeItemDrop(Bot bot, byte slot)
        {
            if (bot.Clientless) return;

            var packet = new Packet(0xb034);
            
            packet.WriteUInt8(1); // success
            packet.WriteUInt8(7);
            packet.WriteUInt8(slot);

            bot.SendToSilkroadClient(packet);
        }

        public static void NPCOpen(uint npcId, SROBot.Bot bot)
        {
            NPCDeselect(npcId, bot);

            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.NPCSELECT, false);
            packet.WriteUInt32(npcId); //NPC ID
            packet.WriteUInt8(1);

            bot.SendToSilkroadServer(packet);
        }

        public static void NPCDeselect(uint npcId, SROBot.Bot bot)
        {
            Packet packet = new Packet((ushort)SROData.Opcodes.CLIENT.NPCDESELECT, false);
            packet.WriteUInt32(npcId); //NPC ID

            bot.SendToSilkroadServer(packet);
        }

        public static void NPCTalkTo(uint npcId, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.NPCSELECT, false);
            packet.WriteUInt32(npcId); //NPC ID
            packet.WriteUInt8(2);

            bot.SendToSilkroadServer(packet);
        }

        public static void NPCEventExchange(byte selection, SROBot.Bot bot)
        {
            var packet = new Packet(0x30D4, false);
            packet.WriteUInt8(selection);

            bot.SendToSilkroadServer(packet);
        }

        public static void UpMastery(uint mastery, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.UPMASTERY, false);
            packet.WriteUInt32(mastery);
            packet.WriteUInt8(0); // levels to increase .. NORMALLY 1, higher than 1 will not work.. BUT 0 increases by 1 BUT dont consume SPs !! :)
            bot.SendToSilkroadServer(packet);
        }

        public static void InvicibleMode(SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.ZERK, false);

            /*
             
             yes it still works on almost all servers

            opcode is 70A7

            01 00 A7 70 00 00 0X

            02 = 'can't attack for 5 seconds'
            03 = invincible
            04 = invisible

             */

            packet.WriteUInt8(3);
            bot.SendToSilkroadServer(packet);
        }

        public static void SearchConsignment(SROBot.Bot bot, byte cmd, byte tidGroupId, byte degree, byte page = 0)
        {
            var packet = new Packet(0x750C);
            
            packet.WriteUInt8(cmd); // cmd: search .. normally the first time its value is "1" ??! is it neccassary?
            packet.WriteUInt8(page); // page: 0 -- only need to put a number on cmd(3): paging
            packet.WriteUInt8(tidGroupId);
            packet.WriteUInt8(0);
            packet.WriteUInt8(0);
            packet.WriteUInt8(0);
            packet.WriteUInt8(degree);
            packet.WriteUInt8(0);
            packet.WriteUInt8(0);

            bot.SendToSilkroadServer(packet);
        }

        public static void InvisibleMode(SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.ZERK, false);

            /*
             
             yes it still works on almost all servers

            opcode is 70A7

            01 00 A7 70 00 00 0X

            02 = 'can't attack for 5 seconds'
            03 = invincible
            04 = invisible

             */
             
            packet.WriteUInt8(4);
            bot.SendToSilkroadServer(packet);
        }

        public static void UpSkill(uint skill, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.UPSKILL, false);
            packet.WriteUInt32(skill);
            bot.SendToSilkroadServer(packet);
        }

        public static void AcceptDead(SROBot.Bot bot, int type = 1 /* 1=back town, 2=ressurect present place*/)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.ACCEPTDEAD, false);
            packet.WriteUInt8(type);
            bot.SendToSilkroadServer(packet);
        }

        public static void MergeItems(byte fromSlot, byte toSlot, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYMOVEMENT, false);
            packet.WriteUInt8(0x00);
            packet.WriteUInt8(toSlot);
            packet.WriteUInt8(fromSlot);
            packet.WriteUInt8(toSlot); // ?? target slot ??
            packet.WriteUInt16(0x0000);

            bot.SendToSilkroadServer(packet);
        }

        public static void MergeStorageItems(UInt32 storageId, InventoryItem from, InventoryItem to, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYMOVEMENT, false);

            packet.WriteUInt8(0x01);
            packet.WriteUInt8(from.Slot);
            packet.WriteUInt8(to.Slot);
            packet.WriteUInt16(from.Count);
            packet.WriteUInt32(storageId);

            bot.SendToSilkroadServer(packet);
        }

        public static void SwapItems(byte fromSlot, byte toSlot, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYMOVEMENT, false);
            packet.WriteUInt8(0x00);
            packet.WriteUInt8(fromSlot);
            packet.WriteUInt8(toSlot);
            packet.WriteUInt16(0x0000);

            bot.SendToSilkroadServer(packet);
        }

        public static void PetToInventory(byte petSlot, SROBot.Bot bot)
        {
            if (bot.Char == null || bot.Char.Pickpet == null) return;

            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYMOVEMENT, false);
            packet.WriteUInt8(0x1A); // pet to inventory
            packet.WriteUInt32(bot.Char.Pickpet.UID);
            packet.WriteUInt8(petSlot);
            packet.WriteUInt8(0x0d); // just use first inventory slot --> will be placed to the next free slot !

            bot.SendToSilkroadServer(packet);
        }

        public static void InventoryToPet(byte invSlot, SROBot.Bot bot)
        {
            if (bot.Char == null || bot.Char.Pickpet == null || bot.Char.Pickpet.Inventory.IsFull()) return;

            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYMOVEMENT, false);
            packet.WriteUInt8(0x1B); // inventory to pet
            packet.WriteUInt32(bot.Char.Pickpet.UID);
            packet.WriteUInt8(invSlot);
            packet.WriteUInt8(bot.Char.Pickpet.Inventory.FirstFreeSlot());

            bot.SendToSilkroadServer(packet);
        }

        public static void SetDesignatePoint(Bot bot, UInt32 portalId)
        {
            if (bot == null) return;

            var packet = new Packet(0x7059);
            packet.WriteUInt32(portalId);

            bot.SendToSilkroadServer(packet);
        }

        public static void Teleport(uint portalId, byte type, uint destination, SROBot.Bot bot)
        {
            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.TELEPORT);
            packet.WriteUInt32(portalId);
            packet.WriteUInt8(type);
            packet.WriteUInt32(destination);

            bot.SendToSilkroadServer(packet);
        }

        public static void AcceptPartyRequest(SROBot.Bot bot, bool value = true)
        {
            var packet = new Packet(0x3080);
            if (value)
            {
                packet.WriteUInt8(1);
                packet.WriteUInt8(1);
            }
            else
            {
                packet.WriteUInt8(1);
                packet.WriteUInt8(0);
            }

            bot.SendToSilkroadServer(packet);
        }

        public static void AcceptExchange(SROBot.Bot bot, bool accept, bool firstTime)
        {
            if (accept)
            {
                if (firstTime)
                {
                    var packet = new Packet(0x7082, true);
                    bot.SendToSilkroadServer(packet);
                }
                else
                {
                    var packet = new Packet(0x7083, true);
                    bot.SendToSilkroadServer(packet);
                }
            }
            else
            {
                var packet = new Packet(0x7084, true);
                bot.SendToSilkroadServer(packet);
            }
        }

        public static void LeaveParty(SROBot.Bot bot)
        {
            var packet = new Packet(0x7061);
            bot.SendToSilkroadServer(packet);
        }

        public static void InviteToParty(uint playerId, byte type, bool create, SROBot.Bot bot)
        {
            Packet packet;
            if (create)
            {
                packet = new Packet(0x7060);
                packet.WriteInt32(playerId);
                packet.WriteInt8(type);
            }
            else
            {
                packet = new Packet(0x7062);
                packet.WriteInt32(playerId);
            }

            bot.SendToSilkroadServer(packet);
        }

        public static void FuseItem(SROBot.Bot bot, byte itemSlot, byte elixierSlot, byte luckyPowderSlot = 0)
        {
            var packet = new Packet(0x7150);
            packet.WriteUInt8(2);
            packet.WriteUInt8(3);

            if (luckyPowderSlot == 0)
            {
                packet.WriteUInt8(2);
            }
            else
            {
                packet.WriteUInt8(3);
            }

            packet.WriteUInt8(itemSlot);
            packet.WriteUInt8(elixierSlot);

            if (luckyPowderSlot != 0)
            {
                packet.WriteUInt8(luckyPowderSlot);
            }

            bot.SendToSilkroadServer(packet);
        }

        public static void AddStone(SROBot.Bot bot, byte itemSlot, byte stoneSlot)
        {
            var packet = new Packet(0x7151);
            packet.WriteUInt8(2);
            packet.WriteUInt8(4);
            packet.WriteUInt8(2);

            packet.WriteUInt8(itemSlot);
            packet.WriteUInt8(stoneSlot);

            bot.SendToSilkroadServer(packet);
        }

        public static void CreateStall(SROBot.Bot bot, string title, string msg)
        {
            var packet = new Packet(0x70b1);

            packet.WriteAscii(title);

            bot.SendToSilkroadServer(packet);

            SetStallMessage(bot, msg);
        }

        public static void SetStallMessage(SROBot.Bot bot, string msg)
        {
            var packet = new Packet(0x70ba);

            packet.WriteUInt8(6); // ?!
            packet.WriteAscii(msg);

            bot.SendToSilkroadServer(packet);
        }

        public static void CloseStall(Bot bot)
        {
            var packet = new Packet(0x70B2);
            bot.SendToSilkroadServer(packet);
        }

        public static void PutItemToStall(Bot bot, InventoryItem item, byte stallSlot, UInt64 price)
        {
            var packet = new Packet(0x70BA);

            packet.WriteUInt8(2); // put item cmd
            packet.WriteUInt8(stallSlot);
            packet.WriteUInt8(item.Slot);

            packet.WriteUInt16(item.Count);
            packet.WriteUInt64(price);
            packet.WriteUInt8(item.Iteminfo.TypeIdGroup);

            packet.WriteUInt8(0x00); // FIX
            packet.WriteUInt8(0x00); // FIX
            packet.WriteUInt8(0x00); // FIX
            packet.WriteUInt8(0x00); // FIX
            packet.WriteUInt8(0x00); // FIX

            bot.SendToSilkroadServer(packet);
        }

        public static void OpenStall(Bot bot)
        {
            var packet = new Packet(0x70BA);
            packet.WriteUInt8(5);
            packet.WriteUInt8(1);
            packet.WriteUInt8(0);
            packet.WriteUInt8(0);
            bot.SendToSilkroadServer(packet);
        }

        public static void ModifyStall(Bot bot)
        {
            var packet = new Packet(0x70BA);
            packet.WriteUInt8(5);
            packet.WriteUInt8(0);
            packet.WriteUInt8(0);
            packet.WriteUInt8(0);
            bot.SendToSilkroadServer(packet);
        }
    }
}
