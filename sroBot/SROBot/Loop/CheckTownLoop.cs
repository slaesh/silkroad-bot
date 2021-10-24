using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {
        private long townLoopTimer = 0;
        private byte sellingSlot = 13;
        public uint CurrentNPCId = 0;

        private List<String> townScript = new List<string>();
        private int townScriptIdx = 0;
        private int townScriptNpcCnt = 0;

        private void loadTownScript()
        {
            townScript.Clear();
            townScriptIdx = 0;

            // check town ..
            var f = Path.Combine(App.ExecutingPath, "scripts", "town", IsInTown(bot.Char.CurPosition));
            bot.Debug("load town script: {0}", System.IO.Path.GetFileName(f));

            if (!File.Exists(f)) return;

            using (var sr = new StreamReader(f))
            {
                while (!sr.EndOfStream)
                {
                    townScript.Add(sr.ReadLine());
                }
            }
        }

        private bool handleRequire()
        {
            if (!LoadPathToTrainplace()) return false;

            var firstTrainScriptCmd = trainplaceScript.ToArray().FirstOrDefault();
            if (string.IsNullOrEmpty(firstTrainScriptCmd) || !firstTrainScriptCmd.StartsWith("require", StringComparison.OrdinalIgnoreCase) || !firstTrainScriptCmd.Contains(";"))
            {
                bot.Log($"handleRequire(): bad require cmd: {firstTrainScriptCmd ?? "--NONE--"}");
                return false;
            }

            var town = firstTrainScriptCmd.Split(';')[1];
            if (string.IsNullOrEmpty(town))
            {
                bot.Log($"handleRequire(): bad town: {town ?? "--NONE--"}");
                return false;
            }

            var curTown = IsInTown(bot.Char.CurPosition).Replace(".txt", "");
            if (!string.Equals(curTown, town, StringComparison.OrdinalIgnoreCase))
            {
                bot.Log($"{curTown} != {town} .. trying to teleport !");

                handleTeleport(town);

                return true; // skip next town-command
            }
            else
            {
                bot.Debug($"{curTown} == {town} ..");

                UInt32 portalId = 0;

                // sometimes we are not to close to get it as a spawn ..
                switch (curTown)
                {
                    case "jangan":

                        break;

                    case "downhang":
                        portalId = 42;
                        break;

                    case "hotan":
                        portalId = 25;
                        break;
                }

                var portal = bot.Spawns.Gates.GetAll().FirstOrDefault();
                if (portal != null || portalId != 0)
                {
                    bot.Debug("change ressurect point !");

                    Actions.SetDesignatePoint(bot, portal?.IngameId ?? portalId);
                }
                else
                {
                    bot.Log("no portal found ..");
                    Stop(Statistic.STOP_REASON.NO_TELEPORT_PORTAL_FOUND);
                }
            }

            return false;
        }

        private void handleGoCmd(String arg)
        {
            castSpeedBuff();

            var argSplitted = arg.Split(',');
            var p = new Point(int.Parse(argSplitted[0]), int.Parse(argSplitted[1]));

            var time2wait = (uint)(Movement.CalculateTime(p, bot.Char.CurPosition, bot.Char.Speed) * 0.9);

            //bot.Debug("{0} | townscript: walk to: {1}", DateTime.Now.ToString("HH:mm:ss.fff"), p);

            Movement.WalkTo(bot, p);
            Movement.WalkTo(bot, p);
            Movement.WalkTo(bot, p);

            townLoopTimer = time2wait;
            ++townScriptIdx;
        }

        private SROData.NPC GetShop(uint npcId)
        {
            var npc = bot.Spawns.Shops.Get(npcId);
            if (npc == null)
            {
                bot.Debug("Loop.GetShop: could not find NPC with curId: {0}", npcId);
                return null;
            }

            var shop = SROData.NPCs.GetByModel(npc.Mobinfo.Model);
            if (shop == null)
            {
                bot.Debug("Loop.GetShop: could not find shop with model: {0}", npc.Mobinfo.Model);
            }

            return shop;
        }

        private bool checkItemSell(InventoryItem invitem)
        {
            string type = invitem.Iteminfo.Type;
            string name = invitem.Iteminfo.Name;

            if ((invitem.Iteminfo.Level >= 120 && invitem.Iteminfo.SOX == SOX_TYPE.SoSUN) ||
                invitem.Iteminfo.Plus >= 20 ||
                GetConsignmentRule(invitem, false /* do not check MinAmount */) != null ||
                GetStoringRule(invitem) != null
                )
            {
                return false; // do NOT sell
            }

            if (type != null && type.Contains("ITEM_MALL") == false)
            {
                if (invitem.Iteminfo.IsWeapon)
                {
                    return true; // sell
                }
                if (invitem.Iteminfo.IsShield)
                {
                    return true; // sell
                }
                if (invitem.Iteminfo.IsArmor)
                {
                    return true; // sell
                }
                if (invitem.Iteminfo.IsAccessory)
                {
                    return true; // sell
                }

                if (type.StartsWith("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_WEAPON"))
                {
                    // weap elixir
                }
                if (type.StartsWith("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_SHIELD"))
                {
                    // shield elixir
                }
                if (type.StartsWith("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_ARMOR"))
                {
                    // armor elixir
                }
                if (type.StartsWith("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_ACCESSARY"))
                {
                    // accs elixir
                }
                if (type.StartsWith("ITEM_ETC_ARCHEMY_ATTRTABLET") || type.StartsWith("ITEM_ETC_ARCHEMY_MAGICSTONE"))
                {

                }
                if (type.StartsWith("ITEM_ETC_ARCHEMY_MATERIAL"))
                {

                }
            }

            return false;
        }

        private void handleBuyHerbalist()
        {
            int mpsToBuy = bot.Config.Loop.MpPotsAmount - (int)bot.Inventory.GetAmountOf("MP Recovery Potion");
            int hpsToBuy = bot.Config.Loop.HpPotsAmount - (int)bot.Inventory.GetAmountOf("HP Recovery Potion");

            var shop = GetShop(CurrentNPCId);
            if (shop == null)
            {
                bot.Log($"Loop.NPC.Buy: shop with CurrentId: {CurrentNPCId} not found!");

                NpcState = LOOP_NPC_STATES.Closing;
                townLoopTimer = 200;
                return;
            }

            var biggestMpPot = shop.Tabs.SelectMany(t => t.ItemModels.Select(i => i.Model)).Select(i => ItemInfos.GetById(i)).LastOrDefault(i => i.Name.StartsWith("MP Recovery Potion"));
            var biggestHpPot = shop.Tabs.SelectMany(t => t.ItemModels.Select(i => i.Model)).Select(i => ItemInfos.GetById(i)).LastOrDefault(i => i.Name.StartsWith("HP Recovery Potion"));

            if (bot.Config.Loop.BuyMpPots && mpsToBuy > 0 && biggestMpPot != null && shop.Tabs.SelectMany(t => t.ItemModels).FirstOrDefault(i => i.Model == biggestMpPot.Model).Price <= bot.Char.Gold)
            {
                bot.Debug("need to buy {0} MP pots", mpsToBuy);

                if (!Actions.BuyItem(CurrentNPCId, biggestMpPot, (ushort)mpsToBuy, bot))
                {
                    bot.Log("Loop.NPC.Buy: could not buy MP pots..!");

                    NpcState = LOOP_NPC_STATES.Closing;
                    townLoopTimer = 200;
                }
                else townLoopTimer = 60 * 1000;
            }
            else if (bot.Config.Loop.BuyHpPots && hpsToBuy > 0 && biggestHpPot != null && shop.Tabs.SelectMany(t => t.ItemModels).FirstOrDefault(i => i.Model == biggestHpPot.Model).Price <= bot.Char.Gold)
            {
                bot.Debug("need to buy {0} HP pots", hpsToBuy);

                if (!Actions.BuyItem(CurrentNPCId, biggestHpPot, (ushort)hpsToBuy, bot))
                {
                    bot.Log("Loop.NPC.Buy: could not buy HP pots..!");

                    NpcState = LOOP_NPC_STATES.Closing;
                    townLoopTimer = 200;
                }
                else townLoopTimer = 60 * 1000;
            }
            else
            {
                NpcState = LOOP_NPC_STATES.Closing;
                townLoopTimer = 200;
            }
        }

        private void handleBuyBlacksmith()
        {
            var shop = GetShop(CurrentNPCId);
            if (shop == null)
            {
                bot.Log($"Loop.NPC.Buy: shop with CurrentId: {CurrentNPCId} not found!");

                NpcState = LOOP_NPC_STATES.Closing;
                townLoopTimer = 200;
                return;
            }

            var weapon = bot.GetWeapon();
            var usingArrows = weapon != null && weapon.Iteminfo.Type.Contains("_BOW_");
            var usingBolts = weapon != null && weapon.Iteminfo.Type.Contains("_CROSSBOW_");

            ItemInfo ammoToBuy = null;
            int ammoCount = bot.Config.Loop.ArrowsBoltsAmount;

            if (usingArrows)
            {
                ammoToBuy = ItemInfos.ItemList.FirstOrDefault(i => i.Name.StartsWith("Arrow"));
                ammoCount -= (int)bot.Inventory.GetAmountOf("Arrow");
            }
            else if (usingBolts)
            {
                ammoToBuy = ItemInfos.ItemList.FirstOrDefault(i => i.Name.StartsWith("Bolt"));
                ammoCount -= (int)bot.Inventory.GetAmountOf("Bolt");
            }
            else
            {
                ammoCount = 0;
            }

            if (bot.Config.Loop.BuyArrowsBolts && ammoCount > 0 && ammoToBuy != null && (shop.Tabs.SelectMany(t => t.ItemModels).FirstOrDefault(i => i.Model == ammoToBuy.Model).Price * 1.2) <= bot.Char.Gold)
            {
                Actions.BuyItem(CurrentNPCId, ammoToBuy, (ushort)ammoCount, bot);
                townLoopTimer = 60 * 1000;
            }
            else
            {
                SROData.NpcItem itemToBuy = null;

                if (bot.Config.Loop.BuyBetterWeapons)
                {
                    var possibleShopItems = shop?.Tabs
                                                .SelectMany(t => t.ItemModels)
                                                .Where(i =>
                                                {
                                                    var iteminfo = ItemInfos.GetById(i.Model);
                                                    return iteminfo.Type.Contains("_CH_") && iteminfo.Level <= bot.Char.Level;
                                                });

                    var shopsBestWeapon = possibleShopItems.Where(i => ItemInfos.GetById(i.Model)?.Type.Contains(weapon.Iteminfo.GetWeaponType()) == true)
                                                            .OrderBy(i => ItemInfos.GetById(i.Model)?.Level)
                                                            .LastOrDefault();

                    if (shopsBestWeapon != null && (ItemInfos.GetById(shopsBestWeapon.Model).IsBetterThan(weapon.Iteminfo) || Inventory.IsItemEmpty(weapon)))
                    {
                        bot.Log($"we can buy a better weapon.. {shopsBestWeapon.Model}");

                        itemToBuy = shopsBestWeapon;
                    }

                    var weapType = weapon.Iteminfo.GetWeaponType();
                    if (itemToBuy == null && (weapType == "SWORD" || weapType == "BLADE"))
                    {
                        var myShield = bot.Inventory.GetItem(7);
                        var shopsBestShield = possibleShopItems.Where(i => ItemInfos.GetById(i.Model)?.Type.Contains("_SHIELD") == true)
                                                                .OrderBy(i => ItemInfos.GetById(i.Model)?.Level)
                                                                .LastOrDefault();

                        if (shopsBestShield != null && (ItemInfos.GetById(shopsBestShield.Model).IsBetterThan(myShield.Iteminfo) || Inventory.IsItemEmpty(myShield)))
                        {
                            bot.Log($"we can buy a better shield.. {shopsBestShield.Model}");

                            itemToBuy = shopsBestShield;
                        }
                    }
                }

                if (itemToBuy != null && (itemToBuy.Price * 1.2) > bot.Char.Gold)
                {
                    bot.Log($"price is to high!! .. {(itemToBuy.Price * 1.2):N0} > {bot.Char.Gold:N0}");

                    NpcState = LOOP_NPC_STATES.Repairing;
                    townLoopTimer = 200;
                }
                else if (itemToBuy != null)
                {
                    Actions.BuyItem(CurrentNPCId, ItemInfos.GetById(itemToBuy.Model), 1, bot);
                    townLoopTimer = 60 * 1000;
                }
                else
                {
                    NpcState = LOOP_NPC_STATES.Repairing;
                    townLoopTimer = 200;
                }
            }
        }

        private void handleBuyJewel()
        {
            SROData.NpcItem itemToBuy = null;
            var npc = bot.Spawns.Shops.Get(CurrentNPCId);
            var shop = SROData.NPCs.GetByModel(npc?.Mobinfo.Model ?? 0);
            ushort buyAmount = 1;

            var curRetScrollAmount = bot.Inventory.GetAmountOf("Return Sc");
            if (bot.Config.Loop.BuyReturnScrolls && curRetScrollAmount < bot.Config.Loop.ReturnScrollsAmount)
            {
                var retScroll = shop?.Tabs
                                    .SelectMany(t => t.ItemModels)
                                    .FirstOrDefault(i =>
                                    {
                                        var iteminfo = ItemInfos.GetById(i.Model);
                                        return iteminfo?.Type == "ITEM_ETC_SCROLL_RETURN_01";
                                    });

                buyAmount = (ushort)(bot.Config.Loop.ReturnScrollsAmount - curRetScrollAmount);
                itemToBuy = retScroll;
            }

            if (itemToBuy == null && bot.Config.Loop.BuyBetterAccessories)
            {
                buyAmount = 1;

                //var itemToBuy = shop.Tabs.SelectMany(t => t.ItemModels).FirstOrDefault(i => true);

                var possibleShopItems = shop?.Tabs
                                            .SelectMany(t => t.ItemModels)
                                            .Where(i =>
                                            {
                                                var iteminfo = ItemInfos.GetById(i.Model);
                                                return iteminfo.Type.Contains("_CH_") && iteminfo.Level <= bot.Char.Level;
                                            });

                var myEarring = bot.Inventory.GetItem(9);
                var shopsBestEarring = possibleShopItems.Where(i => ItemInfos.GetById(i.Model)?.Type.Contains("_EARRING") == true).OrderBy(i => ItemInfos.GetById(i.Model)?.Level).LastOrDefault();

                if (shopsBestEarring != null && (ItemInfos.GetById(shopsBestEarring.Model).IsBetterThan(myEarring.Iteminfo) || Inventory.IsItemEmpty(myEarring)))
                {
                    bot.Log($"we can buy a better earring.. {shopsBestEarring.Model}");

                    itemToBuy = shopsBestEarring;
                }

                if (itemToBuy == null)
                {
                    var myNecklace = bot.Inventory.GetItem(10);
                    var shopsBestNecklace = possibleShopItems.Where(i => ItemInfos.GetById(i.Model)?.Type.Contains("_NECKLACE") == true).OrderBy(i => ItemInfos.GetById(i.Model)?.Level).LastOrDefault();

                    if (shopsBestNecklace != null && (ItemInfos.GetById(shopsBestNecklace.Model).IsBetterThan(myNecklace.Iteminfo) || Inventory.IsItemEmpty(myNecklace)))
                    {
                        bot.Log($"we can buy a better necklace.. {shopsBestNecklace.Model}");

                        itemToBuy = shopsBestNecklace;
                    }

                    if (itemToBuy == null)
                    {
                        var myRing1 = bot.Inventory.GetItem(11);
                        var shopsBestRing = possibleShopItems.Where(i => ItemInfos.GetById(i.Model)?.Type.Contains("_RING") == true).OrderBy(i => ItemInfos.GetById(i.Model)?.Level).LastOrDefault();

                        if (shopsBestRing != null && (ItemInfos.GetById(shopsBestRing.Model).IsBetterThan(myRing1.Iteminfo) || Inventory.IsItemEmpty(myRing1)))
                        {
                            bot.Log($"we can buy a better ring1.. {shopsBestRing.Model}");

                            itemToBuy = shopsBestRing;
                        }

                        if (itemToBuy == null)
                        {
                            var myRing2 = bot.Inventory.GetItem(12);

                            if (shopsBestRing != null && (ItemInfos.GetById(shopsBestRing.Model).IsBetterThan(myRing2.Iteminfo) || Inventory.IsItemEmpty(myRing2)))
                            {
                                bot.Log($"we can buy a better ring2.. {shopsBestRing.Model}");

                                itemToBuy = shopsBestRing;
                            }

                        }
                    }
                }
            }

            if (itemToBuy != null && (itemToBuy.Price * 1.2) > bot.Char.Gold)
            {
                bot.Log($"price is to high!! .. {(itemToBuy.Price * 1.2):N0} > {bot.Char.Gold:N0}");

                NpcState = LOOP_NPC_STATES.Repairing;
                townLoopTimer = 200;
            }
            else if (itemToBuy != null)
            {
                Actions.BuyItem(CurrentNPCId, ItemInfos.GetById(itemToBuy.Model), buyAmount, bot);
                townLoopTimer = 60 * 1000;
            }
            else
            {
                NpcState = LOOP_NPC_STATES.Repairing;
                townLoopTimer = 200;
            }
        }

        private void handleBuyProtector()
        {
            SROData.NpcItem itemToBuy = null;
            if (bot.Config.Loop.BuyBetterArmorparts)
            {
                var npc = bot.Spawns.Shops.Get(CurrentNPCId);
                var shop = SROData.NPCs.GetByModel(npc?.Mobinfo.Model ?? 0);
                var possibleShopItems = shop?.Tabs
                                            .SelectMany(t => t.ItemModels)
                                            .Where(i =>
                                            {
                                                var iteminfo = ItemInfos.GetById(i.Model);
                                                return iteminfo.Type.Contains("_CH_") && iteminfo.Level <= bot.Char.Level;
                                            });

                var myGender = bot.Inventory.GetGender();
                if (myGender != null)
                {
                    possibleShopItems = possibleShopItems.Where(i => ItemInfos.GetById(i.Model)?.Type.Contains("_" + myGender + "_") == true);

                    var myArmorType = bot.Inventory.GetArmorType();
                    switch (myArmorType)
                    {
                        case Inventory.ARMOR_TYPE.GARMENT:
                            possibleShopItems = possibleShopItems.Where(i => ItemInfos.GetById(i.Model)?.Type.Contains("_CLOTHES") == true);
                            break;

                        case Inventory.ARMOR_TYPE.ARMOR:
                            possibleShopItems = possibleShopItems.Where(i => ItemInfos.GetById(i.Model)?.Type.Contains("_HEAVY") == true);
                            break;

                        case Inventory.ARMOR_TYPE.MIXED: // PREFERRE PROTECTOR ?!
                        case Inventory.ARMOR_TYPE.PROTECTOR:
                            possibleShopItems = possibleShopItems.Where(i => ItemInfos.GetById(i.Model)?.Type.Contains("_LIGHT") == true);
                            break;
                    }

                    for (byte armorPartSlot = 0; armorPartSlot < 6; armorPartSlot++)
                    {
                        var myArmorPart = bot.Inventory.GetItem(armorPartSlot);
                        var armorPartTypes = new List<string>();

                        switch (armorPartSlot)
                        {
                            case 0:
                                // there are 2 different heads .. ?! (head and corone)
                                armorPartTypes.Add("_HA_");
                                armorPartTypes.Add("_CA_");
                                break;
                            case 1:
                                armorPartTypes.Add("_BA_");
                                break;
                            case 2:
                                armorPartTypes.Add("_SA_");
                                break;
                            case 3:
                                armorPartTypes.Add("_AA_");
                                break;
                            case 4:
                                armorPartTypes.Add("_LA_");
                                break;
                            case 5:
                                armorPartTypes.Add("_FA_");
                                break;
                        }

                        var shopsBestArmorPart = possibleShopItems.Where(i => armorPartTypes.Any(ap => ItemInfos.GetById(i.Model)?.Type.Contains(ap) == true))
                                                            .OrderBy(i => ItemInfos.GetById(i.Model)?.Level)
                                                            .LastOrDefault();

                        if (shopsBestArmorPart != null && (ItemInfos.GetById(shopsBestArmorPart.Model).IsBetterThan(myArmorPart.Iteminfo) || Inventory.IsItemEmpty(myArmorPart)))
                        {
                            bot.Log($"we can buy a better armorpart({armorPartSlot}).. {shopsBestArmorPart.Model}");

                            itemToBuy = shopsBestArmorPart;
                        }

                        if (itemToBuy != null) break;
                    }
                }
            }

            if (itemToBuy != null && (itemToBuy.Price * 1.2) > bot.Char.Gold)
            {
                bot.Log($"price is to high!! .. {(itemToBuy.Price * 1.2):N0} > {bot.Char.Gold:N0}");

                NpcState = LOOP_NPC_STATES.Repairing;
                townLoopTimer = 200;
            }
            else if (itemToBuy != null)
            {
                Actions.BuyItem(CurrentNPCId, ItemInfos.GetById(itemToBuy.Model), 1, bot);
                townLoopTimer = 60 * 1000;
            }
            else
            {
                NpcState = LOOP_NPC_STATES.Repairing;
                townLoopTimer = 200;
            }
        }

        private void handleOpenStorage()
        {
            var talkToStorage = AnyItemForStorage();

            bot.Log($"open storage{(talkToStorage ? "" : " -- NOTHING TO DO!")}!");

            if (talkToStorage)
            {
                SROBot.Actions.StorageOpen(CurrentNPCId, bot);
                townLoopTimer = 60 * 1000;
            }
            else
            {
                NpcState = LOOP_NPC_STATES.Closing;
                townLoopTimer = 100;
            }
        }

        private void handleOpenGuildStorage()
        {

        }

        private void handleGetStorage()
        {
            bot.Debug($"request storage!");
            SROBot.Actions.GetStorageList(CurrentNPCId, bot);

            townLoopTimer = 60 * 1000;
        }

        private void handleGetGuildStorage()
        {

        }

        private IEnumerable<InventoryItem> GetItemsForStorage()
        {
            //return bot.Inventory.GetItems(i =>
            //        i.Slot >= 13 && !GetItemsForConsignment().Select(ci => ci.Slot).Contains(i.Slot) && // only if its NOT for sale in consignment .. :)
            //        (
            //        (i.Iteminfo.Type.StartsWith("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_") && i.Iteminfo.Type.EndsWith("_B")) || // all kind of elixirs
            //        (i.Iteminfo.Type.EndsWith("_C_RARE") && i.Iteminfo.Type.Contains("_13_")) || // d13 sun items
            //        i.Iteminfo.Type.Equals("ITEM_ETC_ARCHEMY_MAGICSTONE_LUCK_13") // d13 luck stones
            //        )
            //    );

            if (!bot.Config.Storage.StoringConfiguration.Any()) return new InventoryItem[0];

            return bot.Inventory.GetItems(i => i.Slot >= 13)
                                .Where(i => GetConsignmentRule(i, false /* do not check MinAmount */) == null) // only if NO consignment-rule is found !
                                .Where(i => GetStoringRule(i) != null || i.Iteminfo.Plus > 11)
                                .ToArray();
        }

        private Configuration.StoringItemOptions GetStoringRule(InventoryItem item)
        {
            if (item == null) return null;

            return bot.Config.Storage.StoringConfiguration.FirstOrDefault(sc => sc.Store && sc.Match(item, true));
        }

        private bool AnyItemForStorage()
        {
            return GetItemsForStorage().Any();
        }

        private InventoryItem FirstItemForStorage()
        {
            return GetItemsForStorage().FirstOrDefault();
        }

        private void handleStoring()
        {
            if (bot.Storage.IsFull(0))
            {
                bot.Log($"storage is FULL!");

                NpcState = LOOP_NPC_STATES.MergeStorage;
                townLoopTimer = 100;
                return;
            }

            ulong minGoldAmount = 500000;
            if (bot.Char.Gold < minGoldAmount)
            {
                bot.Log($"not enough gold.. myGold: {bot.Char.Gold:N0} < {minGoldAmount:N0}");

                NpcState = LOOP_NPC_STATES.Closing;
                townLoopTimer = 100;
                return;
            }

            var itemToStore = FirstItemForStorage();

            if (Inventory.IsItemEmpty(itemToStore))
            {
                NpcState = LOOP_NPC_STATES.MergeStorage;
                townLoopTimer = 200;
                return;
            }

            bot.Log($"put {itemToStore.Iteminfo.Type}({itemToStore.Slot}) to storage!");

            SROBot.Actions.PutItemToStorage(CurrentNPCId, itemToStore.Slot, bot);
            townLoopTimer = 60 * 1000;
        }

        private void handleGuildStoring()
        {

        }

        public void handleMergeStorage()
        {
            if (bot.Storage.MergeStorageItems(CurrentNPCId))
            {
                bot.Log("Storage: merging..");
                townLoopTimer = 60 * 1000;
                return;
            }

            bot.Log("Storage: merging.. done!");

            NpcState = LOOP_NPC_STATES.Closing;
            townLoopTimer = 200;
        }

        public void handleMergeGuildStorage()
        {
            NpcState = LOOP_NPC_STATES.Closing;
            townLoopTimer = 200;
        }

        private IEnumerable<InventoryItem> GetItemsForConsignment()
        {
            //return bot.Inventory.GetItems(i => i.Slot >= 13 && (
            //        (i.Iteminfo.Type.StartsWith("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_W") && i.Iteminfo.Type.EndsWith("_B") && i.Count == 1000) || // weapon elixirs
            //        (i.Iteminfo.Type.EndsWith("_C_RARE") && i.Iteminfo.Type.Contains("_13_") && (i.Iteminfo.Type.Contains("_W_") || i.Iteminfo.Type.Contains("_M_")) && i.WhiteStats.DurabilityPercentage < 90) // d13 sun items (armor-parts) -- EXCEPT WEB-SHOP !!
            //    ));

            if (!bot.Config.Consignment.SellConfiguration.Any()) return new InventoryItem[0];

            return bot.Inventory.GetItems(i => i.Slot >= 13).Where(i => GetConsignmentRule(i, true) != null && i.Iteminfo.Plus < 11).ToArray();
        }

        private Configuration.ConsignmentSellOptions GetConsignmentRule(InventoryItem item, bool checkMinAmount)
        {
            if (item == null) return null;

            return bot.Config.Consignment.SellConfiguration.FirstOrDefault(sc => sc.Sell && sc.PricePerPiece > 0 && sc.Match(item, checkMinAmount));
        }

        private bool AnythingToSettle()
        {
            return bot.ConsignmentItems.Any(ci => ci.Sold);
        }

        private bool AnythingToAbort()
        {
            return bot.ConsignmentItems.Any(ci => ci.Expired);
        }

        private bool AnyItemForConsignment()
        {
            return GetItemsForConsignment().Any();
        }

        private InventoryItem FirstItemForConsignment()
        {
            return GetItemsForConsignment().FirstOrDefault();
        }

        private Consignment.ConsignmentItem FirstItemToAbort()
        {
            return bot.ConsignmentItems.FirstOrDefault(ci => ci.Expired);
        }

        private void handleConsignmentOpen()
        {
            var talkToConsignment = (AnythingToSettle() || AnyItemForConsignment() || AnythingToAbort()) && bot.Config.Consignment.UseInLoop;

            bot.Log($"open consignment{(talkToConsignment ? $" settle: {AnythingToSettle()}, abort: {AnythingToAbort()}, register: {AnyItemForConsignment()}" : " -- NOTHING TO DO!")}");

            if (talkToConsignment)
            {
                _isUsingConsignment = true;

                bot.Consig = new Consignment(bot);

                Actions.ConsignmentOpen(bot, CurrentNPCId);
                townLoopTimer = 60 * 1000;
            }
            else
            {
                townLoopTimer = 100;
                NpcState = LOOP_NPC_STATES.Closing;
            }
        }

        private void handleNpcCmd(String arg)
        {
            var targetNpc = arg.ToLower();

            if (townScriptNpcCnt == 1)
            {
                NpcState = LOOP_NPC_STATES.Selecting;
            }

            switch (NpcState)
            {
                case LOOP_NPC_STATES.Selecting:
                    #region selecting
                    {
                        if (bot.Config.HalloweenEventSpecial || bot.Config.Training.StopBotOnTrainplace)
                        {
                            NpcState = LOOP_NPC_STATES.Done;
                            townLoopTimer = 100;
                            break;
                        }

                        //bot.Debug("townscript: select NPC: {1}", DateTime.Now.ToString("HH:mm:ss.fff"), targetNpc);

                        var npc = bot.Spawns.Shops.GetByName(targetNpc);

                        targetNpc = targetNpc.Replace(" ", "-").Replace("_", "-");

                        switch (targetNpc)
                        {
                            case "herbalist":
                                npc = bot.Spawns.Shops.GetByType("potion");
                                break;

                            case "blacksmith":
                                npc = bot.Spawns.Shops.GetByType("_SMITH");
                                break;

                            case "jewel":
                                npc = bot.Spawns.Shops.GetByType("_ACCESSORY");
                                break;

                            case "protector":
                                npc = bot.Spawns.Shops.GetByType("_ARMOR");
                                break;

                            case "consignment":
                                npc = bot.Spawns.Shops.GetByType("_JUEL");
                                break;

                            case "storage":
                                npc = bot.Spawns.Shops.GetAll().FirstOrDefault(s => s.Mobinfo.Type.EndsWith("WAREHOUSE_M") || s.Mobinfo.Type.EndsWith("WAREHOUSE"));
                                break;

                            case "guild-storage":
                                npc = bot.Spawns.Shops.GetAll().FirstOrDefault(s => s.Mobinfo.Type.EndsWith("_GUILD"));
                                NpcState = LOOP_NPC_STATES.Done;
                                townLoopTimer = 200;
                                bot.Debug("just skip guild-storage !");
                                return;
                                break;

                            default:
                                bot.Log($"unknown NPC: '{targetNpc}'");
                                break;
                        }

                        if (npc == null)
                        {
                            bot.Log("townscript: could not find NPC: {1}", DateTime.Now.ToString("HH:mm:ss.fff"), targetNpc);
                            Stop(Statistic.STOP_REASON.COULD_NOT_FIND_NPC);
                            break;
                        }

                        CurrentNPCId = npc.UID;
                        Actions.NPCSelect(CurrentNPCId, bot);

                        NpcState = LOOP_NPC_STATES.Opening;
                        townLoopTimer = 60 * 1000;
                    }
                    #endregion
                    break;

                case LOOP_NPC_STATES.Opening:
                    #region opening
                    {
                        Actions.NPCDeselect(CurrentNPCId, bot);

                        switch (targetNpc)
                        {
                            case "herbalist":
                            case "blacksmith":
                            case "jewel":
                            case "protector":
                                NpcState = LOOP_NPC_STATES.Selling;
                                sellingSlot = 13;
                                break;

                            case "consignment":
                                NpcState = LOOP_NPC_STATES.ConsignmentOpen;
                                break;

                            case "storage":
                            case "guild-storage":
                                NpcState = LOOP_NPC_STATES.OpenStorage;
                                break;

                            default:
                                NpcState = LOOP_NPC_STATES.Closing;
                                break;
                        }

                        townLoopTimer = 60 * 1000;
                    }
                    #endregion
                    break;

                case LOOP_NPC_STATES.Selling:
                    #region selling
                    {
                        var putItemsFromPetToInventory = bot.Char.Pickpet != null && !bot.Char.Pickpet.Inventory.IsEmpty(0);
                        InventoryItem petItem = null;

                        if (putItemsFromPetToInventory)
                        {
                            petItem = bot.Char.Pickpet.Inventory.GetItems(i => Inventory.IsItemNotEmpty(i)).FirstOrDefault();
                            var petItemCnt = bot.Char.Pickpet.Inventory.Size - bot.Char.Pickpet.Inventory.FreeSlots(0);

                            if (Inventory.IsItemNotEmpty(petItem) && bot.Inventory.FreeSlots() >= petItemCnt)
                            {
                                bot.Debug("take item from pet .. !");

                                sellingSlot = 13;
                                Actions.PetToInventory(petItem.Slot, bot);

                                townLoopTimer = 60 * 1000;
                                return;
                            }
                        }

                        InventoryItem invitem = null;
                        while (SROBot.Inventory.IsItemEmpty(invitem) && sellingSlot < bot.Inventory.Size)
                        {
                            invitem = bot.Inventory.GetItem(sellingSlot++);

                            if (SROBot.Inventory.IsItemEmpty(invitem) || !checkItemSell(invitem))
                            {
                                invitem = null;
                                continue;
                            }
                        }

                        if (SROBot.Inventory.IsItemNotEmpty(invitem))
                        {
                            bot.Debug($"sell item .. {invitem.Slot}");

                            Actions.SellItem(invitem.Slot, invitem.Count, CurrentNPCId, bot);
                            --sellingSlot; // stay at current slot -> waiting for feedback from server !

                            townLoopTimer = 60 * 1000; // timeout, not the timer for next element !!
                            return;
                        }

                        if (sellingSlot >= bot.Inventory.Size)
                        {
                            if (!putItemsFromPetToInventory || Inventory.IsItemEmpty(petItem) || bot.Inventory.IsFull())
                            {
                                bot.Debug("all sold and no items in pet..");

                                NpcState = LOOP_NPC_STATES.Buying;
                                townLoopTimer = 200;
                            }
                            else
                            {
                                bot.Debug("take item from pet ..");

                                invitem = null;
                                sellingSlot = 13;
                                Actions.PetToInventory(petItem.Slot, bot);

                                townLoopTimer = 60 * 1000;
                            }
                        }
                    }
                    #endregion
                    break;

                case LOOP_NPC_STATES.Buying:
                    #region buying
                    {
                        if (bot.Inventory.IsFull())
                        {
                            bot.Log("inventory full, cant buy any more.. - stop bot!");
                            Stop(Statistic.STOP_REASON.INVENTORY_FULL);
                            return;
                        }

                        switch (targetNpc)
                        {
                            case "blacksmith":
                                handleBuyBlacksmith();
                                break;

                            case "herbalist":
                                handleBuyHerbalist();
                                break;

                            case "jewel":
                                handleBuyJewel();
                                break;

                            case "protector":
                                handleBuyProtector();
                                break;

                            default:
                                NpcState = LOOP_NPC_STATES.Closing;
                                townLoopTimer = 200;
                                break;
                        }
                    }
                    #endregion
                    break;

                #region storage

                case LOOP_NPC_STATES.OpenStorage:
                    #region open storage
                    if (bot.Config.Storage.UseInLoop)
                    {
                        _isStoring = true;

                        switch (targetNpc)
                        {
                            case "storage":
                                handleOpenStorage();
                                break;

                            case "guild-storage":
                                handleOpenGuildStorage();
                                break;

                            default:
                                NpcState = LOOP_NPC_STATES.Closing;
                                townLoopTimer = 100;
                                break;
                        }
                    }
                    else
                    {
                        NpcState = LOOP_NPC_STATES.Closing;
                        townLoopTimer = 100;
                    }
                    #endregion
                    break;

                case LOOP_NPC_STATES.GetStorage:
                    #region get storage
                    switch (targetNpc)
                    {
                        case "storage":
                            handleGetStorage();
                            break;

                        case "guild-storage":
                            handleGetGuildStorage();
                            break;

                        default:
                            NpcState = LOOP_NPC_STATES.Closing;
                            townLoopTimer = 200;
                            break;
                    }
                    #endregion
                    break;

                case LOOP_NPC_STATES.Storing:
                    #region storing
                    switch (targetNpc)
                    {
                        case "storage":
                            handleStoring();
                            break;

                        case "guild-storage":
                            handleGuildStoring();
                            break;

                        default:
                            NpcState = LOOP_NPC_STATES.Closing;
                            townLoopTimer = 200;
                            break;
                    }
                    #endregion
                    break;

                case LOOP_NPC_STATES.MergeStorage:
                    #region merge storage
                    switch (targetNpc)
                    {
                        case "storage":
                            handleMergeStorage();
                            break;

                        case "guild-storage":
                            handleMergeGuildStorage();
                            break;

                        default:
                            NpcState = LOOP_NPC_STATES.Closing;
                            townLoopTimer = 200;
                            break;
                    }
                    #endregion
                    break;

                #endregion

                #region consignment

                case LOOP_NPC_STATES.ConsignmentOpen:
                    handleConsignmentOpen();
                    break;

                case LOOP_NPC_STATES.ConsignmentSettle:
                    if (AnythingToSettle())
                    {
                        bot.Debug($"settle consignment");

                        Actions.ConsignmentSettle(bot);
                        townLoopTimer = 60 * 1000;
                    }
                    else
                    {
                        bot.Debug($"NOTHING to settle");

                        NpcState = LOOP_NPC_STATES.ConsignmentAbortExpired;
                        townLoopTimer = 100;
                    }
                    break;

                case LOOP_NPC_STATES.ConsignmentAbortExpired:
                    {
                        var itemToAbort = FirstItemToAbort();
                        if (AnythingToAbort() && itemToAbort != null)
                        {
                            bot.Debug($"abort consignment item {itemToAbort.Item.Type}({itemToAbort.ConsigId})");

                            Actions.ConsignmentAbortItem(bot, itemToAbort);
                            townLoopTimer = 60 * 1000;
                        }
                        else
                        {
                            bot.Debug($"NOTHING to abort");

                            NpcState = LOOP_NPC_STATES.ConsignmentPutNew;
                            townLoopTimer = 100;
                        }
                    }
                    break;

                case LOOP_NPC_STATES.ConsignmentPutNew:
                    {
                        var itemToRegister = FirstItemForConsignment();
                        var sellConfig = GetConsignmentRule(itemToRegister, true);

                        if (bot.ConsignmentItems.Count < 10 && AnyItemForConsignment() && itemToRegister != null && sellConfig != null && (sellConfig.PricePerPiece * itemToRegister.Count * 0.15) <= bot.Char.Gold)
                        {
                            var price = sellConfig.PricePerPiece * itemToRegister.Count;

                            bot.Debug($"register item {itemToRegister.Iteminfo.Type}({itemToRegister.Slot}) for {price:N0}");

                            Actions.ConsignmentRegisterItem(bot, itemToRegister, price);
                            townLoopTimer = 60 * 1000;
                        }
                        else
                        {
                            NpcState = LOOP_NPC_STATES.ConsignmentSearch;
                            townLoopTimer = 100;
                        }
                    }
                    break;

                case LOOP_NPC_STATES.ConsignmentSearch:
                    bot.Debug("consignmentSearch..");

                    bot.Consig.Search(33 /* elixirs */, 0, (items) =>
                    {
                        foreach (var item in items.Where(i => i.Item?.Type == "ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_WEAPON_B" && (i.Price / i.Count) <= 400000).ToArray())
                        {
                            bot.Log($"we should buy these elixirs: {item.Player}: {item.Price:N0} -- {item.Count}");
                        }

                        NpcState = LOOP_NPC_STATES.ConsignmentBuy;
                        townLoopTimer = 100;
                    });

                    townLoopTimer = 120 * 1000;
                    break;

                case LOOP_NPC_STATES.ConsignmentBuy:
                    NpcState = LOOP_NPC_STATES.ConsignmentClose;
                    townLoopTimer = 100;
                    break;

                case LOOP_NPC_STATES.ConsignmentClose:
                    bot.Log("close consignment!");

                    Actions.ConsignmentClose(bot);
                    townLoopTimer = 60 * 1000;
                    break;

                #endregion

                case LOOP_NPC_STATES.Repairing:
                    #region repairing
                    {
                        if (targetNpc == "blacksmith")
                        {
                            Actions.RepairAll(CurrentNPCId, bot);
                        }

                        NpcState = LOOP_NPC_STATES.Closing;
                        townLoopTimer = 200;
                    }
                    #endregion
                    break;

                case LOOP_NPC_STATES.Closing:
                    #region closing
                    {
                        _isStoring = false;
                        _isUsingConsignment = false;
                        bot.Consig = null;

                        //bot.Debug("close NPC");

                        Actions.NPCDeselect(CurrentNPCId, bot);

                        NpcState = LOOP_NPC_STATES.Merging;
                        townLoopTimer = 60 * 1000;
                    }
                    #endregion
                    break;

                case LOOP_NPC_STATES.Merging:
                    #region mergin
                    {
                        if (bot.Inventory.MergeItems())
                        {
                            townLoopTimer = 60 * 1000;
                        }
                        else
                        {
                            NpcState = LOOP_NPC_STATES.Done;
                            townLoopTimer = 100;
                        }
                    }
                    #endregion
                    break;

                case LOOP_NPC_STATES.Done:
                    #region done
                    {
                        //bot.Debug("townscript: NPC {1} done.", DateTime.Now.ToString("HH:mm:ss.fff"), targetNpc);

                        ++townScriptIdx;
                        townLoopTimer = 100;
                    }
                    #endregion
                    break;
            }
        }

        public void CheckTownLoop()
        {
            if (townLoopTimer > 0)
            {
                townLoopTimer -= 100;
                if (townLoopTimer > 0) return;
            }

            if (townScriptIdx < townScript.Count)
            {
                if (townScriptIdx == 0)
                {
                    if (handleRequire()) return; // do not inc towncounter..
                }

                try
                {
                    var cmdNarg = townScript.ElementAt(townScriptIdx).Split(';');
                    var cmd = cmdNarg[0].ToLower();
                    var arg = cmdNarg[1];

                    //bot.Debug(string.Join(",", cmdNarg));

                    if (cmd != "npc")
                        townScriptNpcCnt = 0;
                    else townScriptNpcCnt++;

                    switch (cmd)
                    {
                        case "go":
                            handleGoCmd(arg);
                            break;

                        case "npc":
                            handleNpcCmd(arg);
                            break;

                        default:
                            bot.Debug("{0} | townscript: unknown command: {1} with arg: {2}", DateTime.Now.ToString("HH:mm:ss.fff"), cmd, arg);
                            break;
                    }
                }
                catch { townLoopTimer = 300; ++townScriptIdx; }
            }
            else StartGoToTrainplace();
        }

        public bool IsBuying()
        {
            return LoopState == LOOP_AREAS.Town && NpcState == LOOP_NPC_STATES.Buying;
        }

        public bool IsSelling()
        {
            return LoopState == LOOP_AREAS.Town && NpcState == LOOP_NPC_STATES.Selling;
        }

        public bool _isStoring = false;
        public bool IsStoring()
        {
            return LoopState == LOOP_AREAS.Town && _isStoring;
        }

        private bool _isUsingConsignment = false;
        public bool IsUsingConsignment()
        {
            return LoopState == LOOP_AREAS.Town && _isUsingConsignment;
        }

        public bool IsMerging()
        {
            return LoopState == LOOP_AREAS.Town && NpcState == LOOP_NPC_STATES.Merging;
        }

        // callbacks

        public void ItemSold(byte slot)
        {
            if (NpcState != LOOP_NPC_STATES.Selling) return;

            //Console.WriteLine("{0} | item sold: slot {1}", DateTime.Now.ToString("HH:mm:ss.fff"), slot);

            sellingSlot++;
            townLoopTimer = 100; // trigger next !
        }

        public void ItemBought(InventoryItem invitem, bool swappingItems = false)
        {
            if (!IsBuying()) return;
            if (!swappingItems) townLoopTimer = 100;
            else townLoopTimer = 20 * 1000;
        }

        public void ItemsMerged()
        {
            if (!IsMerging()) return;

            townLoopTimer = 100;
        }

        public void StorageItemMerged()
        {
            //bot.Debug($"StorageItemMerged(): {IsStoring()}");

            if (!IsStoring()) return;

            townLoopTimer = 100;
        }

        public void StorageOpened(bool success)
        {
            //bot.Debug($"StorageOpened(): {IsStoring()} -- {npcState == LOOP_NPC_STATES.OpenStorage}");

            if (!IsStoring()) return;
            if (NpcState != LOOP_NPC_STATES.OpenStorage) return;
            if (!success)
            {
                bot.Log("storage: cant open..");

                NpcState = LOOP_NPC_STATES.Closing;
                townLoopTimer = 100;
                return;
            }

            NpcState = LOOP_NPC_STATES.GetStorage;
            townLoopTimer = 100;
        }

        public void GotStorageList(bool success)
        {
            //bot.Debug($"GotStorageList({success}): {IsStoring()} -- {npcState == LOOP_NPC_STATES.GetStorage}");

            if (!IsStoring()) return;
            if (NpcState != LOOP_NPC_STATES.GetStorage) return;
            if (!success)
            {
                bot.Log("storage: cant get list..");

                NpcState = LOOP_NPC_STATES.Closing;
                townLoopTimer = 100;
                return;
            }

            NpcState = LOOP_NPC_STATES.Storing;
            townLoopTimer = 100;
        }

        public void ItemPutToStorage()
        {
            //bot.Debug($"ItemPutToStorage(): {IsStoring()}");

            if (!IsStoring()) return;

            townLoopTimer = 100;
        }

        public void NpcOpened(bool success)
        {
            if (LoopState != LOOP_AREAS.Town) return;

            //bot.Debug($"NpcOpen({success})");

            if (!success && (IsBuying() || IsSelling()))
            {
                NpcState = LOOP_NPC_STATES.Closing;
                townLoopTimer = 100;
                return;
            }

            townLoopTimer = 100;
        }

        public void NpcSelected(bool success)
        {
            if (LoopState != LOOP_AREAS.Town) return;

            //bot.Debug($"NpcSelected({success})");

            if (!success)
            {
                NpcState = LOOP_NPC_STATES.Closing;
                townLoopTimer = 100;
                return;
            }

            townLoopTimer = 100;
        }

        public void NpcDeselected(bool success)
        {
            if (LoopState != LOOP_AREAS.Town) return;

            //bot.Debug($"NpcDeselected({success})");

            townLoopTimer = 100;
        }

        public void NpcError()
        {
            if (LoopState != LOOP_AREAS.Town) return;

            bot.Log("NPC ERROR");

            if (!(IsStoring() || IsBuying() || IsSelling())) return;

            bot.Log("NPC ERROR: CLOSE NOW");

            NpcState = LOOP_NPC_STATES.Closing;
            townLoopTimer = 100;
        }

        public void ConsignmentOpened(bool success)
        {
            //bot.Debug($"ConsignmentOpened(): {IsUsingConsignment()} -- {npcState == LOOP_NPC_STATES.ConsignmentOpen}");

            if (!IsUsingConsignment()) return;
            if (NpcState != LOOP_NPC_STATES.ConsignmentOpen) return;
            if (!success)
            {
                bot.Log("consignment: cant open..");

                NpcState = LOOP_NPC_STATES.ConsignmentClose;
                townLoopTimer = 100;
                return;
            }

            NpcState = LOOP_NPC_STATES.ConsignmentSettle;
            townLoopTimer = 100;
        }

        public void ConsignmentSettled(bool success)
        {
            //bot.Debug($"ConsignmentSettled({success}): {IsUsingConsignment()} -- {npcState == LOOP_NPC_STATES.ConsignmentSettle}");

            if (!IsUsingConsignment()) return;
            if (NpcState != LOOP_NPC_STATES.ConsignmentSettle) return;
            if (!success)
            {
                bot.Log($"consignment: could not settle!");

                NpcState = LOOP_NPC_STATES.ConsignmentClose;
                townLoopTimer = 100;
                return;
            }

            NpcState = LOOP_NPC_STATES.ConsignmentAbortExpired;
            townLoopTimer = 100;
        }

        public void ConsignmentItemAborted(bool success)
        {
            //bot.Debug($"ConsignmentItemAborted({success}): {IsUsingConsignment()} -- {npcState == LOOP_NPC_STATES.ConsignmentAbortExpired}");

            if (!IsUsingConsignment()) return;
            if (NpcState != LOOP_NPC_STATES.ConsignmentAbortExpired) return;
            if (!success)
            {
                bot.Log($"consignment: could not abort!");

                NpcState = LOOP_NPC_STATES.ConsignmentClose;
                townLoopTimer = 100;
                return;
            }

            townLoopTimer = 100;
        }

        public void ConsignmentClosed(bool success)
        {
            //bot.Debug($"ConsignmentClosed({success}): {IsUsingConsignment()} -- {npcState == LOOP_NPC_STATES.ConsignmentClose}");

            if (!IsUsingConsignment()) return;

            _isUsingConsignment = false;

            NpcState = LOOP_NPC_STATES.Closing;
            townLoopTimer = 100;
        }

        public void ConsignmentItemRegistered(bool success)
        {
            //bot.Debug($"ConsignmentItemRegistered({success}): {IsUsingConsignment()} -- {npcState == LOOP_NPC_STATES.ConsignmentPutNew}");

            if (!IsUsingConsignment()) return;
            if (NpcState != LOOP_NPC_STATES.ConsignmentPutNew) return;
            if (!success)
            {
                bot.Log("consignment: cant register..");

                NpcState = LOOP_NPC_STATES.ConsignmentClose;
                townLoopTimer = 100;
                return;
            }

            townLoopTimer = 100;
        }
    }
}
