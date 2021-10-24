using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROData
{
    public class MagicOption
    {
        public uint Id;
        public String Type;
        public String Name;
        public byte Degree;

        public bool ForWeapon;
        public bool ForShield;
        public bool ForArmor;
        public bool ForAccessory;
        public bool ForHead;
        public bool ForChest;
        public bool ForLegs;
        public bool ForNecklace;
        public bool ForEarring;
        public bool ForRing;

        public MagicOption(uint id, String type, String name, byte degree, bool forWeapon, bool forShield, bool forArmor, bool forAccessory, bool forHead, bool forChest, bool forLegs, bool forNecklace, bool forEarring, bool forRing)
        {
            Id = id;
            Type = type;
            Name = name;
            Degree = degree;

            ForWeapon = forWeapon;
            ForShield = forShield;
            ForArmor = forArmor;
            ForAccessory = forAccessory;
            ForHead = forHead;
            ForChest = forChest;
            ForLegs = forLegs;
            ForNecklace = forNecklace;
            ForEarring = forEarring;
            ForRing = forRing;
        }
    }

    class MagicOptions : List<MagicOption>
    {
        private static MagicOptions instance;
        public static MagicOptions Current
        {
            get
            {
                return instance ?? (instance = new MagicOptions());
            }
        }

        public static void Load()
        {
            var f = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "parse_magicoptions.txt");
            if (!File.Exists(f)) return;

            using (var sr = new StreamReader(f))
            {
                var line = sr.ReadLine(); // skip first line
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    var splitted = line.Split(',');
                    try
                    {
                        Current.Add(new MagicOption(
                                uint.Parse(splitted[0]),
                                splitted[1],
                                splitted[2],
                                byte.Parse(splitted[3]),
                                bool.Parse(splitted[4]),
                                bool.Parse(splitted[5]),
                                bool.Parse(splitted[6]),
                                bool.Parse(splitted[7]),
                                bool.Parse(splitted[8]),
                                bool.Parse(splitted[9]),
                                bool.Parse(splitted[10]),
                                bool.Parse(splitted[11]),
                                bool.Parse(splitted[12]),
                                bool.Parse(splitted[13])
                            ));
                    }
                    catch
                    {
                        //Console.WriteLine(line);
                    }
                }
            }
        }

        public static MagicOption GetById(uint id)
        {
            return Current.FirstOrDefault(mo => mo.Id == id);
        }

        public static MagicOption GetByType(string type, uint degree)
        {
            return Current.FirstOrDefault(mo => mo.Type.Equals(type, StringComparison.OrdinalIgnoreCase) && mo.Degree == degree);
        }
    }
}
