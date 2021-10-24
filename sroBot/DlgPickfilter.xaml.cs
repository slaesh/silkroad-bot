using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace sroBot
{
    public class PickInfoView
    {
        public ItemInfo Iteminfo { get; set; } = new ItemInfo(0, "");
        public PickInfo Pickinfos { get; set; } = new PickInfo(null);

        public PickInfoView(ItemInfo iteminfo, PickInfo pickinfo)
        {
            Iteminfo = iteminfo;
            Pickinfos = pickinfo;
        }
    }

    /// <summary>
    /// Interaktionslogik für DlgPickfilter.xaml
    /// </summary>
    public partial class DlgPickfilter : Window
    {
        public DlgPickfilter()
        {
            InitializeComponent();
            guiListview_items.ItemsSource = new ObservableCollection<PickInfoView>(ItemInfos.ItemList.Select(i => new PickInfoView(i, new PickInfo(i))).ToArray());

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(guiListview_items.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("Iteminfo.Type", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("Iteminfo.Degree", ListSortDirection.Ascending));
        }

        private void guiBtn_save_Click(object sender, RoutedEventArgs e)
        {
            var pickinfos = guiListview_items.Items.Cast<PickInfoView>().Where(pi => pi.Pickinfos.Pick == true || pi.Pickinfos.Sell == true);
            if (pickinfos.Any())
            {
                PickInfo.Save(System.IO.Path.Combine(App.ExecutingPath, "pickinfos.json"), pickinfos.Select(pi => pi.Pickinfos));
            }
        }

        private void filterChanged(object sender, object arg)
        {
            if (!this.IsLoaded) return;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(guiListview_items.ItemsSource);

            var txt = guiTextbox_search.Text;
            var isRare = guiCheckbox_isRare.IsChecked == true;
            var isChn = guiCheckbox_isChn.IsChecked == true;
            var isEu = guiCheckbox_isEu.IsChecked == true;
            var isWeapon = guiCheckbox_isWeapon.IsChecked == true;
            var isShield = guiCheckbox_isShield.IsChecked == true;
            var isArmor = guiCheckbox_isArmor.IsChecked == true;
            var isAccessory = guiCheckbox_isAccessory.IsChecked == true;

            var minDg = 0;
            var maxDg = 13;
            if (!int.TryParse(guiTextbox_degreeStart.Text, out minDg)) minDg = 0;
            if (!int.TryParse(guiTextbox_degreeEnd.Text, out maxDg)) maxDg = 13;

            var minLvl = 0;
            var maxLvl = 130;
            if (!int.TryParse(guiTextbox_lvlStart.Text, out minLvl)) minLvl = 0;
            if (!int.TryParse(guiTextbox_lvlEnd.Text, out maxLvl)) maxLvl = 130;

            view.Filter = (o) =>
            {
                var piv = o as PickInfoView;
                if (piv == null) return false;

                if ((!isRare || piv.Iteminfo.IsSOX) &&
                    (!isChn || piv.Iteminfo.IsChinese) &&
                    (!isEu || piv.Iteminfo.IsEuropean) &&
                    (!isWeapon || piv.Iteminfo.IsWeapon) &&
                    (!isShield || piv.Iteminfo.IsShield) &&
                    (!isArmor || piv.Iteminfo.IsArmor) &&
                    (!isAccessory || piv.Iteminfo.IsAccessory) &&

                    (piv.Iteminfo.Level >= minLvl && piv.Iteminfo.Level <= maxLvl) &&
                    (piv.Iteminfo.Degree >= minDg && piv.Iteminfo.Degree <= maxDg) &&

                    (txt == "" || piv.Iteminfo.Name.IndexOf(txt, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }

                return false;
            };
        }

        private void guiTextbox_search_TextChanged(object sender, TextChangedEventArgs e)
        {
            filterChanged(sender, e);
        }

        private void pickInfo_Sell_Click(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null) return;

            foreach (PickInfoView piv in guiListview_items.SelectedItems)
            {
                piv.Pickinfos.Sell = checkbox.IsChecked == true;
            }
        }

        private void pickInfo_Pick_Click(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null) return;

            foreach (PickInfoView piv in guiListview_items.SelectedItems)
            {
                piv.Pickinfos.Pick = checkbox.IsChecked == true;
            }
        }

        private void pickInfo_ToInventory_Click(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null) return;

            foreach (PickInfoView piv in guiListview_items.SelectedItems)
            {
                piv.Pickinfos.MoveFromPetToInventory = checkbox.IsChecked == true;
            }
        }

        private void pickInfo_Storage_Click(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null) return;

            foreach (PickInfoView piv in guiListview_items.SelectedItems)
            {
                piv.Pickinfos.Storage = checkbox.IsChecked == true;
            }
        }

        private void pickInfo_GuildStorage_Click(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null) return;

            foreach (PickInfoView piv in guiListview_items.SelectedItems)
            {
                piv.Pickinfos.GuildStorage = checkbox.IsChecked == true;
            }
        }

        private void pickInfo_PickIfSmallerThan_TextChanged(object sender, TextChangedEventArgs e)
        {
            var txtbox = sender as TextBox;
            if (txtbox == null) return;

            foreach (PickInfoView piv in guiListview_items.SelectedItems)
            {
                piv.Pickinfos.PickIfSmallerThan = uint.Parse(txtbox.Text);
                if (piv.Pickinfos.PickIfSmallerThan != 0)
                {
                    piv.Pickinfos.Pick = true;
                }
            }
        }
    }
}
