using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot
{

    public class MasteryLevelModel : MVVM.ViewModelBase
    {
        public ushort Id
        {
            get { return GetValue(() => Id); }
            set { SetValue(() => Id, value); }
        }

        public ushort Level
        {
            get { return GetValue(() => Level); }
            set { SetValue(() => Level, value); }
        }

        public MasteryLevelModel(ushort mastery, ushort level)
        {
            this.Id = mastery;
            this.Level = level;
        }

        public static ObservableCollection<MasteryLevelModel> CreateList(bool ch = true)
        {
            if (ch)
            {
                return new ObservableCollection<MasteryLevelModel>(new MasteryLevelModel[]
                {
                new MasteryLevelModel(Mastery.CH_BICHEON, 0),
                new MasteryLevelModel(Mastery.CH_HEUKSAL, 0),
                new MasteryLevelModel(Mastery.CH_PACHEON, 0),
                new MasteryLevelModel(Mastery.CH_COLD, 0),
                new MasteryLevelModel(Mastery.CH_LIGHTNING, 0),
                new MasteryLevelModel(Mastery.CH_FIRE, 0),
                new MasteryLevelModel(Mastery.CH_FORCE, 0),
                });
            }

            return new ObservableCollection<MasteryLevelModel>(new MasteryLevelModel[]
            {
                new MasteryLevelModel(Mastery.EU_BARD, 0),
                new MasteryLevelModel(Mastery.EU_CLERIC, 0),
                new MasteryLevelModel(Mastery.EU_ROUGE, 0),
                new MasteryLevelModel(Mastery.EU_WARLOCK, 0),
                new MasteryLevelModel(Mastery.EU_WARRIOR, 0),
                new MasteryLevelModel(Mastery.EU_WIZARD, 0),
            });
        }
    }

    public enum MASTERY_TYPES
    {
        CH_BICHEON = 0x101,
        CH_COLD = 0x111,
        CH_LIGHTNING = 0x112,
        CH_FIRE = 0x113,
        CH_FORCE = 0x114,
        CH_HEUKSAL = 0x102,
        CH_PACHEON = 0x103,
        EU_BARD = 0x205,
        EU_CLERIC = 0x206,
        EU_ROUGE = 0x203,
        EU_WARLOCK = 0x204,
        EU_WARRIOR = 0x201,
        EU_WIZARD = 0x202,
    }

    public class Mastery
    {
        public const ushort CH_BICHEON = (ushort)MASTERY_TYPES.CH_BICHEON;
        public const ushort CH_COLD = (ushort)MASTERY_TYPES.CH_COLD;
        public const ushort CH_FIRE = (ushort)MASTERY_TYPES.CH_FIRE;
        public const ushort CH_FORCE = (ushort)MASTERY_TYPES.CH_FORCE;
        public const ushort CH_HEUKSAL = (ushort)MASTERY_TYPES.CH_HEUKSAL;
        public const ushort CH_LIGHTNING = (ushort)MASTERY_TYPES.CH_LIGHTNING;
        public const ushort CH_PACHEON = (ushort)MASTERY_TYPES.CH_PACHEON;
        public const ushort EU_BARD = (ushort)MASTERY_TYPES.EU_BARD;
        public const ushort EU_CLERIC = (ushort)MASTERY_TYPES.EU_CLERIC;
        public const ushort EU_ROUGE = (ushort)MASTERY_TYPES.EU_ROUGE;
        public const ushort EU_WARLOCK = (ushort)MASTERY_TYPES.EU_WARLOCK;
        public const ushort EU_WARRIOR = (ushort)MASTERY_TYPES.EU_WARRIOR;
        public const ushort EU_WIZARD = (ushort)MASTERY_TYPES.EU_WIZARD;

        public static string GetName(uint Pk2Id)
        {
            switch (Pk2Id)
            {
                case 0x101:
                    return "Bicheon";

                case 0x102:
                    return "Heuksal";

                case 0x103:
                    return "Pacheon";

                case 0x111:
                    return "Cold";

                case 0x112:
                    return "Lightning";

                case 0x113:
                    return "Fire";

                case 0x114:
                    return "Force";

                case 0x201:
                    return "Warrior";

                case 0x202:
                    return "Wizard";

                case 0x203:
                    return "Rouge";

                case 0x204:
                    return "Warlock";

                case 0x205:
                    return "Bard";

                case 0x206:
                    return "Cleric";
            }
            return "";
        }

        private Dictionary<uint, byte> masteries = new Dictionary<uint, byte>();
        public static Dictionary<byte, uint> SpAtLevel = new Dictionary<byte, uint>();

        public Mastery() { }

        public void Update(uint id, byte level)
        {
            masteries[id] = level;
        }

        public byte GetLevel(uint id)
        {
            if (masteries.Keys.Contains(id)) return masteries[id];
            return 0;
        }
    }
}
