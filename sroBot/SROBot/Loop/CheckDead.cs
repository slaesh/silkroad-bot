using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {
		private long checkDeadTimer = 15000;
		
        public void CheckDead()
        {
			if (checkDeadTimer > 0)
			{
				checkDeadTimer -= 100;
				if (checkDeadTimer > 0) return;
			
				if (bot.Char.IsParsed && bot.Char.CurHP <= 0) bot.Char.IsAlive = false;
                if (bot.Char.IsAlive) return;

                if (bot.Char.Level > 10)
                {
                    var resScroll = bot.Inventory.GetItemByType("ITEM_EVENT_RESURRECTION");
                    if (Inventory.IsItemNotEmpty(resScroll) && bot.Config.Training.UseResurrectionScroll)
                    {
                        Actions.UseResurrectionScroll(bot, resScroll.Slot);
                    }
                    else if (bot.Config.Training.BackTownWhenDead)
                    {
                        bot.Log("ACCEPT DEAD?");

                        Actions.AcceptDead(bot);
                        BackToTown();
                    }
                }
                else
                {
                    if (bot.Config.HalloweenEventSpecial)
                    {
                        if (TrainState != LOOP_TRAINING_STATES.WalkingToTrainplace)
                        {
                            return;
                        }
                    }

                    Actions.AcceptDead(bot, 2);
                }
			}
			
			checkDeadTimer = 10000;
        }
    }
}
