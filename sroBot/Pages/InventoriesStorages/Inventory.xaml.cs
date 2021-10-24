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

namespace sroBot.Pages.InventoriesStorages
{
    /// <summary>
    /// Interaction logic for Inventory.xaml
    /// </summary>
    public partial class Inventory : UserControl
    {
        public Inventory()
        {
            InitializeComponent();
        }

        private void guiListbox_Inventory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var bot = DataContext as SROBot.Bot;
            if (bot == null) return;

            var lstbox = sender as ListBox;
            if (lstbox == null) return;
            var item = lstbox.SelectedItem as SROBot.InventoryItem;
            if (SROBot.Inventory.IsItemEmpty(item) || !item.Iteminfo.IsDrop) return;

            bot.Debug("whitestats:");
            bot.Debug(item.WhiteStats.ToString());
            bot.Debug();
            bot.Debug("bluestats:");
            foreach (var bs in item.BlueStats)
            {
                bot.Debug("{0}: {1}", bs.Key.Type, bs.Value);
            }
            bot.Debug();
        }
    }
}
