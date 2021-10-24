using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace sroBot.SROBot
{
    public partial class Bot
    {
        public int CurMobHp
        {
            get { return GetValue(() => CurMobHp); }
            set { SetValue(() => CurMobHp, value); }
        }

        public int CurMobDmg
        {
            get { return GetValue(() => CurMobDmg); }
            set { SetValue(() => CurMobDmg, value); }
        }

        public void MobHpChanged()
        {
            var mob = CurSelected;

            if (mob == null)
            {
                CurMobHp = CurMobDmg = 0;
            }
            else
            {
                var hp = 0;
                if (mob.Mobinfo.Hp != 0)
                {
                    hp = (int)((ulong)(mob.CurHP * 100) / mob.Mobinfo.Hp);
                }
                CurMobHp = hp;

                var dmg = 0;
                if (mob.Mobinfo.Hp != 0)
                {
                    var myDmg = mob.DirectDmgDidByMe + mob.SplashDmgDidByMe;
                    dmg = (int)(myDmg * 100 / mob.Mobinfo.Hp);
                }
                CurMobDmg = dmg;
            }
        }
    }

    public class GetAvailableSkillsConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as Bot;
            if (bot == null) return null;

            return bot.GetAvailableSkills();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetAttackingSkillsConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as Bot;
            if (bot == null) return null;

            return bot.Config.GetAttackingSkills();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetBuffingSkillsConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as Bot;
            if (bot == null) return null;

            return bot.Config.GetBuffingSkills();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetActiveBuffsConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as Bot;
            if (bot == null) return null;

            return bot.GetActiveBuffs();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetLogsConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as Bot;
            if (bot == null) return null;

            return bot.GetLogs();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetStartbuttonTextConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isStarted = value as Boolean?;
            if (isStarted == true) return "stop bot";
            return "start bot";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetChatConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as SROBot.Bot;
            if (bot == null) return "";
            return bot.Chat.GetMessages();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetSkillIconConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var icon = value as String;
            if (icon == null) return null;
            icon = System.IO.Path.Combine(MainWindow.ExecutingPath, "skillimgs", icon);
            if (!System.IO.File.Exists(icon))
            {
                icon = System.IO.Path.Combine(MainWindow.ExecutingPath, "skillimgs", "archemy_potion_speed.bmp");
            }

            var x = new ImageSourceConverter();
            return x.ConvertFromString(icon);

            /*
            String stringPath = "Pictures/myPicture.jpg";
            Uri imageUri = new Uri(stringPath, UriKind.Relative);
            BitmapImage imageBitmap = new BitmapImage(imageUri);

            */

            return icon;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetItemIconConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var icon = value as String;
            if (icon == null) return null;

            icon = System.IO.Path.Combine(MainWindow.ExecutingPath, "itemimgs", icon);

            if (!System.IO.File.Exists(icon)) return null;

            var x = new ImageSourceConverter();
            return x.ConvertFromString(icon);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetInventoryConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as SROBot.Bot;
            if (bot == null) return null;

            return bot.Inventory.GetItems();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetInventoryTestPage1Converter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as SROBot.Bot;
            if (bot == null) return null;

            return bot.Inventory.ItemViewPage1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetInventoryTestPage2Converter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as SROBot.Bot;
            if (bot == null) return null;

            return bot.Inventory.ItemViewPage2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetInventoryTestPage3Converter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as SROBot.Bot;
            if (bot == null) return null;

            return bot.Inventory.ItemViewPage3;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetPetInventoryConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as SROBot.Bot;
            if (bot == null) return null;
            if (bot.Char.Pickpet == null) return null;

            return bot.Char.Pickpet.Inventory.GetItems();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetStorageConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as SROBot.Bot;
            if (bot == null) return null;

            return bot.Storage.GetItems();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetGuildStorageConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as SROBot.Bot;
            if (bot == null) return null;

            return bot.GuildStorage.GetItems();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetSpawnedPlayerConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as SROBot.Bot;
            if (bot == null) return null;

            return bot.Spawns.Player.GetList();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetSpawnedNPCsConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as SROBot.Bot;
            if (bot == null) return null;

            return bot.Spawns.Shops.GetList();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToByteConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as String;
            if (s == null) return (byte)0;
            byte b = 0;
            if (!Byte.TryParse(s, out b)) return (byte)0;

            return b;
        }
    }

    public class StringToUint32Converter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (s == null) return 0;

            UInt32 b = 0;
            if (!UInt32.TryParse(s, out b)) return 0;

            return b;
        }
    }

    public class StringToUint16Converter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (s == null) return 0;

            UInt16 b = 0;
            if (!UInt16.TryParse(s, out b)) return 0;

            return b;
        }
    }

    public class TreeviewItemHelper : MVVM.ViewModelBase
    {
        public String Name
        {
            get { return GetValue(() => Name); }
            set { SetValue(() => Name, value); }

        }
        public IEnumerable Items
        {
            get { return GetValue(() => Items); }
            set { SetValue(() => Items, value); }
        }
    }

    public class GetSpawnedPlayerInfosConverter : MVVM.BaseConverter, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //get folder name listing...
            string folder = parameter as string ?? "";
            var folders = folder.Split(',').Select(f => f.Trim()).ToList();
            //...and make sure there are no missing entries
            while (values.Length > folders.Count) folders.Add(String.Empty);

            //this is the collection that gets all top level items
            List<object> items = new List<object>();

            for (int i = 0; i < values.Length; i++)
            {
                //make sure were working with collections from here...
                var childs = values[i] as IEnumerable ?? new List<object> { values[i] };

                if (values[i] is String)
                {
                    childs = new List<object> { values[i] as String };
                }

                string folderName = folders[i];
                if (folderName == String.Empty || folderName.StartsWith("root["))
                {
                    //if no folder name was specified, move the item directly to the root item
                    foreach (var child in childs)
                    {
                        // alt -- prefix nur bei strings?
                        //if (folderName.StartsWith("root[") && child is String)
                        //{
                        //    var prefix = folderName.Split('[')[1].Replace("]", "");
                        //    items.Add(prefix + ": " + child);
                        //}
                        //else
                        //{
                        //    items.Add(child);
                        //}

                        // neu
                        if (folderName.StartsWith("root[") && !(child is IEnumerable))
                        {
                            var prefix = folderName.Split('[')[1].Replace("]", "");
                            items.Add(prefix + ": " + child.ToString());
                        }
                        else
                        {
                            items.Add(child);
                        }
                    }
                }
                else if (folderName != String.Empty)
                {
                    //create folder item and assign childs
                    var folderItem = new TreeviewItemHelper { Name = folderName, Items = childs };
                    items.Add(folderItem);
                }
            }

            return items;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetSpawnedNpcInfosConverter : MVVM.BaseConverter, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //get folder name listing...
            string folder = parameter as string ?? "";
            var folders = folder.Split(',').Select(f => f.Trim()).ToList();
            //...and make sure there are no missing entries
            while (values.Length > folders.Count) folders.Add(String.Empty);

            //this is the collection that gets all top level items
            List<object> items = new List<object>();

            for (int i = 0; i < values.Length; i++)
            {
                //make sure were working with collections from here...
                var childs = values[i] as IEnumerable ?? new List<object> { values[i] };

                if (values[i] is String)
                {
                    childs = new List<object> { values[i] as String };
                }

                string folderName = folders[i];
                if (folderName == String.Empty || folderName.StartsWith("root["))
                {
                    //if no folder name was specified, move the item directly to the root item
                    foreach (var child in childs)
                    {
                        // alt -- prefix nur bei strings?
                        //if (folderName.StartsWith("root[") && child is String)
                        //{
                        //    var prefix = folderName.Split('[')[1].Replace("]", "");
                        //    items.Add(prefix + ": " + child);
                        //}
                        //else
                        //{
                        //    items.Add(child);
                        //}

                        // neu
                        if (folderName.StartsWith("root[") && !(child is IEnumerable))
                        {
                            var prefix = folderName.Split('[')[1].Replace("]", "");
                            items.Add(prefix + ": " + child.ToString());
                        }
                        else
                        {
                            items.Add(child);
                        }
                    }
                }
                else if (folderName != String.Empty)
                {
                    //create folder item and assign childs
                    var folderItem = new TreeviewItemHelper { Name = folderName, Items = childs };
                    items.Add(folderItem);
                }
            }

            return items;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MasteryIdToStringConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = value;
            return Mastery.GetName(uint.Parse(value.ToString()));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class GetShowInventoryItemIconPage1Converter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var invitem = value as InventoryItem;
            if (SROBot.Inventory.IsItemEmpty(invitem)) return null;

            return invitem.Slot >= 13 && invitem.Slot < 13 + 32 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class GetShowInventoryItemIconPage2Converter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var invitem = value as InventoryItem;
            if (SROBot.Inventory.IsItemEmpty(invitem)) return null;

            return invitem.Slot >= 45 && invitem.Slot < 45 + 32 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class SkillsToGroupsConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bot = value as Bot;
            if (bot == null) return null;

            var skills = SkillInfos.SkillList;
            if (skills == null) return null;

            uint mastery;
            var sMastery = parameter as string;
            if (string.IsNullOrEmpty(sMastery) || !uint.TryParse(sMastery, out mastery)) return null;
            if (mastery == 0) return null;

            return Pages.Skilling.Skills.GenerateSkillGroups(bot, skills, mastery);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    
    public class SkillGroupSkillModel : MVVM.ViewModelBase
    {
        public UInt32 Id { get; set; }
        public uint Mastery { get; set; }
        public string Name { get; set; }
        public byte CurLevel { get; set; }
        public byte LevelUpTo { get; set; }
        public byte MaxLevel { get; set; }
        public string Icon { get; set; }
        public bool UseAsBuff { get; set; }
        public bool UseAsAtt { get; set; }
    }

    public class SkillGroupModel : MVVM.ViewModelBase
    {
        public uint GroupId { get; set; }
        public uint Mastery { get; set; }
        public SkillGroupSkillModel[] Skills { get; set; } = new SkillGroupSkillModel[0];
    }
    
    public class InverseBooleanConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class MasteryToCurLevelConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var masteryModel = value as MasteryLevelModel;
            if (masteryModel == null) return -3;

            // TODO: find a better way !!
#warning !! HACK !!
            var bot = MainWindow.CurBot;
            if (bot == null) return -5;

            return bot.Char.Masteries.GetLevel(masteryModel.Id);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
