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

namespace sroBot.Pages.Alchemy
{
    /// <summary>
    /// Interaction logic for Blues.xaml
    /// </summary>
    public partial class Blues : UserControl
    {
        public Blues()
        {
            InitializeComponent();
            
        }

        private void guiBtn_fuse_Click(object sender, RoutedEventArgs e)
        {
            var bot = DataContext as SROBot.Bot;
            if (bot == null) return;

            bot.Alchemy.StartBlue();
        }
    }
}
