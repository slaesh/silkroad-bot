using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {
        private long checkMasteryTimer = 10000;
        private bool cannotLearnDueToMasteryLimit = false;

        public void CannotLearnDueToMasteryLimit(bool state)
        {
            cannotLearnDueToMasteryLimit = state;
        }

        public static void checkSkillsAndBuffs(Bot bot, bool save = false)
        {
            // CHECK FOR BETTER BUFFS
            // ==============================

            foreach (var buffId in bot.Config.Skilling.UseAsBuff.ToArray())
            {
                if (!bot.Config.Skilling.AutomaticChooseSkills) break;

                var curBuffLvl = bot.GetAvailableSkills().Where(s => s.SkillId == buffId).OrderBy(s => s.RequiredMastery1Level).LastOrDefault()?.SkillLevel ?? 255;
                if (curBuffLvl == 255) continue; // not yet skilled..

                var buffInfo = SkillInfos.GetBySkillId(buffId, curBuffLvl);
                if (buffInfo == null) continue;

                var curBuffs = bot.Config.GetBuffingSkills().ToArray().Where(s => s.HasSameCooldownId(buffInfo)).OrderBy(s => s.RequiredMastery1Level).ToArray();
                if (curBuffs.Length > 1)
                {
                    //bot.Debug($"something wrong? {buffId}");
                }

                if (curBuffs.Any(s => s.SkillId == buffId)) continue;

                if (!curBuffs.Any() || curBuffs.Last(s => s.SkillId != buffId).RequiredMastery1Level < buffInfo.RequiredMastery1Level)
                {
                    //bot.Log($"use better buff: {buffInfo.Type} with level: {buffInfo.SkillLevel}({buffInfo.RequiredMastery1Level})");

                    foreach (var buff in curBuffs)
                    {
                        bot.Config.RemoveBuffingSkill(buff, save);
                    }

                    bot.Config.AddBuffingSkill(buffInfo, save);
                }
            }

            // CHECK FOR BETTER SKILLS
            // ==============================

            foreach (var attId in bot.Config.Skilling.UseAsAttack.ToArray())
            {
                if (!bot.Config.Skilling.AutomaticChooseSkills) break;

                var curAttLvl = bot.GetAvailableSkills().Where(s => s.SkillId == attId).OrderBy(s => s.RequiredMastery1Level).LastOrDefault()?.SkillLevel ?? 255;
                if (curAttLvl == 255) continue; // not yet skilled..

                var attInfo = SkillInfos.GetBySkillId(attId, curAttLvl);
                if (attInfo == null) continue;
                
                var curAttSkills = bot.Config.GetAttackingSkills().ToArray().Where(s => s.RequiredMastery1 == attInfo.RequiredMastery1 && s.SkillGroup == attInfo.SkillGroup).OrderBy(s => s.RequiredMastery1Level).ToArray();
                if (curAttSkills.Any(s => s.SkillId == attId)) continue;
                
                var curAttSkillsCount = curAttSkills.Count(s => !s.HasSameCooldownId(attInfo));
                if (curAttSkillsCount >= 3 && curAttSkills.All(s => s.RequiredMastery1Level >= attInfo.RequiredMastery1Level)) continue;

                var attSkillsWithMyCooldown = curAttSkills.Where(s => s.HasSameCooldownId(attInfo));
                var attSkillsWithMyCooldownCount = attSkillsWithMyCooldown.Count();
                if (attSkillsWithMyCooldownCount >= 2 && attSkillsWithMyCooldown.All(s => s.RequiredMastery1Level >= attInfo.RequiredMastery1Level)) continue;

                var attSkillsWithMySkillGroup = curAttSkills.Where(s => s.SkillGroup == attInfo.SkillGroup);
                var attSkillsWithMySkillGroupCount = attSkillsWithMyCooldown.Count();
                if (attSkillsWithMySkillGroupCount >= 2 && attSkillsWithMySkillGroup.All(s => s.RequiredMastery1Level >= attInfo.RequiredMastery1Level)) continue;

                var lowestSkill = curAttSkills.FirstOrDefault();
                if (curAttSkillsCount < 3 || lowestSkill.RequiredMastery1Level < attInfo.RequiredMastery1Level)
                {
                    //bot.Log($"use better skill: {attInfo.Type} with level: {attInfo.SkillLevel}({attInfo.RequiredMastery1Level})");

                    if (curAttSkillsCount >= 3 && lowestSkill != null)
                    {
                        //bot.Debug($" --> instead of: {lowestSkill.Type}");
                        bot.Config.RemoveAttackingSkill(lowestSkill, save);
                    }

                    foreach (var sameAttSkillButLowerLevel in attSkillsWithMyCooldown.Where(s => s.RequiredMastery1Level < attInfo.RequiredMastery1Level).OrderByDescending(s => s.RequiredMastery1Level).Skip(1))
                    {
                        //bot.Debug($" ---> instead of: {sameAttSkillButLowerLevel.Type}");
                        bot.Config.RemoveAttackingSkill(sameAttSkillButLowerLevel, save);
                    }

                    foreach (var sameAttSkillButLowerLevel in attSkillsWithMySkillGroup.Where(s => s.RequiredMastery1Level < attInfo.RequiredMastery1Level).OrderByDescending(s => s.RequiredMastery1Level).Skip(2))
                    {
                        //bot.Debug($" ---> instead of: {sameAttSkillButLowerLevel.Type}");
                        bot.Config.RemoveAttackingSkill(sameAttSkillButLowerLevel, save);
                    }

                    bot.Config.AddAttackingSkill(attInfo, false);
                    curAttSkills = bot.Config.GetAttackingSkills().ToArray();
                    bot.Config.ClearAttackingSkills(save);
                    curAttSkills.OrderByDescending(s => s.RequiredMastery1Level).ToList().ForEach(s => bot.Config.AddAttackingSkill(s, save));
                }
            }
        }

        public enum SkillingType
        {
            None = 0,
            Mastery,
            Skill
        }

        private SkillingType _curSkilling = SkillingType.None;

        public void CheckMastery(bool trigger, SkillingType skillingType)
        {
            if (trigger && (skillingType == _curSkilling || _curSkilling == SkillingType.None))
            {
                checkMasteryTimer = 1000; // small delay ..
                return;
            }

            if (bot.IsUsingReturnScroll) return;
            if (!bot.Char.IsParsed) return;
            if (!bot.Config.Skilling.EnableMasteries) return;

            if (checkMasteryTimer > 0)
            {
                checkMasteryTimer -= 100;

                if (checkMasteryTimer <= 0)
                {
                    _curSkilling = SkillingType.None;

                    if (bot.Config.Skilling.Masteries.Any())
                    {
                        foreach (var mastery in bot.Config.Skilling.Masteries)
                        {
                            var curMasteryId = mastery.Id;
                            var curMasteryLevel = bot.Char.Masteries.GetLevel(curMasteryId);
                            var targetMasteryLevel = mastery.Level;

                            // CHECK MASTERIES
                            // ==================

                            if (!cannotLearnDueToMasteryLimit &&
                                targetMasteryLevel > curMasteryLevel &&
                                curMasteryLevel < (bot.Char.Level - bot.Config.MasteryGap) &&
                                (curMasteryLevel <= 1 || Mastery.SpAtLevel[curMasteryLevel] <= bot.Char.SP))
                            {
                                bot.Debug("updating mastery: {0}", Mastery.GetName(curMasteryId));

                                _curSkilling = SkillingType.Mastery;

                                Actions.UpMastery(curMasteryId, bot);
                                checkMasteryTimer = 60 * 1000;
                                break;
                            }

                            // CHECK SKILLS
                            // ==================

                            else if (bot.Config.Skilling.EnableSkills && bot.Config.Skilling.Skills.Any())
                            {
                                #region checking skills

                                var curMasterySkillIds = bot.Config.Skilling.Skills.Where(s =>
                                {
                                    var curSkill = SkillInfos.GetBySkillId(s.Key);
                                    return curSkill?.RequiredMastery1 == curMasteryId;
                                }).Select(s => s.Key).ToArray();

                                var skillUpdated = false;

                                foreach (var skillId in curMasterySkillIds)
                                {
                                    var wantedSkillLvl = bot.Config.Skilling.Skills[skillId];
                                    var curSkillLvl = bot.GetAvailableSkills().Where(s => s.SkillId == skillId).OrderBy(s => s.SkillLevel).LastOrDefault()?.SkillLevel ?? 0;
                                    if (curSkillLvl >= wantedSkillLvl) continue;

                                    var newSkill = SkillInfos.GetBySkillId(skillId, (byte)(curSkillLvl + 1));
                                    if (newSkill == null)
                                    {
                                        bot.Debug($"cannot get skillinfo.. skillid: {skillId}, lvl: {curSkillLvl + 1}");
                                        continue;
                                    }

                                    if (newSkill.RequiredMastery1Level > bot.Char.Masteries.GetLevel(newSkill.RequiredMastery1)) continue;

                                    var requirementsMet = true;
                                    requirementsMet = requirementsMet && (newSkill.RequiredMastery1 == 0 || bot.Char.Masteries.GetLevel(newSkill.RequiredMastery1) >= newSkill.RequiredMastery1Level);
                                    requirementsMet = requirementsMet && (newSkill.RequiredMastery2 == 0 || bot.Char.Masteries.GetLevel(newSkill.RequiredMastery2) >= newSkill.RequiredMastery2Level);
                                    requirementsMet = requirementsMet && (newSkill.RequiredStr <= bot.Char.STR);
                                    requirementsMet = requirementsMet && (newSkill.RequiredInt <= bot.Char.INT);
                                    requirementsMet = requirementsMet && (newSkill.RequiredSkill1 == 0 || bot.GetAvailableSkills().Where(s => s.SkillId == newSkill.RequiredSkill1).OrderBy(s => s.SkillLevel).LastOrDefault()?.SkillLevel >= newSkill.RequiredSkill1Level);
                                    requirementsMet = requirementsMet && (newSkill.RequiredSkill2 == 0 || bot.GetAvailableSkills().Where(s => s.SkillId == newSkill.RequiredSkill2).OrderBy(s => s.SkillLevel).LastOrDefault()?.SkillLevel >= newSkill.RequiredSkill2Level);
                                    requirementsMet = requirementsMet && (newSkill.RequiredSkill3 == 0 || bot.GetAvailableSkills().Where(s => s.SkillId == newSkill.RequiredSkill3).OrderBy(s => s.SkillLevel).LastOrDefault()?.SkillLevel >= newSkill.RequiredSkill3Level);

                                    if (!requirementsMet)
                                    {
                                        bot.Debug($"requirements NOT met !! skillid: {skillId}, lvl: {curSkillLvl + 1}");
                                        continue;
                                    }
                                    if (newSkill.SPNeeded > bot.Char.SP)
                                    {
                                        //bot.Debug($"not enough SP {newSkill.SPNeeded} > {bot.Char.SP} !! skillid: {skillId}, lvl: {curSkillLvl + 1}");
                                        continue;
                                    }

                                    bot.Debug($"try to up this skill => model: {newSkill.Model}/{newSkill.Type}, id: {newSkill.SkillId}, lvl: {newSkill.SkillLevel}({newSkill.RequiredMastery1Level})");

                                    var curSkill = bot.GetAvailableSkills().FirstOrDefault(s => s.SkillId == newSkill.SkillId);
                                    if (curSkill != null)
                                    {
                                        var coolDownTimerToPreventMissCastings = 20 * 1000;
                                        if (curSkill.CooldownTimer < coolDownTimerToPreventMissCastings)
                                        {
                                            if (curSkill.IsImbue)
                                            {
                                                curSkill.CooldownTimer = coolDownTimerToPreventMissCastings;
                                            }
                                            else
                                            {
                                                bot.Config.SetCooldown(curSkill, coolDownTimerToPreventMissCastings);
                                            }
                                        }
                                    }

                                    _curSkilling = SkillingType.Skill;
                                    skillUpdated = true;

                                    Actions.UpSkill(newSkill.Model, bot);
                                    checkMasteryTimer = 60 * 1000;

                                    break;
                                }

                                if (skillUpdated) break;

                                #endregion
                            }
                        }
                    }

                    // CHECK FOR BETTER IMBUE !
                    // ========================

                    var charModel = MobInfos.GetById(bot.Char.Model);
                    if (charModel != null && charModel.Type.Contains("_CH_"))
                    {
                        var curImbueId = SkillInfos.GetByName(bot.Config.Imbue)?.SkillId ?? uint.MaxValue;

                        // NO IMBUE SET..
                        if (curImbueId == uint.MaxValue)
                        {
                            var bestImbueFound = bot.GetAvailableSkills().Where(s => s.IsImbue).OrderBy(s => s.RequiredMastery1Level).LastOrDefault();
                            if (bestImbueFound != null)
                            {
                                bot.Debug($"there was no imbue set.. take this one: {bestImbueFound.Name}");

                                bot.Config.SetImbue(bestImbueFound, true);
                                curImbueId = bestImbueFound.SkillId;
                            }
                        }

                        var curImbueInfo = SkillInfos.GetBySkillId(curImbueId);
                        if (curImbueId != uint.MaxValue && curImbueInfo != null)
                        {
                            var highestImbue = bot.GetAvailableSkills().Where(s => s.RequiredMastery1 == curImbueInfo.RequiredMastery1 && s.SkillGroup == 0 /* IMBUES ! */).OrderBy(s => s.RequiredMastery1Level).LastOrDefault();
                            if (highestImbue != null && bot.Config.Imbue != highestImbue.Name)
                            {
                                bot.Debug("imbue {0} is better than {1} - take it!", highestImbue.Name, bot.Config.Imbue);
                                bot.Config.SetImbue(highestImbue, true);
                            }
                        }
                    }

                    // CHECK FOR BETTER BUFFS/SKILLS
                    // ==============================

                    checkSkillsAndBuffs(bot);
                }
            }

            if (checkMasteryTimer <= 0) checkMasteryTimer = 3000;
        }
    }
}
