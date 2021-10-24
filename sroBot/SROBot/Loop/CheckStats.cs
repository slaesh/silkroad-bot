using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {
        private long checkRemainStatsTimer = 100;

        public void CheckStats(bool trigger = false)
        {
            if (trigger)
            {
                checkRemainStatsTimer = 100;
                return;
            }

            if (bot.IsUsingReturnScroll) return;

            if (checkRemainStatsTimer > 0)
            {
                checkRemainStatsTimer -= 100;

                if (checkRemainStatsTimer <= 0)
                {
                    if (bot.Config.Loop.IncreaseStatPoints && bot.Char.RemainStatPoints > 0)
                    {
                        //bot.Log($"start updating STR/INT procedure.. {bot.Char.RemainStatPoints}");

                        var strPerLvl = bot.Config.Loop.StrStatPointsPerLevel;
                        var intPerLvl = bot.Config.Loop.IntStatPointsPerLevel;

                        if (strPerLvl >= 3)
                        {
                            //bot.Log("--> STR (3/3)");
                            Actions.IncreaseSTR(bot);
                        }
                        else if (intPerLvl >= 3)
                        {
                            //bot.Log("--> INT (3/3)");
                            Actions.IncreaseINT(bot);
                        }
                        else if (strPerLvl + intPerLvl <= 3)
                        {
                            if (strPerLvl == 1 && (bot.Char.RemainStatPoints % 3) == 0)
                            {
                                //bot.Log($"--> STR (1/3)");
                                Actions.IncreaseSTR(bot);
                            }
                            else if (strPerLvl == 2 && ((bot.Char.RemainStatPoints % 3) == 0 || (bot.Char.RemainStatPoints % 3) == 1))
                            {
                                //bot.Log($"--> STR (2/3)");
                                Actions.IncreaseSTR(bot);
                            }
                            else if (intPerLvl == 1 && (bot.Char.RemainStatPoints % 3) == 2)
                            {
                                //bot.Log($"--> INT (1/3)");
                                Actions.IncreaseINT(bot);
                            }
                            else if (intPerLvl == 2 && ((bot.Char.RemainStatPoints % 3) == 2 || (bot.Char.RemainStatPoints % 3) == 1))
                            {
                                //bot.Log($"--> INT (2/3)");
                                Actions.IncreaseINT(bot);
                            }
                            else
                            {
                                bot.Log($"--> IDK WHAT TO DO.. STR: {bot.Config.Loop.StrStatPointsPerLevel}, INT: {bot.Config.Loop.IntStatPointsPerLevel}");

                                checkRemainStatsTimer = 5 * 60 * 1000;
                                return;
                            }
                        }
                        else
                        {
                            // BAD !!
                            bot.Log($"--> INVALID CONFIGURATION !!");

                            checkRemainStatsTimer = 5 * 60 * 1000;
                            return;
                        }

                        checkRemainStatsTimer = 20 * 1000;
                    }
                }
            }

            if (checkRemainStatsTimer <= 0) checkRemainStatsTimer = 3 * 1000;
        }

    }
}
