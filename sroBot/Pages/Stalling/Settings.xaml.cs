using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace sroBot.Pages.Stalling
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void guiBtn_openStall_Click(object sender, RoutedEventArgs e)
        {
            var bot = DataContext as SROBot.Bot;
            if (bot == null) return;
            
            new Thread(() =>
            {
                bot.Stall.Create();

                Thread.Sleep(1000);

                bot.Stall.PutNewItems();
            }).Start();
        }

        private void guiBtn_closeStall_Click(object sender, RoutedEventArgs e)
        {
            var bot = DataContext as SROBot.Bot;
            if (bot == null) return;

            bot.Stall.Close();
        }

    }
}
