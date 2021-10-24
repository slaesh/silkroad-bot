using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {
        private long checkAttackingTimer = 100;
        //private uint attackingState = 0;

        public bool IsAttacking()
        {
            return TrainState == LOOP_TRAINING_STATES.Attacking;
        }

        public void CheckAttacking(bool trigger = false)
        {
            if (trigger)
            {
                checkAttackingTimer = 100;
                return;
            }

            if (checkAttackingTimer > 0)
            {
                checkAttackingTimer -= 100;

                if (checkAttackingTimer <= 0)
                {
                    if (TrainState != LOOP_TRAINING_STATES.Attacking && TrainState != LOOP_TRAINING_STATES.WalkingAround)
                    {
                        checkAttackingTimer = 100;
                        return;
                    }

                    var mob = bot.Spawns.Mobs.GetClosest(bot.Config.Training.MobPreferences, m => bot.Config.TrainPlace.IsInside(m));

                    if (bot.CurSelected != null && bot.Spawns.Mobs.Get(bot.CurSelected.UID) != null)
                        mob = bot.CurSelected;

                    if (mob != null)
                    {
                        TrainState = LOOP_TRAINING_STATES.Attacking;

                        if (bot.Char.IsAlive && bot.Char.Zerk == 5 && !bot.Char.ZerkInUse)
                        {
                            if (bot.Config.Training.UseZerkImmediatly ||
                                (bot.Config.Training.UseZerkAtGiant && mob.Type == 0x04) ||
                                (bot.Config.Training.UseZerkAtPtMob && mob.Type == 0x10) ||
                                (bot.Config.Training.UseZerkAtPtChamp && mob.Type == 0x11) ||
                                (bot.Config.Training.UseZerkAtPtGiant && mob.Type == 0x14) ||
                                (bot.Config.Training.UseZerkWhenAttackedBy && bot.Spawns.Mobs.GetAll().Count(m => m.IsAttackingMe) >= bot.Config.Training.ZerkWhenAttackedByNoOfMobs)
                                )
                            {
                                Actions.UseZerk(bot);
                            }
                        }

                        if (mob != bot.CurSelected)
                        {
                            bot.CurSelected = mob;
                            bot.Config.ResetAttackingSkillIndex();

                            bot.MobHpChanged();
                            
                            Actions.Select(mob.UID, bot);
                            Actions.Select(mob.UID, bot);

                            checkSkillsTimer = 100; // force attack now !
                            
                            checkAttackingTimer = 200; // just cast the imbue..
                            return;
                        }

                        checkAttackingTimer = 300;
                    }
                    else
                    {
                        bot.SaveLastMob(bot.CurSelected);
                        bot.CurSelected = null;

                        if (TrainState == LOOP_TRAINING_STATES.WalkingAround)
                        {
                            checkAttackingTimer = 100;
                            return;
                        }

                        var runningToMiddle = true;
                        if (bot.Config.TrainPlace.IsUsingCircle() && bot.Config.TrainPlace.Radius != 0)
                        {
                            // walk around !
                            if (Movement.GetDistance(bot.Char.CurPosition.X, bot.Config.TrainPlace.Middle.X, bot.Char.CurPosition.Y, bot.Config.TrainPlace.Middle.Y) < 20)
                            {
                                runningToMiddle = false;
                            }
                            else
                            {
                                runningToMiddle = true;
                            }
                        }
                        else if (!bot.Config.TrainPlace.IsUsingCircle())
                        {
                            runningToMiddle = false;
                        }

                        var walkTo = new Point();
                        if (runningToMiddle)
                        {
                            walkTo.X = bot.Config.TrainPlace.Middle.X;
                            walkTo.Y = bot.Config.TrainPlace.Middle.Y;
                            //bot.Debug("no mobs.. walkt to center ! -> {0}", walkTo);
                        }
                        else
                        {
                            var findPointCnt = 0;
                            do
                            {
                                var randX = Global.Random.Next(15, 40);
                                randX *= ((randX & 1) == 1 ? -1 : 1);
                                var randY = Global.Random.Next(15, 40);
                                randY *= ((randY & 1) == 1 ? -1 : 1);

                                if (bot.Config.TrainPlace.IsUsingCircle())
                                {
                                    walkTo.X = bot.Config.TrainPlace.Middle.X + randX;
                                    walkTo.Y = bot.Config.TrainPlace.Middle.Y + randY;
                                }
                                else
                                {
                                    walkTo.X = bot.Char.CurPosition.X + randX;
                                    walkTo.Y = bot.Char.CurPosition.Y + randY;
                                }
                            }
                            while (!bot.Config.TrainPlace.IsInside(walkTo) && ++findPointCnt < 10);

                            if (findPointCnt >= 10)
                            {
                                walkTo = new Point();
                            }
                            //bot.Debug("no mobs.. i am in the middle.. walk random ! --> {0}", walkTo);
                        }

                        var dist = Movement.GetDistance(walkTo.X, bot.Char.CurPosition.X, walkTo.Y, bot.Char.CurPosition.Y);
                        var time = Movement.CalculateTime(dist, bot.Char.Speed);
                        
                        TrainState = LOOP_TRAINING_STATES.WalkingAround;
                        checkWalkingAroundTimer = (long)time - 500;
                        if (checkWalkingAroundTimer <= 0)
                        {
                            checkWalkingAroundTimer = 300;
                        }

                        if (walkTo.X != 0 || walkTo.Y != 0)
                        {
                            Movement.WalkTo(bot, walkTo.X, walkTo.Y);
                            Movement.WalkTo(bot, walkTo.X, walkTo.Y);
                            Movement.WalkTo(bot, walkTo.X, walkTo.Y);

                            if (checkWalkingAroundTimer > 10000)
                            {
                                bot.Debug("ERROR !!!! checkWalkingAroundTimer: {0}", checkWalkingAroundTimer);
                                checkWalkingAroundTimer = 300;
                            }
                        }
                        else
                        {
                            checkWalkingAroundTimer = 400;
                        }
                    }
                }
            }

            if (checkAttackingTimer <= 0) checkAttackingTimer = 300;
        }
    }
}
