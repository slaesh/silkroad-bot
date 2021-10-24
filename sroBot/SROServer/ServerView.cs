using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace sroBot.SROServer
{
    public class GetBotsConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var server = value as Server;
            if (server == null) return null;

            return server.GetBots();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Do the conversion from visibility to bool
            return null;
        }
    }

    public class GetNameConverter : MVVM.BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var server = value as Server;
            if (server == null) return null;

            return server.Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Do the conversion from visibility to bool
            return null;
        }
    }
}
