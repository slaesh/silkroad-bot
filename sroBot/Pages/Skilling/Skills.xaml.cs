using sroBot.SROBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace sroBot.Pages.Skilling
{
    /// <summary>
    /// Interaction logic for Skills.xaml
    /// </summary>
    public partial class Skills : UserControl
    {
        private MASTERY_TYPES _curMastery = MASTERY_TYPES.CH_COLD;
        public MASTERY_TYPES CurMastery { get { return _curMastery; } set { _curMastery = value; refreshView(); } } 

        public Skills()
        {
            InitializeComponent();
            
            Loaded += (_, __) => refreshView();
            DataContextChanged += (_, __) => refreshView();
        }

        public static SkillGroupModel[] GenerateSkillGroups(Bot bot, IEnumerable<SkillInfo> skills, UInt32 mastery)
        {
            return skills.Where(s => s.SkillGroup != 255 && s.RequiredMastery1 == mastery).GroupBy(s => s.SkillGroup).OrderBy(g => g.Key).Select(g =>
                new SkillGroupModel()
                {
                    GroupId = g.Key,
                    Mastery = mastery,
                    Skills = g.OrderBy(s => s.SkillGroupIndex).GroupBy(s => s.SkillId).Select(sg =>
                                new SkillGroupSkillModel()
                                {
                                    Id = sg.Key,
                                    Mastery = mastery,
                                    Name = sg.LastOrDefault()?.Name ?? "XXX",
                                    MaxLevel = sg.OrderBy(s => s.SkillLevel).LastOrDefault()?.SkillLevel ?? 0,
                                    Icon = sg.Last().Icon,
                                    CurLevel = bot.GetAvailableSkills().FirstOrDefault(s => s.SkillId == sg.Key)?.SkillLevel ?? 0,
                                    LevelUpTo = (bot.Config.Skilling.Skills.ContainsKey(sg.Key) ? bot.Config.Skilling.Skills[sg.Key] : (byte)0),
                                    UseAsAtt = bot.Config.Skilling.UseAsAttack.Contains(sg.Key),
                                    UseAsBuff = bot.Config.Skilling.UseAsBuff.Contains(sg.Key)
                                }).ToArray()
                }
                ).ToArray();
        }

        private static bool skillRequirements(Bot bot, UInt32 skillId, byte level)
        {
            var skillInfo = SkillInfos.GetBySkillId(skillId);
            if (skillInfo == null) return true;

            if ((skillInfo.RequiredSkill1 == 0 || skillRequirements(bot, skillInfo.RequiredSkill1, skillInfo.RequiredSkill1Level)) &&
                (skillInfo.RequiredSkill2 == 0 || skillRequirements(bot, skillInfo.RequiredSkill2, skillInfo.RequiredSkill2Level)) &&
                (skillInfo.RequiredSkill3 == 0 || skillRequirements(bot, skillInfo.RequiredSkill3, skillInfo.RequiredSkill3Level))
                )
            {
                if (bot.Config.Skilling.Skills.ContainsKey(skillId) && bot.Config.Skilling.Skills[skillId] >= level) return true;

                bot.Config.Skilling.Skills[skillId] = level;
                return true;
            }

            return false;
        }
        
        private void guiBtn_skill_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            var skill = btn.DataContext as SkillGroupSkillModel;
            if (skill == null) return;
            
            if (skill.LevelUpTo >= skill.MaxLevel) return;

            var bot = this.DataContext as Bot;
            if (bot == null) return;

            skill.LevelUpTo += 1;
            if (skillRequirements(bot, skill.Id, skill.LevelUpTo))
            {
                bot.Config.Skilling.Skills[skill.Id] = skill.LevelUpTo;
                refreshView();
            }
        }

        private void refreshView()
        {
            var bot = this.DataContext as Bot;
            if (bot == null) return;

            guiItemscontrol_skills.ItemsSource = GenerateSkillGroups(bot, SkillInfos.SkillList, (UInt32)CurMastery);
        }

        private void skillMenu_useAsBuff_Click(object sender, RoutedEventArgs e)
        {
            var mItem = sender as MenuItem;
            if (mItem == null) return;

            var skill = mItem.DataContext as SkillGroupSkillModel;
            if (skill == null) return;

            var bot = this.DataContext as Bot;
            if (bot == null) return;

            var skillInfo = SkillInfos.GetBySkillId(skill.Id);
            if (skillInfo == null || skillInfo.NeedsTarget) return;
            
            if (bot.Config.Skilling.UseAsBuff.Contains(skill.Id)) return;

            bot.Config.Skilling.UseAsBuff.Add(skill.Id);
            refreshView();
        }

        private void skillMenu_useAsAttack_Click(object sender, RoutedEventArgs e)
        {
            var mItem = sender as MenuItem;
            if (mItem == null) return;

            var skill = mItem.DataContext as SkillGroupSkillModel;
            if (skill == null) return;

            var bot = this.DataContext as Bot;
            if (bot == null) return;

            var skillInfo = SkillInfos.GetBySkillId(skill.Id);
            if (skillInfo == null || !skillInfo.NeedsTarget) return;

            if (bot.Config.Skilling.UseAsAttack.Contains(skill.Id)) return;

            bot.Config.Skilling.UseAsAttack.Add(skill.Id);
            refreshView();
        }

        private void getAllSkillsThatRequireMe(uint skillId, List<uint> skillIds)
        {
            if (skillIds.Contains(skillId)) return;

            skillIds.Add(skillId);
            SkillInfos.SkillList.Where(s => s.RequiredSkill1 == skillId || s.RequiredSkill2 == skillId || s.RequiredSkill3 == skillId).Select(s => s.SkillId).ToList().ForEach(s => getAllSkillsThatRequireMe(s, skillIds));

            skillIds = skillIds.Distinct().ToList();
        }

        private void skillMenu_doNotSkill_Click(object sender, RoutedEventArgs e)
        {
            var mItem = sender as MenuItem;
            if (mItem == null) return;

            var skill = mItem.DataContext as SkillGroupSkillModel;
            if (skill == null) return;

            var bot = this.DataContext as Bot;
            if (bot == null) return;

            var allSkillsThatRequireMe = new List<uint>();
            getAllSkillsThatRequireMe(skill.Id, allSkillsThatRequireMe);

            foreach (var skillToRemove in allSkillsThatRequireMe)
            {
                bot.Config.Skilling.Skills.Remove(skillToRemove);
                bot.Config.Skilling.UseAsAttack.Remove(skillToRemove);
                bot.Config.Skilling.UseAsBuff.Remove(skillToRemove);
            }
            
            refreshView();
        }

        private void skillMenu_doNotuseAsBuff_Click(object sender, RoutedEventArgs e)
        {
            var mItem = sender as MenuItem;
            if (mItem == null) return;

            var skill = mItem.DataContext as SkillGroupSkillModel;
            if (skill == null) return;

            var bot = this.DataContext as Bot;
            if (bot == null) return;

            bot.Config.Skilling.UseAsBuff.Remove(skill.Id);
            refreshView();
        }

        private void skillMenu_doNotuseAsAttack_Click(object sender, RoutedEventArgs e)
        {
            var mItem = sender as MenuItem;
            if (mItem == null) return;

            var skill = mItem.DataContext as SkillGroupSkillModel;
            if (skill == null) return;

            var bot = this.DataContext as Bot;
            if (bot == null) return;
            
            bot.Config.Skilling.UseAsAttack.Remove(skill.Id);
            refreshView();
        }
    }
}
