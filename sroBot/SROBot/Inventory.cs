using log4net;
using SilkroadSecurityApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace sroBot.SROBot
{

    public enum ITEM_GROUP_TYPES
    {
        CH_WEAPON_SWORD = 1,
        CH_WEAPON_BLADE = 2,
        CH_WEAPON_SPEAR = 3,
        CH_WEAPON_TBLADE = 4,
        CH_WEAPON_BOW = 5,
        CH_WEAPON_SHIELD = 6,
        CH_ARMOR_HEAVY = 7,
        CH_ARMOR_LIGHT = 8,
        CH_ARMOR_CLOTHES = 9,
        CH_ACCESSORY_NECKLACE = 10,
        CH_ACCESSORY_EARRING = 11,
        CH_ACCESSORY_RING = 12,
        EU_WEAPON_SWORD = 13,
        EU_WEAPON_TSWORD = 14,
        EU_WEAPON_DUALAXE = 15,
        EU_WEAPON_WROD = 16,
        EU_WEAPON_CROD = 17,
        EU_WEAPON_TSTAFF = 18,
        EU_WEAPON_CROSSBOW = 19,
        EU_WEAPON_DAGGER = 20,
        EU_WEAPON_HARP = 21,
        EU_WEAPON_SHIELD = 22,
        EU_ARMOR_HEAVY = 23,
        EU_ARMOR_LIGHT = 24,
        EU_ARMOR_ROBE = 25,
        EU_ACCESSORY_NECKLACE = 26,
        EU_ACCESSORY_EARRING = 27,
        EU_ACCESSORY_RING = 28,
        COSTUME_TRIANGLE = 29,
        COSTUME_AVATAR = 30,
        COSTUME_ETC = 31,
        COSTUME_NASRUN = 32,
        ALCHEMY_ELIXIR = 33,
        SOCKET_STONE = 34,
        ALCHEMY_ELEMENT = 35,
        ALCHEMY_MAGICSTONE = 36,
        ALCHEMY_ATTRSTONE = 37,
        ALCHEMY_TABLET = 38,
        ALCHEMY_MATERIAL = 39,
        COS_COS = 40,
        COS_ETC = 41,
        ARTICLES_RECOVERY = 42,
        ARTICLES_CURE = 43,
        ARTICLES_RETURN = 44,
        ARTICLES_FORTIFY = 45,
        ARTICLES_ARROW = 46,
        GUILD_ITEM = 47,
        FORTWAR_WEAPON = 48,
        FORTWAR_ITEM = 49,
        EXCHANGE_COIN = 50,
        EVENT_ITEM = 51,
        ETC_CHANGE = 52,
        ETC_SPECIAL = 53,
        ETC_SKILL = 54,
        ETC_CHAT = 55,
        ETC_REPAIR = 56,
        ETC_ETC = 57,
        NEW_TRADE_WEAPON = 58,
        NEW_TRADE_ARMOR = 59,
        NEW_TRADE_ACCESSORY = 60,
        NEW_TRADE_ALCHEMY = 61,
        NEW_TRADE_ETC = 62,
        ALCHEMY_UPGRADE = 63,
        GOLD = 64,
        ETC_FIREWORK = 65,
        ETC_CAMPFIRE = 66,
        ETC_TRADE_ITEM = 67,
        None,
        COSTUME_FRPVP = 999,
    }

    public class InventoryItem : MVVM.ViewModelBase
    {
        public uint UID;
        public byte Slot
        {
            get { return GetValue(() => Slot); }
            set { SetValue(() => Slot, value); }
        }
        public ItemInfo Iteminfo { get; set; }
        public uint Durability;

        public ushort Count
        {
            get { return GetValue(() => Count); }
            set { SetValue(() => Count, value); }
        }

        public bool Summoned
        {
            get { return GetValue(() => Summoned); }
            set { SetValue(() => Summoned, value); }
        }

        public ItemStats.WhiteStats WhiteStats { get; set; }
        public Dictionary<SROData.MagicOption, uint> BlueStats = new Dictionary<SROData.MagicOption, uint>();

        //public bool IsWebShopItem => WhiteStats != null && WhiteStats.

        public InventoryItem(byte slot, uint uid, ItemInfo iteminfo, ushort count, ulong whiteStats = 0)
        {
            Slot = slot;
            UID = uid;
            Iteminfo = iteminfo;
            Count = count;
            Summoned = false;

            if (iteminfo != null)
            {
                if (iteminfo.IsWeapon)
                {
                    WhiteStats = new ItemStats.WhiteStats(ItemStats.WhiteStats.Types.Weapon, whiteStats);
                }
                if (iteminfo.IsShield)
                {
                    WhiteStats = new ItemStats.WhiteStats(ItemStats.WhiteStats.Types.Shield, whiteStats);
                }
                else if (iteminfo.IsArmor)
                {
                    WhiteStats = new ItemStats.WhiteStats(ItemStats.WhiteStats.Types.Equipment, whiteStats);
                }
                else if (iteminfo.IsAccessory)
                {
                    WhiteStats = new ItemStats.WhiteStats(ItemStats.WhiteStats.Types.Accessory, whiteStats);
                }
            }
        }

        /// <summary>
        /// Check if THIS item is better than the other one.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsBetterThan(InventoryItem item)
        {
            return Iteminfo.IsBetterThan(item.Iteminfo);
        }
    }

    public class Inventory : MVVM.ViewModelBase
    {
        private static ILog log = LogManager.GetLogger(typeof(Inventory));
        private Bot bot;

        public ushort Size = 0;

        public ICollectionView ItemViewPage1 { get; set; }
        public ICollectionView ItemViewPage2 { get; set; }
        public ICollectionView ItemViewPage3 { get; set; }

        public static Inventory Create(Bot bot)
        {
            Inventory inv = null;
            App.Current.Dispatcher.Invoke(() => inv = new Inventory(bot));
            return inv;
        }

        private Inventory(Bot bot)
        {
            this.bot = bot;

            BindingOperations.EnableCollectionSynchronization(items, itemsLock);
            ((CollectionView)CollectionViewSource.GetDefaultView(items)).SortDescriptions.Add(new SortDescription("Slot", ListSortDirection.Ascending));

            ItemViewPage1 = new ListCollectionView(items);
            ItemViewPage1.Filter = (_) =>
            {
                var item = _ as InventoryItem;

                return item != null && item.Slot >= 13 && item.Slot < 13 + 32;
            };
            ItemViewPage1.SortDescriptions.Add(new SortDescription("Slot", ListSortDirection.Ascending));

            ItemViewPage2 = new ListCollectionView(items);
            ItemViewPage2.Filter = (_) =>
            {
                var item = _ as InventoryItem;

                return item != null && item.Slot >= 13 + 32 && item.Slot < 13 + 32 + 32;
            };
            ItemViewPage2.SortDescriptions.Add(new SortDescription("Slot", ListSortDirection.Ascending));

            ItemViewPage3 = new ListCollectionView(items);
            ItemViewPage3.Filter = (_) =>
            {
                var item = _ as InventoryItem;

                return item != null && item.Slot >= 13 + 32 + 32 && item.Slot < 13 + 32 + 32 + 32;
            };
            ItemViewPage3.SortDescriptions.Add(new SortDescription("Slot", ListSortDirection.Ascending));

            items.CollectionChanged += (_, __) => RefreshInventoryViews();
        }

        public InventoryItem this[byte slot]
        {
            get
            {
                return GetItem(slot);
            }
            set
            {
                if (value == null) return;
                value.Slot = slot;
                Add(value);
            }
        }

        public EventHandler<InventoryItem> ItemGained;
        private void itemGained(InventoryItem item)
        {
            ItemGained?.Invoke(this, item);
        }

        public EventHandler ItemsMerged;
        private void itemsMerged()
        {
            ItemsMerged?.Invoke(this, null);
        }

        public EventHandler ItemSold;
        private void itemSold()
        {
            ItemSold?.Invoke(this, null);
        }

        private ObservableCollection<InventoryItem> items = new ObservableCollection<InventoryItem>();
        private Object itemsLock = new Object();

        public static InventoryItem ParseItem(Packet packet, SROBot.Bot bot, byte slot)
        {
            try
            {
                var rentType = packet.ReadUInt8(); // RentType

                //	switch(Item.RentType)
                //	{
                //		case 1:
                //			2	ushort	Item.Rent.CanDelete (adds "Will be deleted when time period is over" to item)
                //			4	uint	Item.Rent.PeriodBeginTime
                //			4	uint	Item.Rent.PeriodEndTime
                //		break;
                //
                //		case 2:
                //			2	ushort	Item.Rent.CanDelete (adds "Will be deleted when time period is over" to item)
                //			2	ushort	Item.Rent.CanRecharge (adds "Able to extend" to item)
                //			4	uint	Item.Rent.MeterRateTime
                //		break;
                //
                //		case 3:
                //			2	ushort	Item.Rent.CanDelete (adds "Will be deleted when time period is over" to item)
                //			4	uint	Item.Rent.PeriodBeginTime
                //			4	uint	Item.Rent.PeriodEndTime
                //			2	ushort	Item.Rent.CanRecharge (adds "Able to extend" to item)
                //			4	uint	Item.Rent.PackingTime
                //		break;
                //	}	

                //bot.Debug("slot: {0}", slot);
                //bot.Debug("renttype: {0}", rentType);

                switch (rentType)
                {
                    case 0:
                        {
                            var tmp1 = packet.ReadUInt8();
                            var tmp2 = packet.ReadUInt16();
                            //bot.Debug($"{tmp1} - {tmp2}");
                        }
                        break;
                    case 1:
                        {
                            var tmp1 = packet.ReadUInt16();
                            var tmp2 = packet.ReadUInt32();
                            var tmp3 = packet.ReadUInt32();
                            //bot.Debug($"{tmp1} - {tmp2} - {tmp3}");
                        }
                        break;
                    case 2:
                        {
                            var tmp1 = packet.ReadUInt16();
                            var tmp2 = packet.ReadUInt16();
                            var tmp3 = packet.ReadUInt32();
                            //bot.Debug($"{tmp1} - {tmp2} - {tmp3}");
                        }
                        break;
                    case 3:
                        {
                            var tmp1 = packet.ReadUInt16();
                            var tmp2 = packet.ReadUInt32();
                            var tmp3 = packet.ReadUInt32();
                            var tmp4 = packet.ReadUInt16();
                            var tmp5 = packet.ReadUInt32();
                            //bot.Debug($"{tmp1} - {tmp2} - {tmp3} - {tmp4} - {tmp5}");
                        }
                        break;
                }

                var itemModel = packet.ReadUInt32();
                var iteminfo = ItemInfos.GetById(itemModel);
                if (iteminfo == null)
                {
                    bot.Debug("COULD NOT FIND item id {0} -> {1} on slot {2}", itemModel, rentType, slot);
                    return null;
                }
                
                if (iteminfo.TypeId1 == 1 && iteminfo.TypeId2 == 1)
                {
                    var item_plus = packet.ReadUInt8();
                    var WhiteStatss = packet.ReadUInt64(); // white stats         
                    var dura = packet.ReadUInt32();
                    var blueamm = packet.ReadUInt8();

                    var blues = new Dictionary<SROData.MagicOption, uint>();

                    for (int i = 0; i < blueamm; i++)
                    {
                        var blueId = packet.ReadUInt32();
                        var blueVal = packet.ReadUInt32();

                        var blue = SROData.MagicOptions.GetById(blueId);
                        if (blue == null)
                        {
                            bot.Debug("slot: {0}/{1} -> could not find this blue: {2} with value: {3}", slot, iteminfo.Type, blueId, blueVal);
                            continue;
                        }
                        blues[blue] = blueVal;
                    }

                    // sockets

                    var optType = packet.ReadUInt8(); //OptType (1 => Socket)
                    var optCnt = packet.ReadUInt8(); //OptCount

                    //			ForEach(Option)
                    //			{
                    //				1	byte	Option.Slot
                    //				4	uint	Option.ID
                    //				4	uint	Option.nParam1 (=> Reference to Socket)
                    //			}

                    for (int i = 0; i < optCnt; ++i)
                    {
                        var optSlot = packet.ReadUInt8();
                        var optId = packet.ReadUInt32();
                        var optParam = packet.ReadUInt32();

                        bot.Debug("slot: {4} => socket slot: {0}, id: {1}/{2}, param: {3}", optSlot, optId, ItemInfos.GetById(optId)?.Type ?? "---", optParam, slot);
                    }

                    // adv. elixirs

                    optType = packet.ReadUInt8(); //OptType (2 => Advanced elixir)
                    optCnt = packet.ReadUInt8(); //OptCount

                    //			ForEach(Option)
                    //			{
                    //				1	byte	Option.Slot
                    //				4	uint	Option.ID
                    //				4	uint	Option.OptValue (=> "Advanced elixir in effect [+OptValue]")
                    //			}

                    for (int i = 0; i < optCnt; ++i)
                    {
                        var optSlot = packet.ReadUInt8();
                        var optId = packet.ReadUInt32();
                        var optValue = packet.ReadUInt32();

                        bot.Debug("slot: {4} => advelix slot: {0}, id: {1}/{2}, value: {3}", optSlot, optId, ItemInfos.GetById(optId)?.Type ?? "---", optValue, slot);
                    }

                    iteminfo.Plus = item_plus;

                    var item = new InventoryItem(slot, itemModel, iteminfo, 1, WhiteStatss)
                    {
                        Durability = dura
                    };

                    foreach (var blue in blues)
                    {
                        item.BlueStats[blue.Key] = blue.Value;
                    }

                    return item;
                }
                else if (iteminfo.TypeId1 == 2 && iteminfo.TypeId2 == 2)
                {
                    InventoryItem item = null;

                    switch (iteminfo.TypeId3)
                    {
                        case 1:
                            {
                                //		1	byte	Status (1 = Unsumonned, 2 = Summoned, 3 = Alive, 4 = Dead)
                                //		4	uint	RefObjID
                                //		2	ushort	Name.Lenght
                                //		*	string	Name
                                //		if(AbilityPet)
                                //		{
                                //			4	uint	SecondsToRentEndTime
                                //		}
                                //		1	byte	*unk02 -> Check for != 0	

                                var flag = packet.ReadUInt8();
                                if (flag == 2 || flag == 3 || flag == 4)
                                {
                                    var _model = packet.ReadUInt32(); //Model
                                    var _name = packet.ReadAscii();

                                    if (iteminfo.TypeId4 == 2) // ability pet?
                                    {
                                        var xx = packet.ReadUInt32();
                                    }

                                    packet.ReadUInt8();
                                }

                                item = new InventoryItem(slot, itemModel, iteminfo, 1);

                                if (flag == 2)
                                {
                                    item.Summoned = true;
                                }
                            }
                            break;

                        case 2: // ITEM_ETC_TRANS_MONSTER
                        case 3: // ITEM_MALL_MAGIC_CUBE_..
                            packet.ReadUInt32();
                            item = new InventoryItem(slot, itemModel, iteminfo, 1);
                            break;
                    }

                    return item;
                }
                else
                {
                    ushort count = packet.ReadUInt16();

                    switch (iteminfo.TypeId3)
                    {
                        case 11:
                            {
                                var level = int.Parse(iteminfo.Type.Split('_').Last());
                                if (iteminfo.TypeId4 != 7)
                                {
                                    packet.ReadUInt8();
                                }
                            }
                            break;

                        case 8:
                            packet.ReadAscii();
                            break;
                    }

                    return new InventoryItem(slot, itemModel, iteminfo, count);
                }
            }
            catch { return null; }
        }

        public bool Add(InventoryItem invitem)
        {
            if (invitem == null) return false; // do not check with IsEmpty

            var itemOnSlot = items.FirstOrDefault(i => i.Slot == invitem.Slot);
            if (SROBot.Inventory.IsItemNotEmpty(itemOnSlot)) return false;

            lock (itemsLock)
            {
                if (itemOnSlot != null) items.Remove(itemOnSlot);

                items.Add(invitem);
            }

            RefreshInventoryViews();

            return true;
        }

        private static InventoryItem CreateEmptyItem(byte slot)
        {
            return new InventoryItem(slot, 0, new ItemInfo(0, "EMPTY") { Name = "EMPTY", Icon = "empty.bmp" }, 0);
        }

        public bool Remove(InventoryItem invitem)
        {
            if (invitem == null) return false; // do not check with IsEmpty
            if (!items.Any(i => i.Slot == invitem.Slot)) return false;
            lock (itemsLock)
            {
                items.Remove(invitem);
                items.Add(CreateEmptyItem(invitem.Slot));
            }

            RefreshInventoryViews();

            return true;
        }

        public bool Remove(int slot)
        {
            lock (itemsLock)
            {
                foreach (var invitem in items.Where(i => i.Slot == slot).ToArray())
                {
                    items.Remove(invitem);
                    items.Add(CreateEmptyItem(invitem.Slot));
                }
            }

            RefreshInventoryViews();

            return true;
        }

        public bool ChangeDurability(int slot, uint durability)
        {
            lock (itemsLock)
            {
                var invItem = items.FirstOrDefault(i => i.Slot == slot);
                if (SROBot.Inventory.IsItemEmpty(invItem)) return false;
                invItem.Durability = durability;
            }
            return true;
        }

        public static bool IsItemEmpty(InventoryItem item)
        {
            return item == null || item.Iteminfo?.Type == "EMPTY";
        }

        public static bool IsItemNotEmpty(InventoryItem item)
        {
            return item != null && item.Iteminfo.Type != "EMPTY";
        }

        public InventoryItem GetItem(byte slot)
        {
            return items.OrderBy(i => i.Slot).FirstOrDefault(i => i.Slot == slot);
        }

        public InventoryItem GetItem(String nameStartsWith)
        {
            return items.OrderBy(i => i.Slot).FirstOrDefault(i => i.Iteminfo.Name.StartsWith(nameStartsWith));
        }

        public IEnumerable<InventoryItem> GetItems(Func<InventoryItem, bool> check)
        {
            return items.OrderBy(i => i.Slot).Where(i => check(i));
        }

        public InventoryItem GetItemByType(String typeStartsWith)
        {
            return items.OrderBy(i => i.Slot).FirstOrDefault(i => i.Iteminfo.Type.StartsWith(typeStartsWith));
        }

        public InventoryItem GetLowestStackByType(String type)
        {
            return GetItems(i => i.Iteminfo.Type.Equals(type)).OrderBy(i => i.Count).FirstOrDefault();
        }

        public uint GetAmountOf(String nameStartsWith)
        {
            uint amount = 0;
            try
            {
                amount = (uint)items.Where(i => !SROBot.Inventory.IsItemEmpty(i) && i.Iteminfo.Name.StartsWith(nameStartsWith)).Sum(i => i.Count);
            }
            catch { }

            return amount;
        }

        // retuns true if we merged something -> we need to wait
        public bool MergeItems()
        {
            var stackableItems = items.ToArray().Where(i => i.Slot != 7 /* dont stack these arrows ! */ && i.Count != i.Iteminfo.StackSize && !SROBot.Inventory.IsItemEmpty(i));
            foreach (var item in stackableItems)
            {
                var sameType = stackableItems.FirstOrDefault(i => i != item && i.Iteminfo.Type == item.Iteminfo.Type);
                if (SROBot.Inventory.IsItemEmpty(sameType)) continue;

                bot.Debug("we can stack slot {0}/{1} ({2}) with slot {3}/{4} ({5}) !!", item.Slot, item.Iteminfo.Type, item.Count, sameType.Slot, sameType.Iteminfo.Type, sameType.Count);

                Actions.MergeItems(sameType.Slot, item.Slot, bot);

                return true;
            }

            return false;
        }

        // retuns true if we merged something -> we need to wait
        public bool MergeStorageItems(UInt32 storageId)
        {
            var stackableItems = items.ToArray().Where(i => i.Slot != 7 /* dont stack these arrows ! */ && i.Count != i.Iteminfo.StackSize && !SROBot.Inventory.IsItemEmpty(i));
            foreach (var item in stackableItems)
            {
                var sameType = stackableItems.FirstOrDefault(i => i != item && i.Iteminfo.Type == item.Iteminfo.Type);
                if (SROBot.Inventory.IsItemEmpty(sameType)) continue;

                bot.Debug("storage: we can stack slot {0}/{1} ({2}) with slot {3}/{4} ({5}) !!", item.Slot, item.Iteminfo.Type, item.Count, sameType.Slot, sameType.Iteminfo.Type, sameType.Count);

                Actions.MergeStorageItems(storageId, sameType, item, bot);

                return true;
            }

            return false;
        }

        private int refreshHandle = 0;
        public bool DoNotRefreshViews = false;

        public void RefreshInventoryViews()
        {
            if (DoNotRefreshViews) return;

            new System.Threading.Thread((arg) =>
            {
                try
                {
                    var curHandle = (int)arg;

                    System.Threading.Thread.Sleep(1000);

                    if (curHandle != refreshHandle)
                    {
                        return;
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            ItemViewPage1.Refresh();
                            ItemViewPage2.Refresh();
                            ItemViewPage3.Refresh();
                        }
                        catch (Exception ex)
                        {
                            log.Debug($"RefreshInventoryViews(): {ex.Message}: {ex.StackTrace}");
                        }
                    }
                    );
                }
                catch { }
            }).Start(++refreshHandle);
        }

        public void MovementUpdate(Packet packet)
        {
            int check = packet.ReadUInt8();
            if (check == 1)
            {
                int typ = packet.ReadUInt8();

                switch (typ)
                {
                    case 0: // inventory <-> inventory
                        inventoryToInventory(packet);
                        break;

                    case 1: // storage <-> storage
                        handleStorageToStorage(packet);
                        break;

                    case 2: // inventory -> storage
                        handleInventoryToStorage(packet);
                        break;

                    case 3: // storage -> inventory
                        handleStorageToInventory(packet);
                        break;

                    case 6: // picked item: ground -> inventory
                        groundToInventory(packet);
                        break;

                    case 7: // dropped item: inventory -> ground
                        inventoryToGround(packet);
                        break;

                    case 8: // bougt item: shop -> inventory
                        shopToInventory(packet);
                        break;

                    case 9: // sold item: inventory -> shop
                        inventoryToShop(packet);
                        break;

                    case 14: // so-ok ?!
                        soOkToInventory(packet);
                        break;

                    case 15: // item deleted
                        {
                            var slot = packet.ReadUInt8();
                            var cnt = packet.ReadUInt8(); // its more a TYPE like a CNT !!?

                            var invItem = GetItem(slot);
                            if (SROBot.Inventory.IsItemNotEmpty(invItem))
                            {
                                if (invItem.Iteminfo.Type.Contains("_HALLOWEEN_") && invItem.Iteminfo.Type.EndsWith("_H"))
                                {
                                    bot.halloweenWords++;
                                    bot.Debug("changed {0} halloween words.", bot.halloweenWords);
                                }
                                
                                Remove(invItem);
                            }
                        }
                        break;

                    case 16: // pet <-> pet
                        petToPet(packet);
                        break;

                    case 17: // ground to pet
                        groundToPet(packet);
                        break;

                    case 26: // pet -> inventory
                        petToInventory(packet);
                        break;

                    case 27: // inventory -> pet
                        inventoryToPet(packet);
                        break;

                    case 28: // pet picked up gold
                        petPickupGold(packet);
                        break;

                    case 29: // guild storage -> guild storage
                        handleGuildStorageToGuildStorage(packet);
                        break;

                    case 30: // inventory -> guild storage
                        handleInventoryToGuildStorage(packet);
                        break;

                    case 31: // guild storage -> inventory
                        handleGuildStorageToInventory(packet);
                        break;

                    default:
                        bot.Debug("inventory movement unknown type: {0} => {1}", typ, string.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
                        break;
                }

                RefreshInventoryViews();
            }
            else if (check == 2)
            {
                byte check1 = packet.ReadUInt8();
                switch (check1)
                {
                    case 3: // invalid target -> schon verschwunden?
                        //bot.Debug("INVALID TARGET?");
                        break;

                    case 15: // not enough gold
                        bot.Log("NOT ENOUGH GOLD");
                        break;

                    case 18: // not my item.. belongs to another player
                        bot.Debug("ITEM BELONGS TO ANOTHER PLAYER!!");
                        break;

                    case 7: // inventory full
                        packet.ReadUInt8(); // idk
                        bot.Loop.CheckInventory(true);
                        break;

                    default:
                        bot.Debug("inventory movement fails: {0} => {1}", check1, string.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
                        break;
                }

                bot.Loop.NpcError();
            }
            else
            {
                bot.Debug("inventory movement: {0} => {1}", check, string.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
            }
        }

        private void inventoryToInventory(Packet packet)
        {
            var slot1 = packet.ReadUInt8();
            var slot2 = packet.ReadUInt8();
            var count = packet.ReadUInt16();

            var item1 = GetItem(slot1);
            var item2 = GetItem(slot2);

            if (IsItemEmpty(item2))
            {
                // No item, Moving !
                //Char_Data.inventoryslot[index_1] = inv_2;

                if (IsItemNotEmpty(item1))
                {
                    if (count != 0 && count < item1.Count) // splitted !!
                    {
                        item1.Count -= count;

                        Add(new InventoryItem(slot2, item1.UID, item1.Iteminfo, count));

                        bot.Debug("splitted slot {0} to slot {1}. Amount: {2} and {3} !", slot1, slot2, item1.Count, count);
                    }
                    else
                    {
                        item1.Slot = slot2;
                        item2.Slot = slot1; // move the empty item !

                        bot.Debug($"item {item1.Iteminfo.Type} moved from {slot1} to {slot2}");
                    }
                }
                else bot.Debug("item1 is NULL !!!");
            }
            else if (IsItemNotEmpty(item1))
            {
                //The item exist !
                //if (Char_Data.inventorytype[index_1] == Char_Data.inventorytype[index_2])
                if (item1.Iteminfo.Type.Equals(item2.Iteminfo.Type))
                {
                    // Items Are Same, Merge It !
                    //if (Char_Data.inventorycount[index_2] == Items_Info.items_maxlist[Items_Info.itemstypelist.IndexOf(Char_Data.inventorytype[index_2])])
                    if (item2.Iteminfo.StackSize == item2.Count)
                    {
                        //bot.Debug("item2 is max stack size! just swap them!");
                        // Items Are Maxed, Move It !
                        //Char_Data.inventoryslot[index_1] = inv_2;
                        //Char_Data.inventoryslot[index_2] = inv_1;
                        item1.Slot = slot2;
                        item2.Slot = slot1;
                    }
                    else
                    {
                        // Items Are Same, Merge It !
                        //if (Char_Data.inventorycount[index_1] == count)
                        if (item1.Count == count)
                        {
                            // Merged Everything, Delete The First Item !
                            //Char_Data.inventorycount[index_2] += count;
                            item2.Count += count;

                            Remove(item1);

                            //bot.Debug("merged from slot {0} to {1} -> {0} is removed!", slot1, slot2);
                        }
                        else
                        {
                            // Merged Not Everything, Recalculate Quantity !
                            //Char_Data.inventorycount[index_2] += count;
                            //Char_Data.inventorycount[index_1] -= count;
                            bot.Debug("could not merge totally, recalc counts.. {0}({1}): {2}, {3}({4}): {5} ==> count: {6}", item1.Iteminfo.Type, item1.Slot, item1.Count, item2.Iteminfo.Type, item2.Slot, item2.Count, count);
                            item2.Count += count;
                            item1.Count -= count;
                        }
                    }

                    itemsMerged();
                    bot.Loop.ItemsMerged();
                }
                else
                {
                    // Items Are Different, Move It !
                    //Char_Data.inventoryslot[index_1] = inv_2;
                    //Char_Data.inventoryslot[index_2] = inv_1;
                    bot.Debug($"different items, just swap.. {item1.Slot} -> {item2.Slot}");
                    item1.Slot = slot2;
                    item2.Slot = slot1;
                }
            }
            else bot.Debug("cant find any of these items .. teleport pls!!");
            
            if (_swapping)
            {
                bot.Loop.ItemBought(null, false); // retrigger ..
                bot.Loop.ItemMovedFromPetToInvetory(true, false);
                _swapping = false;
            }

            //if (BotData.loopaction == "merge")
            //{
            //    MergeItems();
            //}
        }

        private void groundToInventory(Packet packet)
        {
            byte slot = packet.ReadUInt8();
            if (slot == 254)
            {
                packet.ReadUInt32();
            }
            else
            {
                packet.ReadUInt32();
                uint itemModel = packet.ReadUInt32();

                var item = ItemInfos.GetById(itemModel);
                string type = item.Type ?? "";

                //Console.WriteLine("item picked {0} up to slot: {1}", type, slot);

                if (type.StartsWith("ITEM_CH") || type.StartsWith("ITEM_EU"))
                {
                    byte item_plus = packet.ReadUInt8();
                    packet.ReadUInt64();
                    uint dura = packet.ReadUInt32();
                    byte blueamm = packet.ReadUInt8();
                    for (int i = 0; i < blueamm; i++)
                    {
                        packet.ReadUInt8();
                        packet.ReadUInt16();
                        packet.ReadUInt32();
                        packet.ReadUInt8();
                    }

                    item.Plus = item_plus;
                    var newItem = new InventoryItem(slot, itemModel, item, 1) { Durability = dura };
                    Add(newItem);

                    checkForBetterItem(newItem);

                    itemGained(newItem);
                }
                else
                {
                    ushort count = packet.ReadUInt16();

                    var invitem = GetItem(slot);
                    if (SROBot.Inventory.IsItemNotEmpty(invitem))
                    {
                        var oldcnt = invitem.Count;
                        invitem.Count = count;
                        //Console.WriteLine("slot {0} changed from {1} to {2}", slot, oldcnt, invitem.Count);
                    }
                    else
                    {
                        Add(new InventoryItem(slot, itemModel, item, count));
                    }
                }
            }
        }

        private void inventoryToGround(Packet packet)
        {
            byte slot = packet.ReadUInt8();
            var invitem = GetItem(slot);
            Remove(invitem);
        }

        private bool _swapping;

        private void shopToInventory(Packet packet)
        {
            byte tab = packet.ReadUInt8();
            byte slot = packet.ReadUInt8();
            byte count = packet.ReadUInt8();

            #region Finding Item Info

            var npc = bot.Spawns.Shops.Get(bot.Loop.CurrentNPCId);
            if (npc == null)
            {
                bot.Debug("could not find NPC with id: {0}", bot.Loop.CurrentNPCId);
                return;
            }
            var shop = SROData.NPCs.GetByModel(npc.Mobinfo.Model);
            if (shop == null)
            {
                bot.Debug("could not find SHOP with model: {0}", npc.Mobinfo.Model);
                return;
            }

            var tabItem = shop.Tabs[tab].ItemModels.FirstOrDefault(i => i.IndexOfTab == slot);
            var itemModel = tabItem.Model;
            var item = ItemInfos.GetById(itemModel);
            if (item == null)
            {
                bot.Debug("could not find ITEM with model: {0}", itemModel);
                return;
            }

            #endregion

            if (count == 1)
            {
                byte inv_slot = packet.ReadUInt8();
                ushort inv_count = packet.ReadUInt16();

                bot.Debug("item bought: {0}, count: {1} -> slot: {2}", item.Type, inv_count, inv_slot);
                item.Plus = tabItem.Plus;
                var boughtItem = new InventoryItem(inv_slot, item.Model, item, inv_count) { Durability = (uint)item.Durability };
                Add(boughtItem);

                Actions.FakeItemPickUp(bot, boughtItem);

                _swapping = checkForBetterItem(boughtItem);

                bot.Loop.ItemBought(boughtItem, _swapping);
            }
            else
            {
                bot.Debug("how is it possible to buy more than 1 item -- EXCHANGING????????");
            }
        }

        private void inventoryToShop(Packet packet)
        {
            byte inv_slot = packet.ReadUInt8();
            ushort count = packet.ReadUInt16();

            var invitem = GetItem(inv_slot);
            if (SROBot.Inventory.IsItemEmpty(invitem))
            {
                bot.Debug("could not find item on slot {0} !!", inv_slot);
                return;
            }

            if (count == invitem.Count)
            {
                //Sold Everything - Delete Item
                bot.Debug("item sold, slot: {0} !", inv_slot);
                Remove(invitem);

                bot.Loop.ItemSold(invitem.Slot);
                itemSold();
            }
            else
            {
                //Reduce count of item
                ushort new_count = (ushort)(invitem.Count - count);
                invitem.Count = new_count;
            }
        }

        private void groundToPet(Packet packet)
        {
            var petid = packet.ReadUInt32();

            if (bot.Char.Pickpet == null || bot.Char.Pickpet.UID != petid) return;

            byte slot = packet.ReadUInt8();

            if (slot == 254)
            {
                packet.ReadUInt32();
            }
            else
            {
                packet.ReadUInt32();
                uint itemModel = packet.ReadUInt32();

                var item = ItemInfos.GetById(itemModel);
                string type = item.Type ?? "";

                //Console.WriteLine("item picked {0} up to slot: {1}", type, slot);

                if (type.StartsWith("ITEM_CH") || type.StartsWith("ITEM_EU"))
                {
                    byte item_plus = packet.ReadUInt8();
                    packet.ReadUInt64();
                    uint dura = packet.ReadUInt32();
                    byte blueamm = packet.ReadUInt8();
                    for (int i = 0; i < blueamm; i++)
                    {
                        packet.ReadUInt8();
                        packet.ReadUInt16();
                        packet.ReadUInt32();
                        packet.ReadUInt8();
                    }

                    item.Plus = item_plus;
                    var newItem = new InventoryItem(slot, itemModel, item, 1) { Durability = dura };
                    bot.Char.Pickpet.Inventory.Add(newItem);

                    itemGained(newItem);
                }
                else
                {
                    ushort count = packet.ReadUInt16();

                    var invitem = bot.Char.Pickpet.Inventory.GetItem(slot);
                    if (SROBot.Inventory.IsItemNotEmpty(invitem))
                    {
                        var oldcnt = invitem.Count;
                        invitem.Count = count;
                    }
                    else
                    {
                        bot.Char.Pickpet.Inventory.Add(new InventoryItem(slot, itemModel, item, count));
                    }
                }
            }
        }

        private void petToInventory(Packet packet)
        {
            uint pet_id = packet.ReadUInt32();

            if (bot.Char.Pickpet == null || bot.Char.Pickpet.UID != pet_id) return;

            byte pet_slot = packet.ReadUInt8();
            var petItem = bot.Char.Pickpet.Inventory.GetItem(pet_slot);

            if (SROBot.Inventory.IsItemEmpty(petItem)) return;

            byte inv_slot = packet.ReadUInt8();
            var invItem = new InventoryItem(inv_slot, petItem.UID, petItem.Iteminfo, petItem.Count, (petItem.WhiteStats?.Value ?? 0));

            //Console.WriteLine("{0} | moved from pet to inventory --> {1}/{2}", DateTime.Now.ToString("HH:mm:ss.fff"), pet_slot, inv_slot);

            Add(invItem);
            bot.Char.Pickpet.Inventory.Remove(petItem);

            _swapping = checkForBetterItem(invItem);

            bot.Loop.ItemMovedFromPetToInvetory(true, _swapping);
        }

        private void inventoryToPet(Packet packet)
        {
            uint pet_id = packet.ReadUInt32();

            if (bot.Char.Pickpet == null || bot.Char.Pickpet.UID != pet_id) return;

            byte inv_slot = packet.ReadUInt8();
            byte pet_slot = packet.ReadUInt8();

            var invItem = GetItem(inv_slot);
            if (SROBot.Inventory.IsItemEmpty(invItem)) return;

            var petItem = new InventoryItem(pet_slot, invItem.UID, invItem.Iteminfo, invItem.Count, (invItem.WhiteStats?.Value ?? 0));

            bot.Char.Pickpet.Inventory.Add(petItem);
            Remove(invItem);
        }

        private void petToPet(Packet packet)
        {
            uint petid = packet.ReadUInt32();
            byte pet_1 = packet.ReadUInt8();
            byte pet_2 = packet.ReadUInt8();
            ushort count = packet.ReadUInt16();

            if (bot.Char.Pickpet == null || bot.Char.Pickpet.UID != petid) return;

            var petItem1 = bot.Char.Pickpet.Inventory.GetItem(pet_1);
            var petItem2 = bot.Char.Pickpet.Inventory.GetItem(pet_2);

            if (petItem1.Iteminfo.Type == petItem2.Iteminfo.Type)
            {
                // Items Are Same, Merge It !
                if (petItem2.Count == petItem2.Iteminfo.StackSize)
                {
                    // Items Are Maxed, Move It !
                    if (petItem1 != null) petItem1.Slot = pet_2;
                    if (petItem2 != null) petItem2.Slot = pet_1;
                }
                else
                {
                    if (petItem1.Count == count)
                    {
                        // Merged Everything, Delete The First Item !
                        petItem2.Count += count;
                        bot.Char.Pickpet.Inventory.Remove(petItem1);
                    }
                    else
                    {
                        // Merged Not Everything, Recalculate Quantity !
                        petItem2.Count += count;
                        petItem2.Count -= count;
                    }
                }
            }
            else
            {
                // Items Are Different, Move It !

                if (petItem1 != null) petItem1.Slot = pet_2;
                if (petItem2 != null) petItem2.Slot = pet_1;
            }
        }

        private void petPickupGold(Packet packet)
        {
            var petid = packet.ReadUInt32();

            if (bot.Char.Pickpet == null || bot.Char.Pickpet.UID != petid) return;

            var uk1 = packet.ReadUInt8(); // everytime 0xfe?

            var amountOfGold = packet.ReadUInt32();

            //bot.Char.Gold += amountOfGold; --> should be updated through 0x304e ?!
        }

        private void soOkToInventory(Packet packet)
        {
            /*
            
            5E                                                ^...............
            00                                                ................
            00 00 00 00                                       ................
            D3 17 00 00                                       ................
            05 00

            */

            var slot = packet.ReadUInt8();
            packet.ReadUInt8();
            packet.ReadUInt32();
            var model = packet.ReadUInt32();

            var iteminfo = ItemInfos.GetById(model);
            if (iteminfo == null)
            {
                bot.Debug("could not find item with model: {0}", model);
                return;
            }

            ushort cnt = 1;
            if (!iteminfo.Type.Contains("_COS_P_"))
            {
                cnt = packet.ReadUInt16();
            }

            if (cnt == 0)
            {
                cnt = 1;
            }

            bot.Debug("got item {0}/{1} -> cnt: {2}", iteminfo.Type, iteminfo.Name, cnt);
            //bot.Debug();

            var item = new InventoryItem(slot, model, iteminfo, cnt);
            Add(item);
        }

        private void handleStorageToStorage(Packet packet)
        {
            var slot1 = packet.ReadUInt8();
            var slot2 = packet.ReadUInt8();
            var count = packet.ReadUInt16();

            var item1 = bot.Storage.GetItem(slot1);
            var item2 = bot.Storage.GetItem(slot2);

            if (IsItemEmpty(item2))
            {
                // No item, Moving !
                //Char_Data.inventoryslot[index_1] = inv_2;

                if (IsItemNotEmpty(item1))
                {
                    if (count != 0 && count < item1.Count) // splitted !!
                    {
                        item1.Count -= count;

                        bot.Storage.Add(new InventoryItem(slot2, item1.UID, item1.Iteminfo, count));

                        bot.Debug("storage: splitted slot {0} to slot {1}. Amount: {2} and {3} !", slot1, slot2, item1.Count, count);
                    }
                    else
                    {
                        item1.Slot = slot2;
                        item2.Slot = slot1; // move empty item 

                        bot.Debug("storage: item moved fom {0} to {1}", slot1, slot2);
                    }
                }
                else bot.Debug("item1 is NULL !!!");
            }
            else if (IsItemNotEmpty(item1))
            {
                //The item exist !
                //if (Char_Data.inventorytype[index_1] == Char_Data.inventorytype[index_2])
                if (item1.Iteminfo.Type.Equals(item2.Iteminfo.Type))
                {
                    // Items Are Same, Merge It !
                    //if (Char_Data.inventorycount[index_2] == Items_Info.items_maxlist[Items_Info.itemstypelist.IndexOf(Char_Data.inventorytype[index_2])])
                    if (item2.Iteminfo.StackSize == item2.Count)
                    {
                        //bot.Debug("item2 is max stack size! just swap them!");
                        // Items Are Maxed, Move It !
                        //Char_Data.inventoryslot[index_1] = inv_2;
                        //Char_Data.inventoryslot[index_2] = inv_1;
                        item1.Slot = slot2;
                        item2.Slot = slot1;
                    }
                    else
                    {
                        // Items Are Same, Merge It !
                        //if (Char_Data.inventorycount[index_1] == count)
                        if (item1.Count == count)
                        {
                            // Merged Everything, Delete The First Item !
                            //Char_Data.inventorycount[index_2] += count;
                            item2.Count += count;

                            bot.Storage.Remove(item1);

                            //bot.Debug("merged from slot {0} to {1} -> {0} is removed!", slot1, slot2);
                        }
                        else
                        {
                            // Merged Not Everything, Recalculate Quantity !
                            //Char_Data.inventorycount[index_2] += count;
                            //Char_Data.inventorycount[index_1] -= count;
                            bot.Debug("could not merge totally, recalc counts.. {0}({1}): {2}, {3}({4}): {5} ==> count: {6}", item1.Iteminfo.Type, item1.Slot, item1.Count, item2.Iteminfo.Type, item2.Slot, item2.Count, count);
                            item2.Count += count;
                            item1.Count -= count;
                        }
                    }

                    //itemsMerged();
                    //bot.Loop.ItemsMerged();
                }
                else
                {
                    // Items Are Different, Move It !
                    //Char_Data.inventoryslot[index_1] = inv_2;
                    //Char_Data.inventoryslot[index_2] = inv_1;
                    //bot.Debug("different items, just swap..");
                    item1.Slot = slot2;
                    item2.Slot = slot1;
                }
            }
            else bot.Debug("cant find any of these items .. teleport pls!!");

            bot.Loop.StorageItemMerged();
        }

        private void handleInventoryToStorage(Packet packet)
        {
            byte inventorySlot = packet.ReadUInt8();
            byte storageSlot = packet.ReadUInt8();

            var invItem = GetItem(inventorySlot);
            if (SROBot.Inventory.IsItemEmpty(invItem)) return;

            Remove(invItem);

            invItem.Slot = storageSlot;
            bot.Storage.Add(invItem);

            bot.Loop.ItemPutToStorage();
        }

        private void handleStorageToInventory(Packet packet)
        {
            byte storageSlot = packet.ReadUInt8();
            byte inventorySlot = packet.ReadUInt8();

            var storItem = bot.Storage.GetItem(storageSlot);
            if (SROBot.Inventory.IsItemEmpty(storItem)) return;

            bot.Storage.Remove(storItem);

            storItem.Slot = inventorySlot;
            Add(storItem);
        }

        private void handleGuildStorageToGuildStorage(Packet packet)
        {
            var slot1 = packet.ReadUInt8();
            var slot2 = packet.ReadUInt8();
            var count = packet.ReadUInt16();

            var item1 = bot.GuildStorage.GetItem(slot1);
            var item2 = bot.GuildStorage.GetItem(slot2);

            if (IsItemEmpty(item2))
            {
                // No item, Moving !
                //Char_Data.inventoryslot[index_1] = inv_2;

                if (IsItemNotEmpty(item1))
                {
                    if (count != 0 && count < item1.Count) // splitted !!
                    {
                        item1.Count -= count;

                        bot.GuildStorage.Add(new InventoryItem(slot2, item1.UID, item1.Iteminfo, count));

                        bot.Debug("splitted slot {0} to slot {1}. Amount: {2} and {3} !", slot1, slot2, item1.Count, count);
                    }
                    else
                    {
                        item1.Slot = slot2;
                        item2.Slot = slot1;

                        //bot.Debug("item moved fom {0} to {1}", slot1, slot2);
                    }
                }
                else bot.Debug("item1 is NULL !!!");
            }
            else if (IsItemNotEmpty(item1))
            {
                //The item exist !
                //if (Char_Data.inventorytype[index_1] == Char_Data.inventorytype[index_2])
                if (item1.Iteminfo.Type.Equals(item2.Iteminfo.Type))
                {
                    // Items Are Same, Merge It !
                    //if (Char_Data.inventorycount[index_2] == Items_Info.items_maxlist[Items_Info.itemstypelist.IndexOf(Char_Data.inventorytype[index_2])])
                    if (item2.Iteminfo.StackSize == item2.Count)
                    {
                        //bot.Debug("item2 is max stack size! just swap them!");
                        // Items Are Maxed, Move It !
                        //Char_Data.inventoryslot[index_1] = inv_2;
                        //Char_Data.inventoryslot[index_2] = inv_1;
                        item1.Slot = slot2;
                        item2.Slot = slot1;
                    }
                    else
                    {
                        // Items Are Same, Merge It !
                        //if (Char_Data.inventorycount[index_1] == count)
                        if (item1.Count == count)
                        {
                            // Merged Everything, Delete The First Item !
                            //Char_Data.inventorycount[index_2] += count;
                            item2.Count += count;

                            bot.GuildStorage.Remove(item1);

                            //bot.Debug("merged from slot {0} to {1} -> {0} is removed!", slot1, slot2);
                        }
                        else
                        {
                            // Merged Not Everything, Recalculate Quantity !
                            //Char_Data.inventorycount[index_2] += count;
                            //Char_Data.inventorycount[index_1] -= count;
                            bot.Debug("could not merge totally, recalc counts.. {0}({1}): {2}, {3}({4}): {5} ==> count: {6}", item1.Iteminfo.Type, item1.Slot, item1.Count, item2.Iteminfo.Type, item2.Slot, item2.Count, count);
                            item2.Count += count;
                            item1.Count -= count;
                        }
                    }

                    //itemsMerged();
                    //bot.Loop.ItemsMerged();
                }
                else
                {
                    // Items Are Different, Move It !
                    //Char_Data.inventoryslot[index_1] = inv_2;
                    //Char_Data.inventoryslot[index_2] = inv_1;
                    //bot.Debug("different items, just swap..");
                    item1.Slot = slot2;
                    item2.Slot = slot1;
                }
            }
            else bot.Debug("cant find any of these items .. teleport pls!!");
        }

        private void handleInventoryToGuildStorage(Packet packet)
        {
            byte inventorySlot = packet.ReadUInt8();
            byte storageSlot = packet.ReadUInt8();

            var invItem = GetItem(inventorySlot);
            if (SROBot.Inventory.IsItemEmpty(invItem)) return;

            Remove(invItem);

            invItem.Slot = storageSlot;
            bot.GuildStorage.Add(invItem);
        }

        private void handleGuildStorageToInventory(Packet packet)
        {
            byte storageSlot = packet.ReadUInt8();
            byte inventorySlot = packet.ReadUInt8();

            var storItem = bot.GuildStorage.GetItem(storageSlot);
            if (SROBot.Inventory.IsItemEmpty(storItem)) return;

            bot.GuildStorage.Remove(storItem);

            storItem.Slot = inventorySlot;
            Add(storItem);
        }

        public InventoryItem GetReturnScroll()
        {
            return items.FirstOrDefault(i => i.Iteminfo.Name.StartsWith("Return Scroll") || i.Iteminfo.Name.StartsWith("Beginner Return Scroll"));
        }

        public void Clear()
        {
            items.Clear();

            for (byte slot = 0; slot < Size; slot++)
            {
                Add(CreateEmptyItem(slot));
            }
        }

        public void ItemUsed(Packet packet)
        {
            var type = packet.ReadUInt8();
            if (type == 1)
            {
                byte slot = packet.ReadUInt8();
                ushort count = packet.ReadUInt16();

                try
                {
                    var invUseType = packet.ReadUInt16();
                    if (invUseType == 0x09EC || invUseType == 0x19EC) // some1 used a return scroll !
                    {
                        bot.Loop.BackToTown();
                    }
                }
                catch
                {
                    bot.Debug("could not read invUseType ..");
                }

                var invItem = GetItem(slot);
                if (SROBot.Inventory.IsItemNotEmpty(invItem))
                {
                    //Console.WriteLine("updated slot {0} to count: {1}", slot, count);
                    if (count > 0)
                    {
                        invItem.Count = count;
                    }
                    else
                    {
                        Remove(invItem);
                    }
                }
            }
        }

        public void ArrowUpdate(Packet packet)
        {
            //Console.WriteLine("arrow update .. {0}", String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
            var arrowCount = packet.ReadUInt16();
            var arrows = GetItem(7);
            if (SROBot.Inventory.IsItemEmpty(arrows)) return;

            arrows.Count = arrowCount;
            if (arrowCount == 0)
            {
                //bot.Debug("arrows empty.. remove item on slot 7????");
                Remove(arrows);
            }
        }

        public int FreeSlots(byte startIdx = 13)
        {
            var free = 0;
            for (var cnt = startIdx; cnt < Size; ++cnt)
            {
                var item = GetItem(cnt);
                if (IsItemEmpty(item)) ++free;
            }

            return free;
        }

        public byte FirstFreeSlot(byte startIdx = 13)
        {
            for (var cnt = startIdx; cnt < Size; ++cnt)
            {
                var item = GetItem(cnt);
                if (IsItemEmpty(item)) return cnt;
            }

            return 0;
        }

        public bool IsFull(byte startIdx = 13)
        {
            return FreeSlots(startIdx) == 0;
        }

        public bool IsEmpty(byte startIdx = 13)
        {
            return FreeSlots(startIdx) == Size;
        }

        public enum ARMOR_TYPE
        {
            NONE = 0,
            GARMENT,
            PROTECTOR,
            ARMOR,
            MIXED,
            UNKNOWN
        }

        public ARMOR_TYPE GetArmorType()
        {
            byte armorPartIdx = 0;
            InventoryItem firstArmorPart = null;
            var armorType = "NIX_VERSTEHEN";
            var armorGender = "NO_GENDER";

            do
            {
                firstArmorPart = GetItem(armorPartIdx++);
                if (SROBot.Inventory.IsItemNotEmpty(firstArmorPart))
                {
                    armorGender = "_" + firstArmorPart.Iteminfo.Type.Split('_')[2] + "_";
                    armorType = "_" + firstArmorPart.Iteminfo.Type.Split('_')[3] + "_";
                }
            }
            while (SROBot.Inventory.IsItemEmpty(firstArmorPart) && armorPartIdx < 6);

            if (armorType == "NIX_VERSTEHEN") return ARMOR_TYPE.NONE;
            if (armorType == "_CLOTHES_") return ARMOR_TYPE.GARMENT;
            if (GetItems(i => i.Slot >= 0 && i.Slot < 6 && IsItemNotEmpty(i)).All(i => i.Iteminfo.Type.Contains("_HEAVY"))) return ARMOR_TYPE.ARMOR;
            else if (GetItems(i => i.Slot >= 0 && i.Slot < 6 && IsItemNotEmpty(i)).All(i => i.Iteminfo.Type.Contains("_LIGHT"))) return ARMOR_TYPE.PROTECTOR;
            else return ARMOR_TYPE.MIXED;
        }

        public string GetGender()
        {
            return GetItems(i => i.Slot >= 0 && i.Slot < 6 && IsItemNotEmpty(i)).FirstOrDefault()?.Iteminfo.Type.Split('_')[2];
        }

        private bool checkForBetterItem(InventoryItem invitem)
        {
            // swapping items
            try
            {
                var item = invitem.Iteminfo;
                var type = item.Type;
                var slot = invitem.Slot;

                if (bot.Config.Training.EquipPickedItems && slot >= 13 && type.StartsWith("ITEM_CH_") && item.Level <= bot.Char.Level)
                {
                    var weapon = GetItem(6);
                    if (item.IsWeapon)
                    {
                        var weaponType = "NIX_VERSTEHEN";
                        if (SROBot.Inventory.IsItemNotEmpty(weapon))
                        {
                            weaponType = "_" + weapon.Iteminfo.Type.Split('_')[2] + "_";
                        }

                        // weapon
                        if (type.Contains(weaponType) || bot.Config.Training.UseAnyTypeOfWeapon)
                        {
                            if (invitem.IsBetterThan(weapon))
                            {
                                bot.Log("found a better weapon? {1} with level {2} !!", DateTime.Now.ToString("HH:mm:ss.fff"), item.Type, item.Level);
                                if (!type.Contains(weaponType))
                                {
                                    bot.Log($"switched weapon type.. got: {weaponType}.. and now using: {type}");
                                }

                                Actions.SwapItems(slot, 6, bot);
                                return true;
                            }
                        }
                    }
                    else if (item.IsShield)
                    {
                        var shield = GetItem(7);
                        if ((SROBot.Inventory.IsItemNotEmpty(shield) && shield.Iteminfo.Type.Contains("_SHIELD_")) || (SROBot.Inventory.IsItemEmpty(shield) && SROBot.Inventory.IsItemNotEmpty(weapon) && (weapon.Iteminfo.Type.Contains("_SWORD_") || weapon.Iteminfo.Type.Contains("_BLADE_"))))
                        {
                            if (SROBot.Inventory.IsItemEmpty(shield) || invitem.IsBetterThan(shield))
                            {
                                bot.Log("found a better shield? {1} with level {2} !!", DateTime.Now.ToString("HH:mm:ss.fff"), item.Type, item.Level);
                                Actions.SwapItems(slot, 7, bot);
                                return true;
                            }
                        }
                    }
                    else if (item.IsArmor)
                    {
                        var armorType = "NIX_VERSTEHEN";
                        var armorGender = "NO_GENDER";

                        byte armorPartIdx = 0;
                        InventoryItem firstArmorPart = null;
                        do
                        {
                            firstArmorPart = GetItem(armorPartIdx++);
                            if (SROBot.Inventory.IsItemNotEmpty(firstArmorPart))
                            {
                                armorGender = "_" + firstArmorPart.Iteminfo.Type.Split('_')[2] + "_";
                                armorType = "_" + firstArmorPart.Iteminfo.Type.Split('_')[3] + "_";
                            }
                        }
                        while (SROBot.Inventory.IsItemEmpty(firstArmorPart) && armorPartIdx < 6);

                        if (armorType == "NIX_VERSTEHEN") return false;
                        
                        /*
                            slot: 0: Blue Dragon Hat (Seal of Sun) / ITEM_CH_M_CLOTHES_13_HA_C_RARE
                            slot: 1: Blue Dragon Suit (Seal of Sun) / ITEM_CH_M_CLOTHES_13_BA_C_RARE
                            slot: 2: Blue Dragon Talisman (Seal of Sun) / ITEM_CH_M_CLOTHES_13_SA_C_RARE
                            slot: 3: Blue Dragon Wristlet (Seal of Sun) / ITEM_CH_M_CLOTHES_13_AA_C_RARE
                            slot: 4: Blue Dragon Trousers (Seal of Sun) / ITEM_CH_M_CLOTHES_13_LA_C_RARE
                            slot: 5: Blue Dragon Shoes (Seal of Sun) / ITEM_CH_M_CLOTHES_13_FA_C_RARE
                        */

                        var armorTypes = new List<string>();

                        armorTypes.Add(armorType);

                        if (bot.Config.Training.MixProtectorAndArmor)
                        {
                            if (armorType == "_HEAVY_") armorTypes.Add("_LIGHT_");
                            if (armorType == "_LIGHT_") armorTypes.Add("_HEAVY_");
                        }

                        if (!type.Contains(armorGender) || !armorTypes.Any(at => type.Contains(at))) return false;

                        for (byte armorPartSlot = 0; armorPartSlot < 6; ++armorPartSlot)
                        {
                            var armorPart = GetItem(armorPartSlot);
                            var armorPartType = "NIX_VERSTEHEN";
                            
                            switch (armorPartSlot)
                            {
                                case 0:
                                    armorPartType = "_HA_";
                                    break;
                                case 1:
                                    armorPartType = "_BA_";
                                    break;
                                case 2:
                                    armorPartType = "_SA_";
                                    break;
                                case 3:
                                    armorPartType = "_AA_";
                                    break;
                                case 4:
                                    armorPartType = "_LA_";
                                    break;
                                case 5:
                                    armorPartType = "_FA_";
                                    break;
                            }

                            // there are 2 different heads .. ?! (head and corone)
                            if (armorPartType == "_HA_" && type.Contains("_CA_")) armorPartType = "_CA_";
                            if (armorPartType == "_CA_" && type.Contains("_HA_")) armorPartType = "_HA_";
                            
                            if (type.Contains(armorPartType))
                            {
                                if (SROBot.Inventory.IsItemEmpty(armorPart) || invitem.IsBetterThan(armorPart))
                                {
                                    bot.Log("found a better armorpart? {1} with level {2} !!", DateTime.Now.ToString("HH:mm:ss.fff"), item.Type, item.Level);
                                    Actions.SwapItems(slot, armorPartSlot, bot);
                                    return true;
                                }

                                break; // not better, but was the right part.. break loop!
                            }
                        }
                    }
                    else if (item.IsAccessory)
                    {
                        /*
                            slot: 9: Cintamani Blue Dragon Earring (Seal of Sun) / ITEM_CH_EARRING_13_C_RARE
                            slot: 10: Cintamani Blue Dragon Necklace (Seal of Sun) / ITEM_CH_NECKLACE_13_C_RARE
                            slot: 11: Cintamani Blue Dragon Ring (Seal of Sun) / ITEM_CH_RING_13_C_RARE
                            slot: 12: Cintamani Blue Dragon Ring (Seal of Sun) / ITEM_CH_RING_13_C_RARE
                        */

                        if (type.Contains("EARRING"))
                        {
                            var earring = GetItem(9);
                            if (SROBot.Inventory.IsItemEmpty(earring) || invitem.IsBetterThan(earring))
                            {
                                bot.Log("found a better earring? {1} with level {2} !!", DateTime.Now.ToString("HH:mm:ss.fff"), item.Type, item.Level);
                                Actions.SwapItems(slot, 9, bot);
                                return true;
                            }
                        }
                        else if (type.Contains("NECKLACE"))
                        {
                            var necklace = GetItem(10);
                            if (SROBot.Inventory.IsItemEmpty(necklace) || invitem.IsBetterThan(necklace))
                            {
                                bot.Log("found a better necklace? {1} with level {2} !!", DateTime.Now.ToString("HH:mm:ss.fff"), item.Type, item.Level);
                                Actions.SwapItems(slot, 10, bot);
                                return true;
                            }
                        }
                        else if (type.Contains("RING"))
                        {
                            var ring1 = GetItem(11);
                            var ring2 = GetItem(12);

                            if (SROBot.Inventory.IsItemEmpty(ring1))
                            {
                                bot.Log("ring1 is empty? {1} with level {2} !!", DateTime.Now.ToString("HH:mm:ss.fff"), item.Type, item.Level);

                                Actions.SwapItems(slot, 11, bot);
                                return true;
                            }
                            else if (SROBot.Inventory.IsItemEmpty(ring2))
                            {
                                bot.Log("ring2 is empty? {1} with level {2} !!", DateTime.Now.ToString("HH:mm:ss.fff"), item.Type, item.Level);

                                Actions.SwapItems(slot, 12, bot);
                                return true;
                            }
                            else
                            {
                                var lowestRing = ring1.IsBetterThan(ring2) ? ring2 : ring1;

                                if (invitem.IsBetterThan(lowestRing))
                                {
                                    bot.Log($"found a better ring? {item.Type} with level {item.Level} !! swap with ring: {lowestRing.Slot}");
                                    Actions.SwapItems(slot, lowestRing.Slot, bot);
                                    return true;
                                }
                            }
                        }
                    }

                }
            }
            catch { }

            return false;
        }

        public ObservableCollection<InventoryItem> GetItems()
        {
            return items;
        }
    }
}
