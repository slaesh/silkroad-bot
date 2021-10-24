using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {
        public void CheckImbue()
        {
            var imbue = bot.GetSkill(bot.Config.Imbue);
            if (imbue == null) return;

            if (imbue.CooldownTimer > 0)
            {
                imbue.CooldownTimer -= 100;
                if (imbue.CooldownTimer <= 0) return; // just for a small delay ..
            }
            
            if (bot.Char.Model == 0 || !(MobInfos.GetById(bot.Char.Model)?.Type ?? "").Contains("_CH_")) return;
            if (TrainState != LOOP_TRAINING_STATES.Attacking) return;
            //if (attackingState < 2) return;
            if (imbue.CooldownTimer > 0) return;
            if (bot.CurSelected == null) return;
            if (imbue.MP > bot.Char.CurMP) return;

            //Console.WriteLine("{0} | cast imbue", DateTime.Now.ToString("HH:mm:ss.fff"));

            Actions.CastImbue(imbue.Model, bot);

            imbue.CooldownTimer = (long)imbue.Duration / 4;
        }
    }
}
