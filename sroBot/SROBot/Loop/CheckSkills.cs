using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {
        public long checkSkillsTimer = 0;

        public void CheckSkills(bool trigger = false)
        {
            if (trigger)
            {
                checkSkillsTimer = 100;
                return;
            }
            
            if (checkSkillsTimer > 0)
            {
                checkSkillsTimer -= 100;
                if (checkSkillsTimer > 0) return;
            }

            if (TrainState != LOOP_TRAINING_STATES.Attacking || bot.CurSelected == null)
            {
                checkSkillsTimer = 100;
                return;
            }

            var skill = bot.Config.GetAttackingSkill(bot.Char.CurMP);
            if (skill != null && !bot.GetAvailableSkills().Any(s => s.Model == skill.Model))
            {
                //bot.Debug($"SKILLMODEL NOT PRESENT: {skill.Model}");
                skill = bot.GetAvailableSkills().FirstOrDefault(s => s.SkillId == skill.SkillId);
                //bot.Debug($"SKILLMODEL now present? {skill?.Model}");
                
                // SkillReload needed ?? -- idk.. just try it?!
                //bot.Config.ReloadSkills();
            }

            if (skill == null || !bot.GetAvailableSkills().Any(s => s.Model == skill.Model))
            {
                var attackingSkills = bot.Config.GetAttackingSkills();

                if (attackingSkills.Count > 0)
                {
                    bot.Log($"got no skill.. skill == null ({skill == null})");
                    foreach (var s in attackingSkills)
                    {
                        bot.Log("   skill: {1} -> timer: {2}, MP: {3}, myMP: {4}", DateTime.Now.ToString("HH:mm:ss.fff"), s.Name, s.CooldownTimer, s.MP, bot.Char.CurMP);
                    }
                    bot.Log();
                }

                Actions.Attack(bot.CurSelected?.UID, bot);
                checkSkillsTimer = 1000;

                return;
            }

            if (bot.CurSelected?.IsPartyMemberAttacking() ?? true)
            {
                //bot.Log("ignore mob cause partymember attacks it! => {0}", bot.CurSelected.GetPartyMemberAttacking());

                bot.SaveLastMob(bot.CurSelected);
                bot.CurSelected = null;
                CheckAttacking(true);
                checkSkillsTimer = 100;
                return;
            }

            if (Actions.CastSkill(skill.Model, bot.CurSelected.UID, bot))
            {
                //bot.Log("cast skill {1} !", DateTime.Now.ToString("HH:mm:ss.fff"), skill.Name);
                
                checkSkillsTimer = 500;
            }

            if (checkSkillsTimer <= 0)
                checkSkillsTimer = 500;
        }
    }
}
