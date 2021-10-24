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

namespace sroBot.Pages.Consignment
{
    /// <summary>
    /// Interaction logic for settings.xaml
    /// </summary>
    public partial class settings : UserControl
    {
        public settings()
        {
            InitializeComponent();
        }

        private void guiBtn_consignmentCopyRules_Click(object sender, RoutedEventArgs e)
        {
            var bot = this.DataContext as SROBot.Bot;
            if (bot == null) return;

            Clipboard.SetDataObject(bot.Config.Consignment.SellConfiguration.ToArray(), true);
        }

        private void guiBtn_consignmentImportRules_Click(object sender, RoutedEventArgs e)
        {
            var bot = this.DataContext as SROBot.Bot;
            if (bot == null) return;

            try
            {
                var cbDataObject = Clipboard.GetDataObject();
                var cbData = cbDataObject.GetData(typeof(SROBot.Configuration.ConsignmentSellOptions[]));
                var consignmentRules = cbData as SROBot.Configuration.ConsignmentSellOptions[];
                foreach (var rule in consignmentRules)
                {
                    if (bot.Config.Consignment.SellConfiguration.Contains(rule))
                    {
                        bot.Config.Consignment.SellConfiguration.Remove(rule); // overwrite old one !
                    }

                    bot.Config.Consignment.SellConfiguration.Add(rule);
                }

                var consigConfig = bot.Config.Consignment.SellConfiguration.Distinct().ToList();
                bot.Config.Consignment.SellConfiguration.Clear();
                consigConfig.ForEach(sc => bot.Config.Consignment.SellConfiguration.Add(sc));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not import settings..\r\nNothing similiar found in Clipboard!", "Consignment rule import - ERROR!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
