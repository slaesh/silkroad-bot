using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {
        private long mpTimer = 0;

        public void CheckMP()
        {
            if (!bot.Config.Protection.UseMpPots) return;

            if (mpTimer > 0)
            {
                mpTimer -= 100;
            }

            if (mpTimer <= 0)
            {
                if (bot.Char.MaxMP == 0) return;

                uint mp = bot.Char.CurMP * 100 / bot.Char.MaxMP;
                if (mp <= bot.Config.Protection.UseMpPotsAt)
                {
                    Protection.UseMP(bot);
                    mpTimer = 250;
                }
            }
        }

    }
}
