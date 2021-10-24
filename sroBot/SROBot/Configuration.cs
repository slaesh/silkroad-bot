using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using sroBot.ConfigHandler;
using System.Drawing;
using Newtonsoft.Json;

namespace sroBot.SROBot
{
    public class Configuration
    {
        #region private members

        private IConfiguration<uint[]> attackingSkillConfig;
        private IConfiguration<uint[]> buffingSkillConfig;

        private String configPath = "";

        #endregion

        private ObservableCollection<SkillInfo> attackingSkills = new ObservableCollection<SkillInfo>();
        private object attackingSkillsLock = new object();

        private ObservableCollection<SkillInfo> buffingSkills = new ObservableCollection<SkillInfo>();
        private object buffingSkillsLock = new object();

        private Bot bot;

        #region common stuff


        [Serializable]
        public class ItemOptions
        {
            public UInt32 Model { get; set; }
            public ITEM_GROUP_TYPES TIdGroup { get; set; } = ITEM_GROUP_TYPES.None;
            public SOX_TYPE Sox { get; set; } = SOX_TYPE.SoSUN;
            public byte Degree { get; set; } = 13;
            public UInt16 MinAmount { get; set; } = 1;
            public string Comment { get; set; } = "";

            public override bool Equals(object obj)
            {
                var sellconfig = obj as ItemOptions;
                if (sellconfig == null) return false;

                return (Model != 0 && Model == sellconfig.Model) || (Model == 0 && sellconfig.Model == 0 && TIdGroup == sellconfig.TIdGroup && Sox == sellconfig.Sox && Degree == sellconfig.Degree);
            }

            public override int GetHashCode()
            {
                return (Model != 0) ? Model.GetHashCode() : (TIdGroup.ToString() + Sox.ToString() + Degree.ToString()).GetHashCode();
            }

            public bool Match(InventoryItem item, bool checkMinAmount)
            {
                return (
                        (Model != 0 && item.Iteminfo.Model == Model) || // check exact model..
                        (TIdGroup == (ITEM_GROUP_TYPES)item.Iteminfo.TypeIdGroup && (Degree == 0 || Degree == item.Iteminfo.Degree) && Sox == item.Iteminfo.SOX) // check tidgroup, degree and sox ..
                    ) &&
                    (!checkMinAmount || item.Count >= MinAmount);
            }
        }

        #endregion

        public string GUID { get; set; } = "";

        public TrainingPlace TrainPlace { get; set; } = new TrainingPlace();

        public String Imbue { get; set; } = "";

        public String AccountName { get; set; } = "";

        public String AccountPass { get; set; } = "";

        public uint MasteryGap { get; set; } = 0;

        #region skilling

        public class SkillingOptions
        {
            public bool EnableMasteries { get; set; } = true;
            public bool EnableSkills { get; set; } = true;
            public ObservableCollection<MasteryLevelModel> Masteries { get; set; } = new ObservableCollection<MasteryLevelModel>();
            public Dictionary<uint, byte> Skills { get; set; } = new Dictionary<uint, byte>();
            public bool AutomaticChooseSkills { get; set; } = false;
            public ObservableCollection<uint> UseAsBuff { get; set; } = new ObservableCollection<uint>();
            public ObservableCollection<uint> UseAsAttack { get; set; } = new ObservableCollection<uint>();
        }

        public SkillingOptions Skilling { get; set; } = new SkillingOptions();

        #endregion

        #region party

        public class PartyOptions
        {
            public bool AcceptInvite { get; set; } = true;
            public bool AcceptOnlyFromListedMembers { get; set; } = true;
            public ObservableCollection<string> Members { get; set; } = new ObservableCollection<string>();
            public PARTY_TYPE Type { get; set; } = PARTY_TYPE.EXPSHARE_ITEMFREE;
        }

        public PartyOptions Party { get; set; } = new PartyOptions();

        #endregion

        public bool Clientless { get; set; } = true;

        public bool AutoReconnect { get; set; } = true;

        public int AutoReconnectTimer { get; set; } = 30;

        public bool AutoStart { get; set; } = true;

        public bool HalloweenEventSpecial { get; set; } = false;

        #region alchemy

        public class AlchemyOptions
        {
            public bool UseSteady { get; set; } = true;

            public int UseSteadyAt { get; set; } = 5;

            public bool UseImmortal { get; set; } = true;

            public int UseImmortalAt { get; set; } = 5;

            public bool UseLuckyPowder { get; set; } = true;

            public int UseLuckyPowderAt { get; set; } = 8;

            public bool UseLuckyStone { get; set; } = true;

            public int UseLuckyStoneAt { get; set; } = 10;

            public bool StartOnReconnect { get; set; } = false;
        }

        public AlchemyOptions Alchemy { get; set; } = new AlchemyOptions();

        #endregion alchemy

        #region packet-logging

        public class PacketLoggingOptions
        {
            [JsonIgnore]
            public bool Enable { get; set; } = false;

            public bool UseIgnoreList { get; set; } = false;
            public bool ShowOnlyFiltered { get; set; } = false;
            public bool ShowBotToServer { get; set; } = true;
            public bool ShowServerToBot { get; set; } = true;
            public bool ShowBotToClient { get; set; } = true;
            public bool ShowClientToBot { get; set; } = true;
            public ObservableCollection<string> FilteredPackets { get; set; } = new ObservableCollection<string>();
            public ObservableCollection<string> IgnoredPackets { get; set; } = new ObservableCollection<string>();
        }

        public PacketLoggingOptions PacketLogging { get; set; } = new PacketLoggingOptions();

        #endregion

        #region training

        public class TrainingOptions
        {
            public ObservableCollection<MobTypePreference> MobPreferences { get; set; } = new ObservableCollection<MobTypePreference>();
            public bool UseMobPreferences { get; set; } = true;
            public bool UseMobHpFactor { get; set; } = true;

            public bool UseZerkImmediatly { get; set; } = true;
            public bool UseZerkAtGiant { get; set; } = true;
            public bool UseZerkAtPtMob { get; set; } = true;
            public bool UseZerkAtPtChamp { get; set; } = true;
            public bool UseZerkAtPtGiant { get; set; } = true;
            public bool UseZerkWhenAttackedBy { get; set; } = true;
            public uint ZerkWhenAttackedByNoOfMobs { get; set; } = 3;

            public bool UseResurrectionScroll { get; set; } = true;
            public bool BackTownWhenDead { get; set; } = true;

            public bool EquipPickedItems { get; set; } = false;
            public bool MixProtectorAndArmor { get; set; } = true;
            public bool UseAnyTypeOfWeapon { get; set; } = true;

            public bool UseLevelDependentTrainplace { get; set; } = false;

            public bool StopBotOnTrainplace { get; set; } = false;

        }

        public TrainingOptions Training { get; set; } = new TrainingOptions();

        #endregion

        #region loop

        public class LoopOptions
        {
            public bool BuyBetterAccessories { get; set; } = false;
            public bool BuyBetterArmorparts { get; set; } = false;
            public bool BuyBetterWeapons { get; set; } = false;

            public bool IncreaseStatPoints { get; set; } = false;
            public byte StrStatPointsPerLevel { get; set; } = 0;
            public byte IntStatPointsPerLevel { get; set; } = 0;

            public bool UseReverseReturnToLastDead { get; set; } = false;

            public bool BuyArrowsBolts { get; set; } = true;
            public ushort ArrowsBoltsAmount { get; set; } = 4000;
            public bool BuyHpPots { get; set; } = true;
            public ushort HpPotsAmount { get; set; } = 1000;
            public bool BuyMpPots { get; set; } = true;
            public ushort MpPotsAmount { get; set; } = 2000;
            public bool BuyReturnScrolls { get; set; } = true;
            public ushort ReturnScrollsAmount { get; set; } = 2;
        }

        public LoopOptions Loop { get; set; } = new LoopOptions();

        #endregion

        #region exchanging

        public class ExchangingOptions
        {
            public bool AutoAccept { get; set; } = false;
            public bool OnlyFromList { get; set; } = true;
            public ObservableCollection<string> Players { get; set; } = new ObservableCollection<string>();
        }

        public ExchangingOptions Exchanging { get; set; } = new ExchangingOptions();

        #endregion

        #region protection

        public class ProtectionOptions
        {
            public bool UseHpPots { get; set; } = true;
            public byte UseHpPotsAt { get; set; } = 70;
            public bool UseMpPots { get; set; } = true;
            public byte UseMpPotsAt { get; set; } = 60;
            public bool UseUniversalPills { get; set; } = true;
        }

        public ProtectionOptions Protection { get; set; } = new ProtectionOptions();

        #endregion

        #region storage

        [Serializable]
        public class StoringItemOptions : ItemOptions
        {
            public bool Store { get; set; } = false;
        }

        public class StorageOptions
        {
            public bool UseInLoop { get; set; } = true;
            public ObservableCollection<StoringItemOptions> StoringConfiguration { get; set; } = new ObservableCollection<StoringItemOptions>();
        }

        public StorageOptions Storage { get; set; } = new StorageOptions();

        #endregion

        #region consignment

        [Serializable]
        public class ConsignmentSellOptions : ItemOptions
        {
            public bool Sell { get; set; } = false;
            public UInt64 PricePerPiece { get; set; }
        }

        public class ConsignmentOptions
        {
            public bool UseInLoop { get; set; } = true;
            public ObservableCollection<ConsignmentSellOptions> SellConfiguration { get; set; } = new ObservableCollection<ConsignmentSellOptions>();
        }

        public ConsignmentOptions Consignment { get; set; } = new ConsignmentOptions();

        #endregion

        #region stalling

        public class StallingOptions
        {
            public string Title { get; set; } = "";
            public string Message { get; set; } = "";
            public bool ReCreateAfterLogin { get; set; } = true;
        }

        public StallingOptions Stalling { get; set; } = new StallingOptions();

        #endregion // stalling

        public static Configuration Load(SROServer.Server server, SROBot.Bot bot)
        {
            try
            {
                var configPath = Path.Combine(App.ExecutingPath, "server", server.Name, "bots", bot.CharName);
                var config = new JsonConfiguration<Configuration>(Path.Combine(configPath, "config.json")).Load();
                if (config == null)
                {
                    config = Create(configPath);
                    config.Load();
                    config.Save();
                }
                else config.configPath = configPath;

                if (config.Training.MobPreferences == null || !config.Training.MobPreferences.Any())
                {
                    config.Training.MobPreferences = MobTypePreference.CreateList();
                }

                if (config.Skilling.Masteries == null || !config.Skilling.Masteries.Any())
                {
                    config.Skilling.Masteries = MasteryLevelModel.CreateList();
                }

                config.bot = bot;

                if (string.IsNullOrEmpty(config.GUID))
                {
                    config.GUID = Guid.NewGuid().ToString();
                    config.Load();
                    config.Save();
                }
                return config;
            }
            catch { }

            return null;
        }

        private Configuration()
        {
            BindingOperations.EnableCollectionSynchronization(attackingSkills, attackingSkillsLock);
            BindingOperations.EnableCollectionSynchronization(buffingSkills, buffingSkillsLock);
        }

        private Configuration(String configPath) : this() { this.configPath = configPath; }

        public static Configuration Create(String configPath)
        {
            return new Configuration(configPath);
        }

        public bool Load()
        {
            attackingSkillConfig = new JsonConfiguration<uint[]>(Path.Combine(configPath, "attackingSkills.json"));
            buffingSkillConfig = new JsonConfiguration<uint[]>(Path.Combine(configPath, "buffingSkills.json"));

            load();

            return true;
        }

        public void ReloadSkills()
        {
            load();

            if (Skilling.AutomaticChooseSkills)
            {
                ClearAttackingSkills();
                ClearBuffingSkills();

                SROBot.Loop.checkSkillsAndBuffs(bot);
            }
        }

        private bool load()
        {
            if (Skilling.AutomaticChooseSkills)
            {
                return true;
            }

            var skillIds = attackingSkillConfig.Load();
            if (skillIds != null)
            {
                foreach (var skillId in skillIds)
                {
                    var curSkillLevel = bot.GetAvailableSkills().Where(s => s.SkillId == skillId).OrderBy(s => s.SkillLevel).LastOrDefault()?.SkillLevel ?? 0;
                    AddAttackingSkill(SkillInfos.GetBySkillId(skillId, curSkillLevel), false);
                }
            }

            skillIds = buffingSkillConfig.Load();
            if (skillIds != null)
            {
                foreach (var skillId in skillIds.Distinct())
                {
                    var curSkillLevel = bot.GetAvailableSkills().Where(s => s.SkillId == skillId).OrderBy(s => s.SkillLevel).LastOrDefault()?.SkillLevel ?? 0;
                    AddBuffingSkill(SkillInfos.GetBySkillId(skillId, curSkillLevel), false);
                }
            }

            return true;
        }

        public bool Save()
        {
            attackingSkillConfig.Save(attackingSkills.Select(a => a.SkillId).ToArray());
            buffingSkillConfig.Save(buffingSkills.Select(b => b.SkillId).ToArray());

            new JsonConfiguration<Configuration>(Path.Combine(configPath, "config.json")).Save(this);

            return true;
        }

        public ObservableCollection<SkillInfo> GetAttackingSkills()
        {
            return attackingSkills ?? new ObservableCollection<SkillInfo>();
        }

        public ObservableCollection<SkillInfo> GetBuffingSkills()
        {
            return buffingSkills ?? new ObservableCollection<SkillInfo>();
        }

        public void ClearAttackingSkills(bool save = true)
        {
            attackingSkills.Clear();

            if (save) Save();
        }

        public void ClearBuffingSkills(bool save = true)
        {
            buffingSkills.Clear();

            if (save) Save();
        }

        public void AddAttackingSkill(SkillInfo skillinfo, bool save = true)
        {
            if (skillinfo == null) return;
            skillinfo = skillinfo.Copy();

            lock (attackingSkillsLock)
            {
                attackingSkills.Add(skillinfo);
            }

            if (save) Save();
        }

        public void RemoveAttackingSkill(SkillInfo skillinfo, bool save = true)
        {
            if (skillinfo == null) return;

            lock (attackingSkillsLock)
            {
                if (!attackingSkills.Contains(skillinfo)) return;
                attackingSkills.Remove(skillinfo);
            }

            if (save) Save();
        }

        public void AddBuffingSkill(SkillInfo skillinfo, bool save = true)
        {
            if (skillinfo == null) return;
            skillinfo = skillinfo.Copy();

            lock (buffingSkillsLock)
            {
                if (buffingSkills.Contains(skillinfo)) return;
                buffingSkills.Add(skillinfo);
            }

            if (save) Save();
        }

        public void RemoveBuffingSkill(SkillInfo skillinfo, bool save = true)
        {
            if (skillinfo == null) return;

            lock (buffingSkillsLock)
            {
                if (!buffingSkills.Contains(skillinfo)) return;
                buffingSkills.Remove(skillinfo);
            }

            if (save) Save();
        }

        public void SetImbue(SkillInfo skillinfo, bool save = true)
        {
            Imbue = skillinfo.Name;

            if (save) Save();
        }

        public void UpdateSkill(SkillInfo skillinfo, bool save = true)
        {
            if (skillinfo == null) return;
            skillinfo = skillinfo.Copy();

            lock (attackingSkillsLock)
            {
                var attSkill = attackingSkills.FirstOrDefault(a => a.Name == skillinfo.Name);
                if (attSkill != null)
                {
                    var idx = attackingSkills.IndexOf(attSkill);
                    attackingSkills[idx] = skillinfo;
                    attackingSkills[idx].CooldownTimer = 3 * 1000; // to prevent miss castings !?
                }
            }

            lock (buffingSkillsLock)
            {
                var buffSkill = buffingSkills.FirstOrDefault(a => a.Name == skillinfo.Name);
                if (buffSkill != null)
                {
                    var idx = buffingSkills.IndexOf(buffSkill);
                    buffingSkills[idx] = skillinfo;
                    buffingSkills[idx].CooldownTimer = 3 * 1000; // to prevent miss castings !?
                }
            }

            if (save) Save();
        }

        private int lastAttackingSkillIdx = 0;

        public void ResetAttackingSkillIndex()
        {
            //Console.WriteLine("reset attacking skill idx");
            lastAttackingSkillIdx = 0;
        }

        public void NextSkill()
        {
            if (attackingSkills.Count == 0) return;
            //Console.WriteLine("next attacking skill idx");
            lastAttackingSkillIdx = ++lastAttackingSkillIdx % attackingSkills.Count;
        }

        public SkillInfo GetAttackingSkill(uint curMp)
        {
            lock (attackingSkillsLock)
            {
                //return Current.AttackingSkills.FirstOrDefault(s => s.Timer <= 0 && s.MP <= bot.Char.CurMP);
                if (attackingSkills.Count > 0)
                {
                    lastAttackingSkillIdx = lastAttackingSkillIdx % attackingSkills.Count;

                    var skill = attackingSkills.ElementAt(lastAttackingSkillIdx);
                    var cnt = attackingSkills.Count;
                    while ((skill == null || skill.CooldownTimer > 0 || skill.MP > curMp) && cnt-- > 0)
                    {
                        //bot.Log("invalid skill -> next attacking skill idx // {0} || {1} || {2}", skill == null, skill?.CooldownTimer, skill?.MP > curMp);
                        lastAttackingSkillIdx = ++lastAttackingSkillIdx % attackingSkills.Count;

                        skill = attackingSkills.ElementAt(lastAttackingSkillIdx);
                    }
                    return skill;
                }
                return null;
            }
        }

        public void SetCooldown(SkillInfo skill, long timeOut = 0)
        {
            if (skill == null) return;

            if (buffingSkills.Any(s => s.Model == skill.Model))
            {
                lock (buffingSkillsLock)
                {
                    foreach (var buff in buffingSkills.Where(s => s.Model == skill.Model || s.HasSameCooldownId(skill)))
                    {
                        if (timeOut == 0)
                        {
                            buff.CooldownTimer = ((long)skill.Cooldown) - (long)(skill.Cooldown * .095);
                        }
                        else
                        {
                            buff.CooldownTimer = timeOut;
                        }
                    }
                }
            }
            if (attackingSkills.Any(s => s.Model == skill.Model))
            {
                lock (attackingSkillsLock)
                {
                    foreach (var attSkill in attackingSkills.Where(s => s.Model == skill.Model || s.HasSameCooldownId(skill)))
                    {
                        if (timeOut == 0)
                        {
                            attSkill.CooldownTimer = ((long)skill.Cooldown) - (long)(skill.Cooldown * .095);
                        }
                        else
                        {
                            attSkill.CooldownTimer = timeOut;
                        }
                    }
                }
            }
        }

        public void CooldownTimer()
        {
            lock (buffingSkillsLock)
            {
                foreach (var buff in buffingSkills)
                {
                    if (buff.CooldownTimer > 0)
                    {
                        buff.CooldownTimer -= 100;
                        if (buff.CooldownTimer < 0) // just for the GUI
                            buff.CooldownTimer = 0;
                    }
                }
            }

            lock (attackingSkillsLock)
            {
                foreach (var skill in attackingSkills)
                {
                    if (skill.CooldownTimer > 0)
                    {
                        skill.CooldownTimer -= 100;
                        if (skill.CooldownTimer < 0) // just for the GUI
                            skill.CooldownTimer = 0;
                    }
                }
            }
        }
    }
}
