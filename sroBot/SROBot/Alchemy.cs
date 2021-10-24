using SilkroadSecurityApi;
using sroBot.SROData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public class Alchemy : MVVM.ViewModelBase
    {
        private Bot bot;

        public byte TargetSlot
        {
            get { return GetValue(() => TargetSlot); }
            set { SetValue(() => TargetSlot, value); }
        }
        public byte StartSlot
        {
            get { return GetValue(() => StartSlot); }
            set { SetValue(() => StartSlot, value); }
        }
        public byte EndSlot
        {
            get { return GetValue(() => EndSlot); }
            set { SetValue(() => EndSlot, value); }
        }
        public byte MinPlus
        {
            get { return GetValue(() => MinPlus); }
            set { SetValue(() => MinPlus, value); }
        }
        public byte TargetPlus
        {
            get { return GetValue(() => TargetPlus); }
            set { SetValue(() => TargetPlus, value); }
        }
        public Dictionary<MagicOption, int> TargetBlues
        {
            get { return GetValue(() => TargetBlues); }
            set { SetValue(() => TargetBlues, value); }
        }

        public bool IsStartedFusing
        {
            get;
            private set;
        }

        public bool IsStartedBlues
        {
            get;
            private set;
        }

        private bool fusingWasActive = false;
        private bool bluesWasActive = false;
        private int errorCount = 0;
        private MagicOption _curBlue;
        private List<string> _missing = new List<string>();

        public Alchemy(Bot bot)
        {
            this.bot = bot;

            StartSlot = 13;
            EndSlot = 47;
            MinPlus = 0;
            TargetPlus = 18;

            TargetBlues = new Dictionary<MagicOption, int>();
            TargetBlues[MagicOptions.GetByType("MATTR_STR", 13)] = 10;
            TargetBlues[MagicOptions.GetByType("MATTR_INT", 13)] = 10;
            TargetBlues[MagicOptions.GetByType("MATTR_DUR", 13)] = 200; // master (dura)
            TargetBlues[MagicOptions.GetByType("MATTR_HP", 13)] = 1700;
            TargetBlues[MagicOptions.GetByType("MATTR_MP", 13)] = 1700;
            TargetBlues[MagicOptions.GetByType("MATTR_ER", 13)] = 60; // dodging (parry)
            TargetBlues[MagicOptions.GetByType("MATTR_HR", 13)] = 60; // strikes (accuary)
            TargetBlues[MagicOptions.GetByType("MATTR_EVADE_BLOCK", 13)] = 100; // discipline (block)

            bot.Disconnected += (s, e) =>
            {
                fusingWasActive = IsStartedFusing;
                bluesWasActive = IsStartedBlues;

                Stop(false);
            };

            bot.Reconnected += (s, e) =>
              {
                  new Thread(() =>
                  {
                      Thread.Sleep(5000);

                      if (fusingWasActive && bot.Config.Alchemy.StartOnReconnect)
                      {
                          bot.Log("started fusing after reconnect !");
                          StartPlus();
                      }
                      else if (bluesWasActive && bot.Config.Alchemy.StartOnReconnect)
                      {
                          bot.Log("started blues after reconnect !");
                          StartBlue();
                      }

                      fusingWasActive = false;
                      bluesWasActive = false;
                  }).Start();
              };
        }

        private InventoryItem getNextItem(int minPlus = 0)
        {
            var start = StartSlot;
            if (TargetSlot > start) start = TargetSlot;
            if (start >= EndSlot) start = (byte)(StartSlot - 1);

            Func<InventoryItem, bool> isValidNextItem = i => (i != null && i.Iteminfo.IsDrop && i.Slot > start && i.Slot <= EndSlot && i.Iteminfo.Plus >= minPlus) && ((IsStartedFusing && i.Iteminfo.Plus < TargetPlus) || (IsStartedBlues));

            var nextItem = bot.Inventory.GetItems(i => isValidNextItem(i)).FirstOrDefault();
            if (SROBot.Inventory.IsItemEmpty(nextItem))
            {
                start = (byte)(StartSlot - 1);
                nextItem = bot.Inventory.GetItems(i => isValidNextItem(i)).FirstOrDefault();
                if (SROBot.Inventory.IsItemEmpty(nextItem)) return null;

                if (nextItem.Slot == TargetSlot)
                {
                    nextItem = null;
                }
            }

            if (SROBot.Inventory.IsItemEmpty(nextItem))
            {
                var curItem = bot.Inventory.GetItem(TargetSlot);
                if (SROBot.Inventory.IsItemNotEmpty(curItem) && isValidNextItem(curItem))
                {
                    return curItem;
                }
            }

            return nextItem;
        }

        private void PrintMissing()
        {
            if (_missing.Any()) bot.Debug("missing stones:");
            _missing.ForEach(m => bot.Debug($"--> {m}"));
        }

        private void StartFusingThread()
        {
            new Thread(() =>
            {
                Thread.Sleep(250);

                bool? result = false;

                if (IsStartedFusing)
                {
                    result = fuse();
                }
                else
                {
                    result = blue();
                }

                var itemsCannotFused = new List<int>();
                while ((IsStartedFusing || IsStartedBlues) && result != null)
                {
                    var nextItem = getNextItem(MinPlus);
                    if (SROBot.Inventory.IsItemNotEmpty(nextItem))
                    {
                        var curSlot = TargetSlot;

                        bot.Debug("[{0}] choose other item:: {1}", TargetSlot, nextItem.Slot);
                        bot.Debug();
                        TargetSlot = nextItem.Slot;

                        if (IsStartedFusing)
                        {
                            result = fuse();
                        }
                        else
                        {
                            result = blue();
                        }

                        if (result != null)
                        {
                            if (itemsCannotFused.Contains(TargetSlot) || curSlot == TargetSlot)
                            {
                                bot.Debug("goal reached or some error .. !");
                                IsStartedFusing = false;
                                IsStartedBlues = false;
                                break;
                            }

                            itemsCannotFused.Add(TargetSlot);
                        }

                        continue;
                    }
                    else
                    {
                        bot.Debug("goal reached !");
                        IsStartedFusing = false;
                        IsStartedBlues = false;
                        break;
                    }
                }
            }).Start();
        }

        private void StartBluesThread()
        {
            new Thread(() =>
            {
                Thread.Sleep(250);
                var result = blue();

                var itemsCannotFused = new List<int>();
                while (IsStartedBlues && result != null)
                {
                    var nextItem = getNextItem(MinPlus);
                    if (SROBot.Inventory.IsItemNotEmpty(nextItem))
                    {
                        var curSlot = TargetSlot;

                        bot.Debug("[{0}] choose other item:: {1}", TargetSlot, nextItem.Slot);
                        bot.Debug();

                        TargetSlot = nextItem.Slot;

                        result = blue();

                        if (result != null)
                        {
                            if (itemsCannotFused.Contains(TargetSlot) || curSlot == TargetSlot)
                            {
                                bot.Debug("goal reached or some error .. !");
                                PrintMissing();

                                IsStartedBlues = false;
                                break;
                            }

                            itemsCannotFused.Add(TargetSlot);
                        }

                        continue;
                    }
                    else
                    {
                        bot.Debug("goal reached !");
                        PrintMissing();

                        IsStartedBlues = false;
                        break;
                    }
                }
            }).Start();
        }

        public void HandleAlchemyResult(Packet packet)
        {
            var check = packet.ReadUInt8();
            var type = packet.ReadUInt8();

            // 01 02 00 10 01 - destroyed

            if (check == 1 && type == 2)
            {
                errorCount = 0;

                byte num = packet.ReadUInt8();

                var item = bot.Inventory.GetItem(TargetSlot);
                if (SROBot.Inventory.IsItemEmpty(item)) return;

                var slot = packet.ReadUInt8();

                var destroyed = false;
                if (num == 0)
                {
                    destroyed = packet.ReadUInt8() == 1;
                }

                var oldPlus = item.Iteminfo.Plus;
                var oldBlues = item.BlueStats;

                if (!destroyed)
                {
                    var newItem = Inventory.ParseItem(packet, bot, slot);
                    if (newItem == null)
                    {
                        return;
                    }

                    item.Iteminfo.Plus = newItem.Iteminfo.Plus;
                    item.WhiteStats = newItem.WhiteStats;
                    item.BlueStats = newItem.BlueStats;
                }

                if (num == 1)
                {
                    if (IsStartedFusing)
                    {
                        bot.Debug("[{0}] Success! Item +{1} => +{2}", TargetSlot, oldPlus, item.Iteminfo.Plus);
                    }
                    else if (IsStartedBlues)
                    {
                        var blue = item.BlueStats[_curBlue];
                        if (oldBlues.Keys.Contains(_curBlue))
                        {
                            bot.Debug("[{0}] {1} changed {2} => {3}", TargetSlot, _curBlue.Type, oldBlues[_curBlue], item.BlueStats[_curBlue]);
                        }
                        else
                        {
                            bot.Debug("[{0}] {1} added   {2}", TargetSlot, _curBlue.Type, item.BlueStats[_curBlue]);
                        }
                    }
                }
                else if (destroyed)
                {
                    bot.Debug("[{0}] Destroyed @ +{1} !", TargetSlot, oldPlus);
                    item = null;
                    ++TargetSlot;
                }
                else
                {
                    if (IsStartedFusing)
                    {
                        bot.Debug("[{0}] Failed ! Item +{1} => +{2}", TargetSlot, oldPlus, item.Iteminfo.Plus);
                    }
                    else if (IsStartedBlues)
                    {
                        bot.Debug("[{0}] blue failed??", TargetSlot);
                    }
                }

                //bot.Debug();

                if (IsStartedFusing)
                {
                    if (SROBot.Inventory.IsItemNotEmpty(item) && ((IsStartedFusing && item.Iteminfo.Plus >= 12)))
                    {
                        var nextItem = getNextItem(MinPlus);
                        if (SROBot.Inventory.IsItemNotEmpty(nextItem))
                        {
                            bot.Debug("[{0}] choose other item: {1}", TargetSlot, nextItem.Slot);
                            bot.Debug();
                            TargetSlot = nextItem.Slot;
                        }
                    }

                    StartFusingThread();
                }
                else if (IsStartedBlues)
                {
                    StartBluesThread();
                }
            }
            else if (check == 2 && type == 0x23)
            {
                var err = packet.ReadUInt8();
                if (err == 0x54)
                {
                    if (IsStartedBlues)
                    {
                        bot.Debug("[{0}] blue failed", TargetSlot);
                        StartBluesThread();
                    }
                }
                else
                {
                    bot.Debug("[{0}] !!alchemyResult: {1}", TargetSlot, String.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
                }
            }
            else
            {
                bot.Debug("[{0}] alchemyResult: {1}", TargetSlot, String.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
                if (++errorCount > 3)
                {
                    errorCount = 0;
                    Stop();
                }
            }
        }

        private InventoryItem getLowestElixirStack(InventoryItem targetItem)
        {
            if (SROBot.Inventory.IsItemEmpty(targetItem)) return null;

            if (targetItem.Iteminfo.IsWeapon)
            {
                return bot.Inventory.GetItems(i => i.Iteminfo.Type.Equals("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_WEAPON_B")).OrderBy(i => i.Count).FirstOrDefault();
            }
            else if (targetItem.Iteminfo.IsShield)
            {
                return bot.Inventory.GetItems(i => i.Iteminfo.Type.Equals("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_SHIELD_B")).OrderBy(i => i.Count).FirstOrDefault();
            }
            else if (targetItem.Iteminfo.IsArmor)
            {
                return bot.Inventory.GetItems(i => i.Iteminfo.Type.Equals("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_ARMOR_B")).OrderBy(i => i.Count).FirstOrDefault();
            }
            else if (targetItem.Iteminfo.IsAccessory)
            {
                return bot.Inventory.GetItems(i => i.Iteminfo.Type.Equals("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_ACCESSARY_B")).OrderBy(i => i.Count).FirstOrDefault();
            }

            return null;
        }

        private InventoryItem getLowestLuckyPowderStack(InventoryItem targetItem)
        {
            if (SROBot.Inventory.IsItemEmpty(targetItem)) return null;

            return bot.Inventory.GetItems(i => i.Iteminfo.Type.Equals("ITEM_ETC_ARCHEMY_REINFORCE_PROB_UP_A_" + targetItem.Iteminfo.Degree.ToString("D2"))).OrderBy(i => i.Count).FirstOrDefault();
        }

        private int fuseHandle = 0;
        private void fuseTimeout(object handle)
        {
            try
            {
                var timer = 12;
                while (timer-- > 0 && !MainWindow.WillBeClosed && (IsStartedFusing || IsStartedBlues))
                {
                    Thread.Sleep(1000);
                }

                if (MainWindow.WillBeClosed) return;

                var _handle = (int)handle;

                if (IsStartedFusing && _handle == fuseHandle)
                {
                    bot.Debug("[{0}] fusing timeout..", TargetSlot);
                    fuse();
                    return;
                }

                if (IsStartedBlues && _handle == fuseHandle)
                {
                    bot.Debug("[{0}] blues timeout..", TargetSlot);
                    blue();
                    return;
                }
            }
            catch { }
        }

        private bool? fuse()
        {
            var item = bot.Inventory.GetItem(TargetSlot);
            if (SROBot.Inventory.IsItemEmpty(item) || !item.Iteminfo.IsDrop) return false;
            if (item.Iteminfo.Plus < MinPlus) return false;
            if (item.Iteminfo.Plus >= TargetPlus) return true;

            var elixirs = getLowestElixirStack(item);
            if (elixirs == null)
            {
                bot.Debug("[{0}] no elixirs found!", TargetSlot);
                return false;
            }

            var luckypowders = getLowestLuckyPowderStack(item);
            if (bot.Config.Alchemy.UseLuckyPowder && bot.Config.Alchemy.UseLuckyPowderAt <= item.Iteminfo.Plus && luckypowders == null)
            {
                bot.Debug("[{0}] no lucky powder found!", TargetSlot);
                return false;
            }
            else if (!bot.Config.Alchemy.UseLuckyPowder || (bot.Config.Alchemy.UseLuckyPowder && bot.Config.Alchemy.UseLuckyPowderAt > item.Iteminfo.Plus))
            {
                luckypowders = null;
            }

            // steady
            if (bot.Config.Alchemy.UseSteady && bot.Config.Alchemy.UseSteadyAt <= item.Iteminfo.Plus && !item.BlueStats.Any(bs => bs.Key.Type.Equals("MATTR_SOLID")))
            {
                if (item.Iteminfo.CanGetStone("MATTR_SOLID"))
                {
                    bot.Debug("[{0}] add steady..", TargetSlot);

                    var stones = bot.Inventory.GetLowestStackByType("ITEM_ETC_ARCHEMY_MAGICSTONE_SOLID_" + item.Iteminfo.Degree.ToString("D2"));
                    if (SROBot.Inventory.IsItemEmpty(stones))
                    {
                        bot.Debug("[{0}] no steady stones found!", TargetSlot);
                        return false;
                    }

                    Actions.AddStone(bot, item.Slot, stones.Slot);
                    new Thread(fuseTimeout).Start(++fuseHandle);

                    return null;
                }
            }

            // immortal
            if (bot.Config.Alchemy.UseImmortal && bot.Config.Alchemy.UseImmortalAt <= item.Iteminfo.Plus && !item.BlueStats.Any(bs => bs.Key.Type.Equals("MATTR_ATHANASIA")))
            {
                bot.Debug("[{0}] add immortal..", TargetSlot);

                var stones = bot.Inventory.GetLowestStackByType("ITEM_ETC_ARCHEMY_MAGICSTONE_ATHANASIA_" + item.Iteminfo.Degree.ToString("D2"));
                if (SROBot.Inventory.IsItemEmpty(stones))
                {
                    bot.Debug("[{0}] no immortal stones found!", TargetSlot);
                    return false;
                }

                Actions.AddStone(bot, item.Slot, stones.Slot);
                new Thread(fuseTimeout).Start(++fuseHandle);

                return null;
            }

            // lucky
            if (bot.Config.Alchemy.UseLuckyStone && bot.Config.Alchemy.UseLuckyStoneAt <= item.Iteminfo.Plus && !item.BlueStats.Any(bs => bs.Key.Type.Equals("MATTR_LUCK")))
            {
                bot.Debug("[{0}] add lucky..", TargetSlot);

                var stones = bot.Inventory.GetLowestStackByType("ITEM_ETC_ARCHEMY_MAGICSTONE_LUCK_" + item.Iteminfo.Degree.ToString("D2"));
                if (SROBot.Inventory.IsItemEmpty(stones))
                {
                    bot.Debug("[{0}] no luck stones found!", TargetSlot);
                    return false;
                }

                Actions.AddStone(bot, item.Slot, stones.Slot);
                new Thread(fuseTimeout).Start(++fuseHandle);

                return null;
            }

            if (false)
            {
                var luckyCount = 2;

                // just add luckies to the items..
                var luckyBlue = item.BlueStats.FirstOrDefault(bs => bs.Key.Type.Equals("MATTR_LUCK"));
                if (bot.Config.Alchemy.UseLuckyStoneAt <= item.Iteminfo.Plus && (luckyBlue.Key == null || luckyBlue.Value < luckyCount))
                {
                    bot.Debug("[{0}] add lucky..", TargetSlot);

                    var stones = bot.Inventory.GetLowestStackByType("ITEM_ETC_ARCHEMY_MAGICSTONE_LUCK_" + item.Iteminfo.Degree.ToString("D2"));
                    if (SROBot.Inventory.IsItemEmpty(stones))
                    {
                        bot.Debug("[{0}] no luck stones found!", TargetSlot);
                        return false;
                    }

                    Actions.AddStone(bot, item.Slot, stones.Slot);
                    new Thread(fuseTimeout).Start(++fuseHandle);

                    return null;
                }
                else if (luckyBlue.Value >= luckyCount)
                {
                    ++TargetSlot;
                    return true;
                }

                return false;
            }
            else
            {
                bot.Debug("[{0}] fuse item..", TargetSlot);

                Actions.FuseItem(bot, item.Slot, elixirs.Slot, luckypowders?.Slot ?? 0);
                new Thread(fuseTimeout).Start(++fuseHandle);

                return null;
            }
        }

        private bool? blue()
        {
            var item = bot.Inventory.GetItem(TargetSlot);
            if (SROBot.Inventory.IsItemEmpty(item) || !item.Iteminfo.IsDrop) return false;

            var targetBlues = TargetBlues.Where(b => item.Iteminfo.CanGetStone(b.Key)).ToArray();
            if (targetBlues.All(b => item.BlueStats.ContainsKey(b.Key) && item.BlueStats[b.Key] >= b.Value)) return false; // we are done !

            InventoryItem stones = null;
            var blue = targetBlues.FirstOrDefault(b =>
            {
                var stoneType = b.Key.Type.Replace("MATTR", "ITEM_ETC_ARCHEMY_MAGICSTONE");
                var _stones = bot.Inventory.GetLowestStackByType(stoneType + "_" + item.Iteminfo.Degree.ToString("D2"));
                if (Inventory.IsItemEmpty(_stones))
                {
                    if (!_missing.Contains(stoneType)) _missing.Add(stoneType);
                    return false;
                }

                stones = _stones;

                return (!item.BlueStats.ContainsKey(b.Key) || item.BlueStats[b.Key] < b.Value);
            });

            if (blue.Key == null)
            {
                bot.Debug($"[{TargetSlot}] no stones found..");
                return false;
            }

            bot.Debug($"[{TargetSlot}] add blue ({blue.Key.Type}): {item.BlueStats.FirstOrDefault(b => b.Key == blue.Key).Value}/{blue.Value}");

            if (Inventory.IsItemEmpty(stones))
            {
                bot.Debug($"[{TargetSlot}] no {blue.Key.Type} stones found!");
                return false;
            }

            _curBlue = blue.Key;

            Actions.AddStone(bot, item.Slot, stones.Slot);
            new Thread(fuseTimeout).Start(++fuseHandle);

            return null;
        }

        public void StartPlus()
        {
            if (IsStartedBlues)
            {
                bot.Debug("blues already running !");
                return;
            }

            if (IsStartedFusing)
            {
                bot.Debug("stop alchemy (fusing) !");
                IsStartedFusing = false;
                return;
            }

            bot.Debug("start alchemy! {0} - {1} (>={2}) => {3}", StartSlot, EndSlot, MinPlus, TargetPlus);

            TargetSlot = StartSlot;

            while (TargetSlot <= EndSlot)
            {
                if (fuse() == null)
                {
                    IsStartedFusing = true;
                    break;
                }

                ++TargetSlot;
            }

            if (!IsStartedFusing)
            {
                bot.Debug("nothing todo..");
            }
        }

        public void StartBlue()
        {
            if (IsStartedFusing)
            {
                bot.Debug("blues already running !");
                return;
            }

            if (IsStartedBlues)
            {
                bot.Debug("stop alchemy (blues) !");
                IsStartedBlues = false;
                return;
            }

            bot.Debug("start alchemy (blues) !");

            TargetSlot = StartSlot;

            _missing.Clear();
            while (TargetSlot <= EndSlot)
            {
                if (blue() == null)
                {
                    IsStartedBlues = true;
                    break;
                }

                ++TargetSlot;
            }

            if (!IsStartedBlues)
            {
                bot.Debug("nothing todo..");
            }
        }

        public void Stop(bool resetWasActiveStates = true)
        {
            IsStartedFusing = false;
            IsStartedBlues = false;

            if (resetWasActiveStates)
            {
                fusingWasActive = IsStartedFusing;
                bluesWasActive = IsStartedBlues;
            }

        }

    }
}
