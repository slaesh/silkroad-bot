using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {
        private long hpTimer = 0;

        public void CheckHP(bool trigger = false)
        {
            if (!bot.Config.Protection.UseHpPots) return;

            if (hpTimer > 0)
            {
                hpTimer -= 100;
            }

            if (hpTimer <= 0)
            {
                if (bot.Char.MaxHP == 0) return;

                uint hp = bot.Char.CurHP * 100 / bot.Char.MaxHP;
                if (hp < bot.Config.Protection.UseHpPotsAt)
                {
                    Protection.UseHP(bot);
                    hpTimer = 250;
                }

                if (bot.Char.Ridepet != null)
                {
                    var pethp = (ulong)(bot.Char.Ridepet.CurHP * 100) / bot.Char.Ridepet.Mobinfo.Hp;
                    if (pethp < 70)
                    {
                        Protection.UsePetHP(bot, bot.Char.Ridepet.UID);
                    }
                }
            }
        }
    }
}
