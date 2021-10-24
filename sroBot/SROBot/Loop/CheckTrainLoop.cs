using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {
        public void CheckTrainLoop()
        {
            switch (TrainState)
            {
                case LOOP_TRAINING_STATES.WalkingToTrainplace:
                    CheckGoToTrainplace();
                    break;

                default:
                    CheckBackTown();
                    CheckPickup();
                    CheckAttacking();
                    CheckBuffing();
                    CheckImbue();
                    CheckSkills();
                    CheckWalkingAround();
                    break;
            }
        }
    }
}
