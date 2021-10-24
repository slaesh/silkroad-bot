using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace sroBot
{
    [Serializable]
    public class MobInfo
    {
        //ID,Type,Name,Level,Hp
        public uint Model { get; set; }
        public String Type { get; set; }
        public String Name { get; set; }
        public int Level { get; set; }
        public ulong Hp { get; set; }

        public byte TypeId1; // IMMER 1 ?
        public byte TypeId2; // 1 bei CHARS.. 2 bei allem anderen
        public byte TypeId3;

        public enum TypeId3_Types
        {
            CHARACTER = 0,
            MOB = 1, // "STRUCTURES" too ?!
            NPC = 2, // "STRUCTURES" too ?!
            PET = 3,
            COS_GUARD = 4, // "STRUCTURES" too ?!
            STRUCTURES = 5
        }

        public byte TypeId4;

        /* CHARACTERS:
         * --> ist immer 0
         * 
         * MOBS:
         * 1: just a mob?
         * 2: thief
         * 3: hunter
         * 4: MOB_QT_.. und STRUCTURE_..
         * 5: MOB_EVENT_PANDORA
         * 
         * NPCS:
         * 0: just a npc?
         * 1: STRUCTURE_...
         * 
         * PETS:
         * 1: RIDE
         * 2: Transport
         * 3: Attack
         * 4: Pick
         * 5: COS_GUILD_XX_SOLDIER_..
         * 6: MOB_QT_..
         * 7: NPC_CH_QT_..
         * 8: NPC_CH_QT_..
         * 
         */

        public MobInfo Copy()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
                stream.Position = 0;

                return (MobInfo)formatter.Deserialize(stream);
            }
        }
    }

    public class MobInfos
    {
        public static List<MobInfo> MobList = new List<MobInfo>();

        public static void Load()
        {
            var f = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "parse_mobs.txt");
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
                        MobList.Add(new MobInfo()
                        {
                            Model = uint.Parse(splitted[0]),
                            Type = splitted[1],
                            Name = splitted[2],
                            Level = int.Parse(splitted[3]),
                            Hp = ulong.Parse(splitted[4]),
                            TypeId1 = byte.Parse(splitted[5]),
                            TypeId2 = byte.Parse(splitted[6]),
                            TypeId3 = byte.Parse(splitted[7]),
                            TypeId4 = byte.Parse(splitted[8])
                        });
                    }
                    catch
                    {
                        //Console.WriteLine(line);
                    }
                }
            }
        }

        public static int GetIndexById(uint id)
        {
            var mob = MobList.FirstOrDefault(m => m.Model == id);
            return mob == null ? -1 : MobList.IndexOf(mob);
        }

        public static MobInfo GetById(uint id)
        {
            return MobList.FirstOrDefault(m => m.Model == id)?.Copy();
        }
    }
}
