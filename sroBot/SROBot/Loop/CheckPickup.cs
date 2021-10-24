using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {
        private long checkPickupTimer = 100;
        private sroBot.Item pickUpItem = null;
        private sroBot.Item petPickUpItem = null;
        private bool isMovingFromPetToInventory = false;
        private int moveFromPetToInventoryHandle = 0;

        private static void WaitAndReleasePetToInventoryMoving(object botNhandle)
        {
            try
            {
                var bot = (Bot)((dynamic)botNhandle).bot;
                var handle = (int)((dynamic)botNhandle).handle;

                System.Threading.Thread.Sleep(20000);

                if (bot.Loop.moveFromPetToInventoryHandle == handle && bot.Loop.isMovingFromPetToInventory) // still the same handle..
                {
                    bot.Debug("WaitAndReleasePetToInventoryMoving(): same handle - release pickpetmoving !!");
                    bot.Loop.ItemMovedFromPetToInvetory(false, false); // .. so release waiting !
                }
            }
            catch { }
        }

        public void ItemMovedFromPetToInvetory(bool triggerTownLoop, bool swappingItems)
        {
            checkPickupTimer = 100;
            isMovingFromPetToInventory = false;

            if (triggerTownLoop)
            {
                if (swappingItems) townLoopTimer = 60 * 1000;
                else townLoopTimer = 100;
            }
        }

        private void CheckPickPet()
        {
            if (bot.Char.Pickpet == null) return;

            var petitem = bot.Spawns.Items.GetClosest(null, i =>
                           i.Iteminfo.SOX == SOX_TYPE.SoSUN ||
                           i.Iteminfo.Name.Equals("Gold") ||
                           i.Iteminfo.Type.Contains("MAGICSTONE_LUCK_13") ||
                           i.Iteminfo.Type.StartsWith("ITEM_CH") ||
                           i.Iteminfo.Type.StartsWith("ITEM_EU") ||
                           i.Iteminfo.Type.Contains("_HALLOWEEN_") ||
                           i.Iteminfo.Type.StartsWith("ITEM_ETC_E110125")
                           || i.Iteminfo.Type.StartsWith("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE") // elixirs
                           //|| i.Iteminfo.Type.StartsWith("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_WEAPON")
                           //i.Iteminfo.Type.EndsWith("MAGICSTONE_MP_08") ||
                           //i.Iteminfo.Type.EndsWith("MAGICSTONE_HP_08") ||
                           //i.Iteminfo.Type.EndsWith("MAGICSTONE_ER_08") ||
                           //i.Iteminfo.Type.EndsWith("MAGICSTONE_DUR_08") ||
                           //i.Iteminfo.Type.EndsWith("MAGICSTONE_INT_08") ||
                           //i.Iteminfo.Type.EndsWith("MAGICSTONE_STR_08") ||
                           //i.Iteminfo.Type.EndsWith("MAGICSTONE_SOLID_08") ||
                           //i.Iteminfo.Type.EndsWith("ATTRSTONE_PA_08") ||
                           //i.Iteminfo.Type.EndsWith("ATTRSTONE_MA_08") ||
                           //(i.Iteminfo.Type.StartsWith("ITEM_ETC_AMMO_ARROW") && usingArrows && bot.Inventory.GetAmountOf("Arrow") < 2500) ||
                           //(i.Iteminfo.Type.StartsWith("ITEM_ETC_AMMO_BOLT") && usingBolts && bot.Inventory.GetAmountOf("Bolt") < 1000) ||

                           //(i.Iteminfo.Name.StartsWith("HP Recovery") && bot.Inventory.GetAmountOf("HP Recovery") < 900) ||
                           //(i.Iteminfo.Name.StartsWith("MP Recovery") && bot.Inventory.GetAmountOf("MP Recovery") < 900) ||
                           //(i.Iteminfo.Name.StartsWith("Universal Pill") && bot.Inventory.GetAmountOf("Universal Pill") < 500)
                           );

            if (petPickUpItem != null && bot.Spawns.Items.Get(petPickUpItem.UID) != null)
            {
                petitem = petPickUpItem;
            }
            petPickUpItem = petitem;

            var freeSlots = bot.Char.Pickpet.Inventory.FreeSlots(0);
            if (!isMovingFromPetToInventory && freeSlots <= 3)
            {
                if (bot.Inventory.IsFull())
                {
                    if (IsStarted && LoopState == LOOP_AREAS.Trainplace && !bot.IsUsingReturnScroll)
                    {
                        bot.Log("inventory full and pickpet nearly full.. back town!");
                        bot.UseReturnScroll();
                    }
                }
                else
                {
                    for (byte cnt = 0; cnt < bot.Char.Pickpet.Inventory.Size; ++cnt)
                    {
                        var curitem = bot.Char.Pickpet.Inventory.GetItem(cnt);
                        if (SROBot.Inventory.IsItemEmpty(curitem)) continue;
                        if (curitem.Iteminfo.Type.StartsWith("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE") && curitem.Count < curitem.Iteminfo.StackSize) continue;
                        if (curitem.Iteminfo.Type.Contains("MAGICSTONE_LUCK_13") && curitem.Count < curitem.Iteminfo.StackSize) continue;

                        isMovingFromPetToInventory = true;
                        ++moveFromPetToInventoryHandle;

                        System.Threading.Thread t_standup = new System.Threading.Thread(WaitAndReleasePetToInventoryMoving);
                        t_standup.Start(new { bot, handle = moveFromPetToInventoryHandle });

                        Actions.PetToInventory(curitem.Slot, bot);
                        //Console.WriteLine("{0} | move item from pet to inventory --> {1} @ {2}", DateTime.Now.ToString("HH:mm:ss.fff"), curitem.Iteminfo.Name, curitem.Slot);
                        checkPickupTimer = 1000;

                        //return;
                        break;
                    }
                }
            }

            if (petPickUpItem != null && freeSlots > 0)
            {
                Actions.PickUp(petPickUpItem.UID, bot, true);
                //bot.Debug("pick up with PET! => {0}", petitem.Iteminfo.Type);
                //checkPickupTimer = 300;
                //return;
            }
        }

        public void CheckPickup(bool trigger = false)
        {
            if (trigger)
            {
                if (TrainState != LOOP_TRAINING_STATES.WalkingToTrainplace)
                {
                    TrainState = LOOP_TRAINING_STATES.Picking;
                }
                checkPickupTimer = 100;
                return;
            }

            if (checkPickupTimer > 0)
            {
                checkPickupTimer -= 100;

                if (checkPickupTimer <= 0)
                {
                    if (TrainState != LOOP_TRAINING_STATES.Picking)
                    {
                        checkPickupTimer = 300;
                        return;
                    }
                    
                    var weapon = bot.GetWeapon();
                    var usingArrows = weapon != null && weapon.Iteminfo.Type.Contains("_BOW_");
                    var usingBolts = weapon != null && weapon.Iteminfo.Type.Contains("_CROSSBOW_");

                    var item = bot.Spawns.Items.GetClosest(null, i =>
                        bot.Config.TrainPlace.IsInside(i.X, i.Y, 10) &&
                        (
                        (i.Iteminfo.SOX == SOX_TYPE.SoSUN ||
                         i.Iteminfo.Name.StartsWith("Return Scroll") && (bot.Inventory.GetAmountOf("Return Scroll") < 10) ||
                         i.Iteminfo.Type.Contains("MAGICSTONE_LUCK_13")
                        ) ||
                        (
                          (bot.Char.Pickpet == null) &&
                          (i.Iteminfo.SOX == SOX_TYPE.SoSUN ||
                           i.Iteminfo.Name.Equals("Gold") ||
                           i.Iteminfo.IsDrop)
                           ) ||
                          (i.Iteminfo.Type.Contains("_HALLOWEEN_") ||
                          i.Iteminfo.Type.StartsWith("ITEM_ETC_E110125"))

                          //|| i.Iteminfo.Type.StartsWith("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE_WEAPON")
                          || i.Iteminfo.Type.StartsWith("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE")
                        )
                        && i.UID != (petPickUpItem?.UID ?? 0)
                        /*
                        (
                        i.Iteminfo.Name.Equals("Gold") ||
                        ((i.Iteminfo.Name.StartsWith("Return Scroll") && (bot.Inventory.GetAmountOf("Return Scroll") < 10) ||
                        i.Iteminfo.Type.Contains("RARE") ||
                        i.Iteminfo.Type.Contains("MAGICSTONE_LUCK_13") ||
                        i.Iteminfo.Type.StartsWith("ITEM_CH") ||
                        i.Iteminfo.Type.StartsWith("ITEM_EU") ||
                        i.Iteminfo.Type.StartsWith("ITEM_ETC_ARCHEMY_REINFORCE_RECIPE") ||
                        i.Iteminfo.Type.EndsWith("MAGICSTONE_MP_08") ||
                        i.Iteminfo.Type.EndsWith("MAGICSTONE_HP_08") ||
                        i.Iteminfo.Type.EndsWith("MAGICSTONE_ER_08") ||
                        i.Iteminfo.Type.EndsWith("MAGICSTONE_DUR_08") ||
                        i.Iteminfo.Type.EndsWith("MAGICSTONE_INT_08") ||
                        i.Iteminfo.Type.EndsWith("MAGICSTONE_STR_08") ||
                        i.Iteminfo.Type.EndsWith("MAGICSTONE_SOLID_08") ||
                        i.Iteminfo.Type.EndsWith("ATTRSTONE_PA_08") ||
                        i.Iteminfo.Type.EndsWith("ATTRSTONE_MA_08") ||
                        (i.Iteminfo.Type.StartsWith("ITEM_ETC_AMMO_ARROW") && usingArrows && bot.Inventory.GetAmountOf("Arrow") < 2500) ||
                        (i.Iteminfo.Type.StartsWith("ITEM_ETC_AMMO_BOLT") && usingBolts && bot.Inventory.GetAmountOf("Bolt") < 1000) ||

                        (i.Iteminfo.Name.StartsWith("HP Recovery") && bot.Inventory.GetAmountOf("HP Recovery") < 900) ||
                        (i.Iteminfo.Name.StartsWith("MP Recovery") && bot.Inventory.GetAmountOf("MP Recovery") < 900) ||
                        (i.Iteminfo.Name.StartsWith("Universal Pill") && bot.Inventory.GetAmountOf("Universal Pill") < 500)
                        )
                        )
                        */);

                    if (pickUpItem != null && bot.Spawns.Items.Get(pickUpItem.UID) != null)
                    {
                        item = pickUpItem;
                    }
                    pickUpItem = item;

                    if (item != null)
                    {
                        Actions.PickUp(item.UID, bot);
                        //bot.Debug("pick up with char! => {0}", item.Iteminfo.Type);
                        checkPickupTimer = 500;
                    }
                    else
                    {
                        checkPickupTimer = 500;
                        TrainState = LOOP_TRAINING_STATES.Attacking;
                        checkAttackingTimer = 100;
                        checkSkillsTimer = 100;
                        //bot.Debug("start attacking after picking up something..");
                    }
                }
            }

            if (checkPickupTimer <= 0) checkPickupTimer = 1000;
        }

    }
}
