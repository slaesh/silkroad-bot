using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Data;

namespace sroBot.SROBot
{
    public partial class Loop : MVVM.ViewModelBase
    {
        private Timer loopTimer = new Timer();
        private Bot bot;

        public Statistic Statistic
        {
            get { return GetValue(() => Statistic); }
            private set { SetValue(() => Statistic, value); }
        }

        public Statistic CurStatistic
        {
            get { return GetValue(() => CurStatistic); }
            private set { SetValue(() => CurStatistic, value); }
        }

        public ObservableCollection<Statistic> Statistics { get; set; } = new ObservableCollection<Statistic>();
        private object _statisticsLock = new object();

        public bool IsStarted
        {
            get { return GetValue(() => IsStarted); }
            private set { SetValue(() => IsStarted, value); }
        }

        public DateTime LastLoop
        {
            get { return GetValue(() => LastLoop); }
            private set { SetValue(() => LastLoop, value); }
        }

        public Loop(Bot bot)
        {
            IsStarted = false;
            this.bot = bot;
            
            BindingOperations.EnableCollectionSynchronization(Statistics, _statisticsLock);

            LoopState = LOOP_AREAS.Trainplace;
            TownState = LOOP_TOWN_STATES.None;
            TrainState = LOOP_TRAINING_STATES.Attacking;
            NpcState = LOOP_NPC_STATES.Opening;

            Statistic = new Statistic(bot);

            bot.GoldAmountChanged += (_, __) =>
              {
                  CurStatistic?.UpdateGold(__);
                  Statistic?.UpdateGold(__);
              };
            bot.Inventory.ItemGained += (_, __) =>
             {
                 CurStatistic?.GotDrop();
                 Statistic?.GotDrop();
             };

            loopTimer.Interval = 100;
            loopTimer.AutoReset = false;
            loopTimer.Elapsed += (s, e) =>
            {
                LastLoop = DateTime.Now;

                var t = s as Timer;
                t.Stop();

                try
                {
                    var sw = new System.Diagnostics.Stopwatch();
                    var times = loop(sw);
                    sw.Stop();
                    if (sw.ElapsedMilliseconds > 500)
                    {
                        bot.Debug("loop more than 80ms => {0}", String.Join(", ", times));
                    }
                }
                catch (Exception ex) { bot.Log("loop exception: {0} => {1}", ex.Message, ex.StackTrace); }

                t.Start();
            };

            loopTimer.Start();
        }

        ~Loop()
        {
            if (loopTimer != null)
            {
                try
                {
                    //bot.Log("loop destroyed?!");
                    loopTimer.Stop();
                    loopTimer.Close();
                    loopTimer.Dispose();
                }
                catch { }
            }
        }

        private string curTown = "";

        public String IsInTown(Point p)
        {
            if (Movement.GetDistance(6433, bot.Char.CurPosition.X, 1099, bot.Char.CurPosition.Y) < 30)
            {
                curTown = "jangan";
                return curTown + ".txt";
            }
            else if (Movement.GetDistance(3552, bot.Char.CurPosition.X, 2070, bot.Char.CurPosition.Y) < 30)
            {
                curTown = "downhang";
                return curTown + ".txt";
            }
            else if (Movement.GetDistance(111, bot.Char.CurPosition.X, 14, bot.Char.CurPosition.Y) < 30)
            {
                curTown = "hotan";
                return curTown + ".txt";
            }
            else if (Movement.GetDistance(-5158, bot.Char.CurPosition.X, 2829, bot.Char.CurPosition.Y) < 30)
            {
                curTown = "samarkand";
                return curTown + ".txt";
            }
            else if (Movement.GetDistance(-16641, bot.Char.CurPosition.X, -332, bot.Char.CurPosition.Y) < 30)
            {
                curTown = "alex_south";
                return curTown + ".txt";
            }

            return "";
        }

        public void Start()
        {
            bot.Log("starting bot!");

            // TODO: get position and decide what to do .. !

            if (!LoadPathToTrainplace()) return;

            var idxOnRoute = IsOnMyRoute(bot.Char.CurPosition);
            if (IsInTown(bot.Char.CurPosition) != "")
            {
                bot.Debug("i am in town.. => {0}", IsInTown(bot.Char.CurPosition));

                _isStoring = false;
                _isUsingConsignment = false;

                LoopState = LOOP_AREAS.Town;
                TrainState = LOOP_TRAINING_STATES.WalkingToTrainplace;
                loadTownScript();
                townLoopTimer = 2000;
            }
            else if (bot.Config.TrainPlace.IsInside(bot.Char.CurPosition.X, bot.Char.CurPosition.Y, 20))
            {
                bot.Debug("i am near my trainplace..");

                if (bot.Config.Training.StopBotOnTrainplace)
                {
                    bot.Log("STOP ON TRAINPLACE !");
                    Stop(Statistic.STOP_REASON.STOP_ON_TRAINPLACE);
                    return;
                }

                LoopState = LOOP_AREAS.Trainplace;
                TownState = 0;
                TrainState = LOOP_TRAINING_STATES.Attacking;

                bot.SaveLastMob(bot.CurSelected);
                bot.CurSelected = null;
            }
            else if (idxOnRoute >= 0)
            {
                bot.Debug("i am omw to trainplace..");
                StartGoToTrainplace(idxOnRoute);
            }
            else
            {
                bot.Log("idk where i am.. use return scroll !");
                if (!bot.UseReturnScroll())
                {
                    Stop(Statistic.STOP_REASON.UNKNOWN_STARTING_POINT);
                }
            }

            startStatistic();

            IsStarted = true;
            loopTimer.Start();
        }

        public void Stop(Statistic.STOP_REASON reason)
        {
            IsStarted = false;
            bot.Log("stopped!");

            stopStatistic(reason);
        }

        private void startStatistic()
        {
            stopStatistic(Statistic.STOP_REASON.RESTART);

            Statistics.Where(s => s.Running).ToList().ForEach(s => s.Stop(Statistic.STOP_REASON.RESTART));

            CurStatistic = new Statistic(bot);
            Statistics.Add(CurStatistic);
        }

        private void stopStatistic(Statistic.STOP_REASON reason)
        {
            var curStatistic = CurStatistic;
            if (curStatistic == null) return;

            curStatistic.Stop(reason);

            bot.Debug(">>>>>>>>>>");
            bot.Debug("Time:  {0}", (curStatistic.End - curStatistic.Start));
            bot.Debug("Gold:  {0:N0}", curStatistic.Gold);
            bot.Debug("Drops: {0}", curStatistic.Drops);
            bot.Debug("<<<<<<<<<<");

            CurStatistic = null;
        }

        public void ModDied(uint mobId)
        {
            var mob = bot.Spawns.Mobs.Get(mobId);
            if (mob == null) return;

            if (bot.CurSelected == mob)
            {
                bot.SaveLastMob(bot.CurSelected);
                bot.CurSelected = null;

                CheckPickup(true);
            }

            bot.Spawns.Mobs.Remove(mob);
        }

        public void MobHpUpdate(uint mobId, uint mobHp)
        {
            if (mobHp == 0)
            {
                ModDied(mobId);
                return;
            }

            var mob = bot.Spawns.Mobs.Get(mobId);
            if (mob == null) return;

            mob.CurHP = mobHp;

            if (bot.CurSelected != null && bot.CurSelected.UID == mob.UID)
                bot.MobHpChanged();
        }

        public void Obstacle()
        {
            if (bot.IsUsingReturnScroll) return;
            if (TrainState != LOOP_TRAINING_STATES.Attacking) return;

            if (bot.CurSelected != null)
            {
                bot.CurSelected.Ignore = 15;
            }

            bot.SaveLastMob(bot.CurSelected);
            bot.CurSelected = null;

            bot.Debug(".. setting walking back due to found an obstacle");

            walkingPoints.Clear();
            walkingPointsIdx = 0;

            var stepsBack = 4;
            while (stepsBack-- > 0 && bot.Char.LastPositions.Count != 0)
            {
                walkingPoints.Add(bot.Char.LastPositions.Pop());
            }

            TrainState = LOOP_TRAINING_STATES.WalkingBack;
            checkWalkingAroundTimer = 200;
        }

        public void BackInTown()
        {
            if (bot.Config.Training.StopBotOnTrainplace)
            {
                Start();
                return;
            }

            if (LoopState == LOOP_AREAS.Trainplace && TrainState == LOOP_TRAINING_STATES.WalkingToTrainplace)
            {
                goToTrainplaceTimer = 3000;
            }
            else
            {
                LoopState = LOOP_AREAS.Town;
                loadTownScript();
                townLoopTimer = 5000;
            }
            //Start();
        }

        private List<long> loop(System.Diagnostics.Stopwatch sw)
        {
            var times = new List<long>();
            sw.Start();

            if (printstatus)
                printStatus();

            if (bot == null)
            {
                bot.Log("bot == null => stop loop !");
                //loopTimer.Stop();
                return times;
            }

            if (bot.Proxy == null) return times;
            if (!bot.Char.IsParsed) return times;

            CheckPickPet();
            times.Add(sw.ElapsedMilliseconds);

            bot.Config.CooldownTimer();
            times.Add(sw.ElapsedMilliseconds);

            CheckDead();
            times.Add(sw.ElapsedMilliseconds);

            if (!bot.Char.IsAlive) return times;

            CheckHP();
            times.Add(sw.ElapsedMilliseconds);

            CheckMP();
            times.Add(sw.ElapsedMilliseconds);

            CheckBadStatus();
            times.Add(sw.ElapsedMilliseconds);

            if (bot.IsUsingReturnScroll) return times;

            CheckStats();
            times.Add(sw.ElapsedMilliseconds);

            CheckMastery(false, SkillingType.None);
            times.Add(sw.ElapsedMilliseconds);

            if (!IsStarted)
            {
                if (IsBuying() || IsSelling() || IsStoring())
                {
                    bot.Log("currently talking to an npc.. NEED TO CLOSE IT NOW !!");

                    _isStoring = false;

                    NpcState = LOOP_NPC_STATES.Closing;
                    townLoopTimer = 100;
                }
                else if (IsUsingConsignment())
                {
                    bot.Log("currently using consignment.. NEED TO CLOSE IT NOW !!");

                    NpcState = LOOP_NPC_STATES.ConsignmentClose;
                    townLoopTimer = 100;
                }
                else
                {
                    return times;
                }
            }

            switch (LoopState)
            {
                case LOOP_AREAS.Town:
                    CheckTownLoop();
                    times.Add(sw.ElapsedMilliseconds);
                    break;

                case LOOP_AREAS.Trainplace:
                    CheckTrainLoop();
                    times.Add(sw.ElapsedMilliseconds);
                    break;
            }

            return times;
        }

        public void PrintStatus()
        {
            printstatus = true;
        }

        private bool printstatus = false;
        private void printStatus()
        {
            printstatus = false;
            bot.Log("------------------------------------>");
            bot.Log("Loop status: {0}", IsStarted);
            bot.Log("   returnscroll    : {0}", bot.IsUsingReturnScroll);
            bot.Log("   isAlive         : {0}", bot.Char.IsAlive);
            bot.Log("   loopState       : {0}", LoopState);
            bot.Log("   npcState        : {0}", NpcState);
            bot.Log("   trainState      : {0}", TrainState);

            bot.Log("   buying          : {0}", IsBuying());
            bot.Log("   selling         : {0}", IsSelling());
            bot.Log("   storing         : {0}", IsStoring());
            bot.Log("   consignment     : {0}", IsUsingConsignment());

            var mob = bot.CurSelected;
            bot.Log("   CurSelected     : {0}", mob != null);
            if (mob != null)
            {
                bot.Log("   CurSelectedAlive: {0}", bot.Spawns.Mobs.Get(mob.UID) != null);
                bot.Log("   CurSelectedIVT  : {0}", mob.InvalidTarget);
                bot.Log("   CurSelectedIgn  : {0}", mob.Ignore);
                bot.Log("   CurSelected type: {0}", mob.Mobinfo?.Type ?? "");
                bot.Log("   CurSelected hp  : {0}", mob.CurHP);
            }
            bot.Log("   attackingTimer  : {0}", checkAttackingTimer);
            bot.Log("   skillsTimer     : {0}", checkSkillsTimer);
            bot.Log("   buffingTimer    : {0}", checkBuffingTimer);
            bot.Log("   pickingTimer    : {0}", checkPickupTimer);
            bot.Log("   walktingTimer   : {0}", checkWalkingAroundTimer);
            bot.Log("<------------------------------------");
        }

        public enum LOOP_AREAS
        {
            Town = 0,
            Trainplace,
            Unknown
        }

        public enum LOOP_TOWN_STATES
        {
            None = 0,
            ChangingTown,
            Blacksmith
        }

        public enum LOOP_NPC_STATES
        {
            RunTo = 0,
            Selecting,
            Opening,
            Selling,
            Buying,
            Repairing,
            OpenStorage,
            GetStorage,
            Storing,
            MergeStorage,
            ConsignmentOpen,
            ConsignmentSettle,
            ConsignmentAbortExpired,
            ConsignmentPutNew,
            ConsignmentSearch,
            ConsignmentBuy,
            ConsignmentClose,
            Merging,
            Closing,
            Done
        }

        public enum LOOP_TRAINING_STATES
        {
            Returning = 0,
            Picking,
            Attacking,
            Buffing,
            Ressing,
            WalkingAround,
            WalkingBack,
            WalkingToTrainplace,
        }

        public LOOP_AREAS LoopState
        {
            get { return GetValue(() => LoopState); }
            private set { SetValue(() => LoopState, value); }
        }

        public LOOP_TOWN_STATES TownState
        {
            get { return GetValue(() => TownState); }
            private set { SetValue(() => TownState, value); }
        }

        public LOOP_TRAINING_STATES TrainState
        {
            get { return GetValue(() => TrainState); }
            private set { SetValue(() => TrainState, value); }
        }

        public LOOP_NPC_STATES NpcState
        {
            get { return GetValue(() => NpcState); }
            private set { SetValue(() => NpcState, value); }
        }
    }
}
