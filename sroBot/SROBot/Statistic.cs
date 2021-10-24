using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace sroBot.SROBot
{
    public class Statistic : MVVM.ViewModelBase
    {
        public enum STOP_REASON
        {
            IS_RUNNING = 0,
            USER,
            DEAD,
            INVENTORY_FULL,
            DURA_LOW,
            MP_POTS_LOW,
            HP_POTS_LOW,
            ARROWS_LOW,
            NO_RETURNSCROLL,
            DISCONNECTED,
            INVALID_WALKSCRIPT,
            INVALID_TELEPORT_DESTINATION,
            NO_TELEPORT_PORTAL_FOUND,
            STOP_ON_TRAINPLACE,
            COULD_NOT_FIND_NPC,
            UNKNOWN_STARTING_POINT,
            RESTART
        }

        public DateTime Start
        {
            get { return GetValue(() => Start); }
            set { SetValue(() => Start, value); }
        }

        public DateTime? End
        {
            get { return GetValue(() => End); }
            set { SetValue(() => End, value); }
        }

        private ulong startGold = 0;
        public long Gold
        {
            get { return GetValue(() => Gold); }
            set { SetValue(() => Gold, value); }
        }

        public uint Drops
        {
            get { return GetValue(() => Drops); }
            set { SetValue(() => Drops, value); }
        }

        public TimeSpan? Duration
        {
            get { return GetValue(() => Duration); }
            set { SetValue(() => Duration, value); }
        }

        public STOP_REASON StopReason
        {
            get { return GetValue(() => StopReason); }
            set { SetValue(() => StopReason, value); }
        }

        public bool Running = true;
        private Bot bot;
        private Timer _timer = new Timer();

        public Statistic(Bot bot)
        {
            this.bot = bot;
            startGold = bot.Char.Gold;

            Start = DateTime.Now;
            Gold = 0;
            Drops = 0;

            _timer.Interval = 250;
            _timer.AutoReset = false;
            _timer.Elapsed += (s, e) =>
            {
                var t = s as Timer;
                t.Stop();
                Duration = DateTime.Now - Start;
                if (Running) t.Start();
            };
            _timer.Start();
        }

        public void Stop(STOP_REASON reason)
        {
            End = DateTime.Now;
            Running = false;
            StopReason = reason;
        }

        public void UpdateGold(ulong amount)
        {
            if (!Running) return;
            if (startGold == 0) startGold = amount;

            Gold = (long)amount - (long)startGold;
        }

        public void GotDrop()
        {
            if (!Running) return;
            ++Drops;
        }
    }
}
