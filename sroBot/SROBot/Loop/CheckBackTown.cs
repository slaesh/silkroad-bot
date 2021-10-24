using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace sroBot.SROBot
{
    public partial class Loop
    {
        private long checkBackTownTimer = 100;

        public void BackToTown()
        {
            bot.Log("go back to town!");
            bot.IsUsingReturnScroll = true;
            TrainState = LOOP_TRAINING_STATES.Returning;
            LoopState = LOOP_AREAS.Trainplace;
        }

        public void CheckBackTown(bool trigger = false)
        {
            if (trigger)
            {
                checkBackTownTimer = 100;
                return;
            }

            if (checkBackTownTimer > 0)
            {
                checkBackTownTimer -= 100;

                if (checkBackTownTimer <= 0)
                {
                    if (bot.Config.HalloweenEventSpecial)
                    {
                        return;
                    }

                    var weapon = bot.GetWeapon();
                    if (weapon != null)
                    {
                        if (weapon.Durability <= 1)
                        {
                            bot.Log("dura to low !! go back to town or stop bot !!");
                            bot.UseReturnScroll();
                            return;
                        }
                    }
                    else
                    {
                        bot.Log("no weapon? thats bad .. !!");
                        bot.UseReturnScroll();
                        return;
                    }

                    if (bot.Inventory.GetItems(i => i.Slot < 6).Count(i => Inventory.IsItemNotEmpty(i) && i.Durability <= 3) >= 2)
                    {
                        bot.Log("dura to low !! >= 2 Armor parts ..");
                        bot.UseReturnScroll();
                        return;
                    }

                    if (bot.Inventory.GetAmountOf("MP Recovery Potion") < 10)
                    {
                        bot.Log("not enough MP pots!!");
                        bot.UseReturnScroll();
                        return;
                    }
                    if (bot.Inventory.GetAmountOf("HP Recovery Potion") < 10)
                    {
                        bot.Log("not enough HP pots!!");
                        bot.UseReturnScroll();
                        return;
                    }

                    var usingArrows = weapon != null && weapon.Iteminfo.Type.Contains("_BOW_");
                    var usingBolts = weapon != null && weapon.Iteminfo.Type.Contains("_CROSSBOW_");
                    if (usingArrows && bot.Inventory.GetAmountOf("Arrow") < 10)
                    {
                        bot.Log("not enough arrows!!");
                        bot.UseReturnScroll();
                        return;
                    }

                    CheckInventory();
                }
            }

            if (checkBackTownTimer <= 0) checkBackTownTimer = 3000;
        }

    }
}
