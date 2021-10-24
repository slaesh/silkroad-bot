using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sroBot
{
    public class MobTypePreference : MVVM.ViewModelBase
    {
        public byte Type
        {
            get { return GetValue(() => Type); }
            set { SetValue(() => Type, value); }
        }

        public String Name
        {
            get { return GetValue(() => Name); }
            set { SetValue(() => Name, value); }
        }

        /// <summary>
        /// -1 = ignore, 0 = no preference, >= 1 prefere ..
        /// </summary>
        public int Preference
        {
            get { return GetValue(() => Preference); }
            set { SetValue(() => Preference, value); }
        }

        public MobTypePreference(byte type, String name, int preference = 0)
        {
            this.Type = type;
            this.Name = name;
            this.Preference = preference;
        }

        public static ObservableCollection<MobTypePreference> CreateList()
        {
            return new ObservableCollection<MobTypePreference>(new MobTypePreference[] {
                new MobTypePreference(0, "Normal"),
                new MobTypePreference(1, "Champion"),
                new MobTypePreference(3, "Unique"),
                new MobTypePreference(4, "Giant"),
                new MobTypePreference(5, "Titan"),
                new MobTypePreference(6, "Elite"),

                new MobTypePreference(0x10, "(Party) Normal"),
                new MobTypePreference(0x11, "(Party) Champion"),
                new MobTypePreference(0x13, "(Party) Unique"),
                new MobTypePreference(0x14, "(Party) Giant"),
                new MobTypePreference(0x15, "(Party) Titan"),
                new MobTypePreference(0x16, "(Party) Elite")
            });
        }

        public static IEnumerable<Mob> Order(IEnumerable<Mob> mobs, IEnumerable<MobTypePreference> mobPreferences)
        {
            if (mobPreferences == null) return mobs;

            mobs = mobs.Where(m => mobPreferences.Any(mp => mp.Type == m.Type && mp.Preference >= 0)); // filter ignored..
            mobs = mobs.OrderByDescending(m => mobPreferences.FirstOrDefault(mp => mp.Type == m.Type)?.Preference ?? 0);

            return mobs;
        }
    }

    public class Mob : MVVM.ViewModelBase
    {
        public uint UID { get; set; }
        public MobInfo Mobinfo { get; set; }

        public int Distance { get; set; }

        public int X;
        public int Y;
        public long CurHP = 0;
        public bool IsAttackingMe = false;
        public bool KnockedDown = false;
        public float Speed = 0;
        public int Ignore = 0;
        public ulong DirectDmgDidByMe = 0;
        public ulong SplashDmgDidByMe = 0;
        public uint InvalidTarget = 0;
        public byte Type = 0;
        public UInt32 BadStatus { get; set; }

        private String partyMemberAttacking = "";
        private int partyMemberAttackingHandle = 0;

        public Mob(MobInfo mobinfo, uint uid)
        {
            Mobinfo = mobinfo;
            UID = uid;
            Distance = X = Y = 0;
            CurHP = (long)mobinfo.Hp;
        }

        private static void waitAndReleasePartyMemberAttacking(object mobNhandle)
        {
            try
            {
                var mob = (Mob)((dynamic)mobNhandle).mob;
                var handle = (int)((dynamic)mobNhandle).handle;

                Thread.Sleep(7500);

                if (mob != null && mob.partyMemberAttackingHandle == handle)
                {
                    mob.partyMemberAttacking = "";
                }
            }
            catch { }
        }

        public void SetPartyMemberAttacking (String name)
        {
            partyMemberAttacking = name;
            new Thread(waitAndReleasePartyMemberAttacking).Start(new { mob = this, handle = ++partyMemberAttackingHandle });
        }

        public bool IsPartyMemberAttacking()
        {
            return partyMemberAttacking != "";
        }

        public String GetPartyMemberAttacking()
        {
            return partyMemberAttacking;
        }

        private double getTypeFactor(IEnumerable<MobTypePreference> mobPreferences = null)
        {
            var factor = 1.0;
            switch (Type)
            {
                case 0: // normal
                    factor = 1.0;
                    break;

                case 1: // champion
                    factor = 2.0;
                    break;

                case 3: // unique
                    factor = 40.0;
                    break;

                case 4: // giant
                    factor = 3.0;
                    break;

                case 5: // titan
                    factor = 4.0;
                    break;

                case 6: // elite
                    factor = 5.0;
                    break;

                // party

                case 0x10: // normal
                    factor = 3.0;
                    break;

                case 0x11: // champion
                    factor = 4.0;
                    break;

                case 0x13: // unique
                    factor = 40.0;
                    break;

                case 0x14: // giant
                    factor = 6.0;
                    break;

                case 0x15: // titan
                    factor = 9.0;
                    break;

                case 0x16: // elite
                    factor = 10.0;
                    break;
            }

            factor = mobPreferences?.FirstOrDefault(mp => mp.Type == Type)?.Preference ?? factor;
            if (factor >= 0)
                factor += 1; // otheriwse no difference between 0 and 1 ..

            return factor;
        }

        private double getHpFactor()
        {
            if (Mobinfo.Hp == 0) return 1;
            if (CurHP < 0) CurHP = 0;
            var hp = (double)CurHP / Mobinfo.Hp;
            return 1.1 - hp;
        }

        public double GetScore(IEnumerable<MobTypePreference> mobPreferences)
        {
            var score = Distance * (1.0 / getTypeFactor(mobPreferences)) * getHpFactor();
            return score;
        }
    }
}
