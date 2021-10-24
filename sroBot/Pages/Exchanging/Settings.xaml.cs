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

namespace sroBot.Pages.Exchanging
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

        private void AddPlayer(string player)
        {
            var bot = DataContext as Bot;
            if (bot == null) return;

            if (bot.Config.Exchanging.Players.Any(p => p.Equals(player, StringComparison.OrdinalIgnoreCase))) return;

            bot.Config.Exchanging.Players.Add(player);

            GuiTextbox_player.Text = "";
        }

        private void GuiBtn_addPlayerToList_Click(object sender, RoutedEventArgs e)
        {
            var player = GuiTextbox_player.Text.Trim();
            if (string.IsNullOrEmpty(player)) return;

            AddPlayer(player);
        }

        private void GuiListbox_players_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var player = GuiListbox_players.SelectedItem as string;
            if (string.IsNullOrEmpty(player)) return;

            player = player.Trim();

            var bot = DataContext as Bot;
            if (bot == null) return;

            if (!bot.Config.Exchanging.Players.Any(p => p.Equals(player, StringComparison.OrdinalIgnoreCase))) return;

            bot.Config.Exchanging.Players.Remove(player);
        }

        private void GuiTextbox_player_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var player = GuiTextbox_player.Text.Trim();
            if (string.IsNullOrEmpty(player)) return;

            AddPlayer(player);
        }
    }
}
