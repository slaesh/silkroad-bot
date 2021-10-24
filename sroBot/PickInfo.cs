using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using sroBot.SROBot;

namespace sroBot
{
    public class PickIfSmallerThanConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = value as String;
            if (val == null) return null;

            uint number = 0;
            if (!uint.TryParse(val, out number)) return 0;

            return number;
        }
    }

    public class PickInfo : MVVM.ViewModelBase
    {
        public uint Model { get; set; } = 0;

        public bool? Pick
        {
            get { return GetValue(() => Pick); }
            set { SetValue(() => Pick, value); }
        }

        public uint PickIfSmallerThan
        {
            get { return GetValue(() => PickIfSmallerThan); }
            set { SetValue(() => PickIfSmallerThan, value); }
        }

        public bool? Sell
        {
            get { return GetValue(() => Sell); }
            set { SetValue(() => Sell, value); }
        }

        public bool? MoveFromPetToInventory
        {
            get { return GetValue(() => MoveFromPetToInventory); }
            set { SetValue(() => MoveFromPetToInventory, value); }
        }

        public bool? Storage
        {
            get { return GetValue(() => Storage); }
            set { SetValue(() => Storage, value); }
        }

        public bool? GuildStorage
        {
            get { return GetValue(() => GuildStorage); }
            set { SetValue(() => GuildStorage, value); }
        }

        private PickInfo()
        {
            PickIfSmallerThan = 0;
            Sell = false;
            Pick = false;
            MoveFromPetToInventory = false;
            Storage = false;
            GuildStorage = false;
        }

        public PickInfo(ItemInfo iteminfo) : this()
        {
            if (iteminfo != null)
                Model = iteminfo.Model;
        }

        public static PickInfo[] Load(String file)
        {
            if (!File.Exists(file)) return new PickInfo[0];
            return new ConfigHandler.JsonConfiguration<PickInfo[]>(file).Load();
        }

        public static bool Save(String file, IEnumerable<PickInfo> pickinfos)
        {
            return new ConfigHandler.JsonConfiguration<PickInfo[]>(file).Save(pickinfos.ToArray());
        }
    }
}
