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
        private long checkWalkingAroundTimer = 0;
        private List<Point> walkingPoints = new List<Point>();
        private int walkingPointsIdx = 0;

        public void CheckWalkingAround(bool trigger = false)
        {

            if (trigger) checkWalkingAroundTimer = 100;

            if (checkWalkingAroundTimer > 0)
            {
                checkWalkingAroundTimer -= 100;

                if (checkWalkingAroundTimer <= 0)
                {
                    if (TrainState == LOOP_TRAINING_STATES.WalkingAround || TrainState == LOOP_TRAINING_STATES.WalkingBack)
                    {
                        if (walkingPointsIdx >= walkingPoints.Count)
                        {
                            if (TrainState == LOOP_TRAINING_STATES.WalkingAround)
                                TrainState = LOOP_TRAINING_STATES.Picking;

                            if (TrainState == LOOP_TRAINING_STATES.WalkingBack)
                                TrainState = LOOP_TRAINING_STATES.Attacking;

                            return;
                        }

                        var walkingTo = walkingPoints[walkingPointsIdx++];

                        var time = Movement.CalculateTime(Movement.GetDistance(bot.Char.CurPosition, walkingTo), bot.Char.Speed);
                        checkWalkingAroundTimer = time + 300;

                        //bot.Debug("walking back to {0} ({1}/{2})", walkingTo, walkingPointsIdx, walkingPoints.Count);

                        Movement.WalkTo(bot, walkingTo);
                        Movement.WalkTo(bot, walkingTo);
                        Movement.WalkTo(bot, walkingTo);

                        //Console.WriteLine("{0} | i am @ {1}/{2} - try to pick something..", DateTime.Now.ToString("HH:mm:ss.fff"), bot.Char.CurPosition.X, bot.Char.CurPosition.Y);
                    }
                }
            }

            if (checkWalkingAroundTimer <= 0)
                checkWalkingAroundTimer = 100;
        }
    }
}
