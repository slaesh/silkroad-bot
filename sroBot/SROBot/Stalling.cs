using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public class Stalling
    {
        public class StallSlot
        {
            public byte Slot { get; set; }
            public InventoryItem Item { get; set; }
            public UInt64 Price { get; set; }
        }
        
        private bool _stalling = false;

        public bool Created { get; protected set; } = false;
        public bool Opened { get; protected set; } = false;
        public StallSlot[] Slots { get; protected set; } = new StallSlot[10];
        public int FreeSlots => Slots.Count(s => s == null);

        private Bot bot;

        public Stalling(Bot bot)
        {
            this.bot = bot;

            bot.Disconnected += (_, __) =>
            {
                Created = false;
                Opened = false;
                Slots = new StallSlot[10];
            };

            bot.Reconnected += (_, __) =>
            {
                if (!bot.Config.Stalling.ReCreateAfterLogin) return;
                if (!_stalling) return;

                bot.Log("re-creating stall in 10 seconds..");

                new Thread(() =>
                {
                    Thread.Sleep(10000);

                    Create();
                    PutNewItems();
                }).Start();
            };
        }

        public bool Create()
        {
            if (Created || Opened) return false;

            _stalling = true;
            
            SROBot.Actions.CreateStall(bot, bot.Config.Stalling.Title, bot.Config.Stalling.Message);

            bot.Log($"stall created: {bot.Config.Stalling.Title}");

            return Created = true;
        }

        public bool Open()
        {
            if (!Created || Opened) return false;

            SROBot.Actions.OpenStall(bot);

            bot.Log($"stall opened");

            return Opened = true;
        }

        public bool Modify()
        {
            if (!Created || !Opened) return false;

            SROBot.Actions.ModifyStall(bot);

            bot.Log($"stall will be modified");

            Opened = false;

            return true;
        }

        public bool Close()
        {
            if (!Created) return true;

            _stalling = false;

            SROBot.Actions.CloseStall(bot);

            Opened = false;
            Created = false;
            Slots = new StallSlot[10];

            bot.Log($"stall closed");

            return true;
        }

        public int AddItem(InventoryItem item, UInt64 price)
        {
            if (!Created || Opened) return -1;

            byte slot = 0;
            for (; slot < Slots.Length; slot++)
            {
                if (Slots[slot] == null) break;
            }

            if (slot >= Slots.Length) return -1;

            SROBot.Actions.PutItemToStall(bot, item, slot, price);
            Slots[slot] = new StallSlot { Item = item, Slot = slot, Price = price };

            bot.Log($"stall item from slot {item.Slot} to stall-slot {slot}");

            return slot;
        }

        public bool ItemSold(byte slot, string playerName)
        {
            if (!Created || !Opened) return false;
            if (slot >= Slots.Length) return false;
            if (Slots[slot] == null) return false;

            var stallSlot = Slots[slot];

            bot.Inventory.Remove(stallSlot.Item.Slot);
            Slots[slot] = null;

            bot.Log($"{playerName ?? "--UNKNOWN--"} has has bought stall-item({slot}): {stallSlot.Item.Iteminfo.Type} for {stallSlot.Price:N0} Gold");

            if (FreeSlots < 2) return true;

            PutNewItems();

            return true;
        }

        public void PutNewItems()
        {
            var invSlots = Slots.Where(s => s != null).Select(s => s.Item.Slot).ToList();
            var elixirs = bot.Inventory.GetItems(i => i.Iteminfo.Type.StartsWith("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_") && i.Iteminfo.Type.EndsWith("_B") && !invSlots.Contains(i.Slot)).ToArray();
            var pricesPerPiece = new Dictionary<string, ulong>
                {
                    { "ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_WEAPON_B", 16 * (ulong)1000000000 /*(19b)*/ / 1000 },
                    { "ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_SHIELD_B", (ulong)(.7 * (ulong)1000000000) /*(1b)*/ / 1000 },
                    { "ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_ARMOR_B", (ulong)(1.5 * (ulong)1000000000) /*(2b)*/ / 1000 },
                    { "ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_ACCESSARY_B", (ulong)(1 * (ulong)1000000000) /*(2.5b)*/ / 1000 },
                };

            if (!elixirs.Any() || FreeSlots == 0)
            {
                if (FreeSlots == Slots.Length)
                {
                    Close();
                }

                return;
            }

            new Thread(() =>
            {
                Thread.Sleep(1000);
                Modify();

                var cnt = FreeSlots > elixirs.Length ? elixirs.Length : FreeSlots;
                var freeStallSlots = Slots.Where(s => s != null).Select(s => s.Slot).ToList();
                var item = elixirs.FirstOrDefault();
                var ignoredTypes = new List<string>();

                Func<int> addItem = () =>
                {
                    var price = pricesPerPiece[item.Iteminfo.Type] * item.Count;
                    var ret = AddItem(item, price);
                    if (ret >= 0)
                    {
                        invSlots.Add(item.Slot);
                        --cnt;
                    }
                    return ret;
                };
                Action<bool> nextItem = (added) =>
                {
                    if (!added)
                    {
                        ignoredTypes.Add(item.Iteminfo.Type);
                    }
                    item = elixirs.FirstOrDefault(i => !ignoredTypes.Contains(i.Iteminfo.Type) && !invSlots.Contains(i.Slot));
                };

                while (cnt > 0 && Inventory.IsItemNotEmpty(item))
                {
                    Thread.Sleep(1000);

                    var elixirCnt = pricesPerPiece.ToDictionary(x => x.Key, x => x.Value);
                    elixirCnt.Keys.ToList().ForEach(k => elixirCnt[k] = (ulong)elixirs.Count(e => e.Iteminfo.Type == k));

                    var stallCnt = pricesPerPiece.ToDictionary(x => x.Key, x => x.Value);
                    stallCnt.Keys.ToList().ForEach(k => stallCnt[k] = (ulong)Slots.Count(s => s != null && s.Item.Iteminfo.Type == k));

                    var curType = item.Iteminfo.Type;
                    bot.Debug($"{curType}");
                    elixirCnt.Keys.ToList().ForEach(k => bot.Debug($"{k}: stall({stallCnt[k]}) - inventory({elixirCnt[k]})"));
                    bot.Debug();

                    var added = false;
                    if (stallCnt[curType] < 2 || elixirCnt.Keys.All(k => stallCnt[k] >= 2 || stallCnt[k] == elixirCnt[k]))
                    {
                        if (addItem() < 0) break;
                        added = true;

                        if (elixirCnt.Keys.All(k => stallCnt[k] >= 2 || stallCnt[k] == elixirCnt[k]))
                        {
                            ignoredTypes.Clear();
                        }
                    }

                    nextItem(added);
                }

                Thread.Sleep(5000);
                Open();

            }).Start();
        }
    }
}
