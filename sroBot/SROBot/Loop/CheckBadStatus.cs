using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {
        private long checkBadStatustimer = 0;

        public void CheckBadStatus(bool trigger = false)
        {
            if (!bot.Config.Protection.UseUniversalPills) return;

            if (checkBadStatustimer > 0)
            {
                checkBadStatustimer -= 100;
            }

            if (checkBadStatustimer <= 0)
            {
                if (bot.Char.BadStatus)
                {
                    Protection.UseUniversalPill(bot);
                    checkBadStatustimer = 1000;
                }

                // TODO
                if (bot.Char.Ridepet != null && bot.Char.Ridepet.BadStatus != 0)
                {

                }
            }
        }
    }
}
