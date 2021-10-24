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
    public class SkillInfo : MVVM.ViewModelBase
    {
        //ID,Type,Name,CastTime,Cooldown,MP
        public uint Model { get; set; }
        private string _type;
        public String Type
        {
            get { return _type; }
            set
            {
                _type = value;

                byte skillLevel = 0;
                if (byte.TryParse(_type?.Split('_')?.LastOrDefault() ?? "0", out skillLevel))
                {
                    SkillLevel = skillLevel;
                }
                else
                {
                    SkillLevel = 0;
                }
            }
        }
        public String Name { get; set; }
        public byte RequiredMastery1Level { get; set; }
        public ulong CastTime { get; set; }
        public ulong Cooldown;
        public ulong Duration;
        public int MP;
        public uint RequiredMastery1 { get; set; }
        public ushort SkillGroup { get; set; }
        public String Icon { get; set; }
        public uint SPNeeded { get; set; }
        public long __CooldownIdDoNotUse { get; set; } // !! USE FUNCTIONS BELOW !!
        public string GetCooldownId()
        {
            if (__CooldownIdDoNotUse != 0)
            {
                return $"{RequiredMastery1}_{SkillGroup}_{__CooldownIdDoNotUse}";
            }

            return $"{RequiredMastery1}_{SkillGroup}_{Cooldown}_{CastTime}";
        }
        public bool HasSameCooldownId(string cdId)
        {
            return GetCooldownId() == cdId;
        }
        public bool HasSameCooldownId(SkillInfo si)
        {
            return si != null && HasSameCooldownId(si.GetCooldownId());
        }
        public bool NeedsTarget { get; set; }

        public Dictionary<String, int> Attributes { get; set; }
        public ItemInfo.WEAPON_TYPE[] RequiredItems { get; set; }
        public ItemInfo.WEAPON_TYPE WeaponToUse { get; set; }
        public byte WeaponType1 { get; set; }
        public byte WeaponType2 { get; set; }

        public uint RequiredMastery2 { get; set; }
        public byte RequiredMastery2Level { get; set; }
        public uint RequiredStr { get; set; }
        public uint RequiredInt { get; set; }
        public uint RequiredSkill1 { get; set; }
        public uint RequiredSkill2 { get; set; }
        public uint RequiredSkill3 { get; set; }
        public byte RequiredSkill1Level { get; set; }
        public byte RequiredSkill2Level { get; set; }
        public byte RequiredSkill3Level { get; set; }

        public ushort SkillGroupIndex { get; set; }

        public uint SkillId { get; set; }

        public byte SkillLevel { get; private set; }

        public long CooldownTimer
        {
            get { return GetValue(() => CooldownTimer); }
            set { SetValue(() => CooldownTimer, value); }
        }

        public uint IngameId = 0;
        
        public bool IsImbue => SkillGroup == 0 && RequiredMastery1 >= (uint)MASTERY_TYPES.CH_COLD && RequiredMastery1 <= (uint)MASTERY_TYPES.CH_FIRE;

    }

    public static class SkillInfos
    {
        public static List<SkillInfo> SkillList = new List<SkillInfo>();

        public static void Load()
        {
            var f = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "parse_skills.txt");
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
                        var weaponToUse = ItemInfo.WEAPON_TYPE.UNKNOWN;
                        Enum.TryParse(splitted[14], out weaponToUse);

                        var reqItems = new List<ItemInfo.WEAPON_TYPE>();
                        var reqItemStrings = splitted[15].Split('#');
                        if (reqItemStrings.Any())
                        {
                            foreach (var ri in reqItemStrings)
                            {
                                ItemInfo.WEAPON_TYPE riType;
                                if (Enum.TryParse(ri, out riType))
                                {
                                    reqItems.Add(riType);
                                }
                            }
                        }

                        var skill = new SkillInfo()
                        {
                            Model = uint.Parse(splitted[0]),
                            Type = splitted[1],
                            Name = splitted[2],
                            RequiredMastery1Level = byte.Parse(splitted[3]),
                            CastTime = ulong.Parse(splitted[4]),
                            Cooldown = ulong.Parse(splitted[5]),
                            Duration = ulong.Parse(splitted[6]),
                            MP = int.Parse(splitted[7]),
                            RequiredMastery1 = uint.Parse(splitted[8]),
                            SkillGroup = ushort.Parse(splitted[9]),
                            Icon = splitted[10],
                            SPNeeded = uint.Parse(splitted[11]),
                            __CooldownIdDoNotUse = long.Parse(splitted[12]),
                            NeedsTarget = bool.Parse(splitted[13]),
                            WeaponToUse = weaponToUse,
                            RequiredItems = reqItems.ToArray(),
                            WeaponType1 = byte.Parse(splitted[16]),
                            WeaponType2 = byte.Parse(splitted[17]),

                            RequiredMastery2 = uint.Parse(splitted[18]),
                            RequiredMastery2Level = byte.Parse(splitted[19]),
                            RequiredStr = uint.Parse(splitted[20]),
                            RequiredInt = uint.Parse(splitted[21]),
                            RequiredSkill1 = uint.Parse(splitted[22]),
                            RequiredSkill2 = uint.Parse(splitted[23]),
                            RequiredSkill3 = uint.Parse(splitted[24]),
                            RequiredSkill1Level = byte.Parse(splitted[25]),
                            RequiredSkill2Level = byte.Parse(splitted[26]),
                            RequiredSkill3Level = byte.Parse(splitted[27]),
                            SkillId = uint.Parse(splitted[28]),
                            SkillGroupIndex = ushort.Parse(splitted[29]),
                        };

                        if (skill.Name == "")
                        {
                            skill.Name = skill.Type.Remove(skill.Type.Length - 2);
                        }

                        SkillList.Add(skill);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex.Message}: {ex.StackTrace} ==> {line}");
                    }
                }
            }
        }
        
        public static SkillInfo GetByModel(uint model)
        {
            return SkillList.FirstOrDefault(s => s.Model == model)?.Copy();
        }

        public static SkillInfo GetBySkillId(UInt32 id, byte level = 0)
        {
            return SkillList.FirstOrDefault(s => s.SkillId == id && (level == 0 || s.SkillLevel == level))?.Copy();
        }

        public static SkillInfo GetByName(string name, byte level = 0)
        {
            return SkillList.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && (level == 0 || level == s.SkillLevel))?.Copy();
        }
    }
    
}
