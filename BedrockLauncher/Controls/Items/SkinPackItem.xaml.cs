using BedrockLauncher.Classes.SkinPack;
using BedrockLauncher.Core;
using BedrockLauncher.Pages;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace BedrockLauncher.Controls.Items
{
    /// <summary>
    /// Interaction logic for SkinPackItem.xaml
    /// </summary>
    public partial class SkinPackItem : UserControl
    {
        public SkinPackItem()
        {
            InitializeComponent();
        }

        public Visibility ButtonPanelVisibility
        {
            get
            {
                return ButtonPanel != null ? ButtonPanel.Visibility : Visibility.Collapsed;
            }
            set
            {
                if (ButtonPanel != null) ButtonPanel.Visibility = value;
            }
        }

        public static readonly DependencyProperty dependencyProperty = DependencyProperty.Register("ButtonPanelVisibility", typeof(Visibility), typeof(SkinPackItem), new PropertyMetadata(Visibility.Collapsed, new PropertyChangedCallback(ChangePanelVisibility)));

        private static void ChangePanelVisibility(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as SkinPackItem).ButtonPanelVisibility = (Visibility)e.NewValue;
        }

        private SkinsPage GetParent()
        {
            return this.Tag as SkinsPage;
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var skinPack = button.DataContext as MCSkinPack;
            OpenContextMenu(skinPack);
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            GetParent().LoadedSkinPacks.SelectedItem = null;
        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var skinPack = button.DataContext as MCSkinPack;
            ConfigManager.GameManager.OpenFolder(skinPack);
        }

        public void OpenContextMenu(MCSkinPack skinPack)
        {
            GetParent().LoadedSkinPacks.SelectedItem = skinPack;
            MoreButton.ContextMenu.PlacementTarget = MoreButton;
            MoreButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            MoreButton.ContextMenu.DataContext = skinPack;
            MoreButton.ContextMenu.IsOpen = true;
        }

        private void EditSkinPackButton_Click(object sender, RoutedEventArgs e)
        {
            var skinPack = GetParent().LoadedSkinPacks.SelectedItem as MCSkinPack;
            int index = GetParent().LoadedSkinPacks.SelectedIndex;
            ConfigManager.MainThread.SetOverlayFrame(new EditSkinPackScreen(skinPack, index));

            MoreButton.ContextMenu.IsOpen = false;
        }

        private async void DeleteSkinPackButton_Click(object sender, RoutedEventArgs e)
        {
            MoreButton.ContextMenu.IsOpen = false;
            var skinPack = GetParent().LoadedSkinPacks.SelectedItem as MCSkinPack;

            if (skinPack == null) return;

            var title = this.FindResource("Dialog_DeleteItem_Title") as string;
            var content = this.FindResource("Dialog_DeleteItem_Text") as string;
            var item = this.FindResource("Dialog_Item_SkinPack_Text") as string;

            var result = await DialogPrompt.ShowDialog_YesNo(title, content, item, skinPack.DisplayName);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                try
                {

                    Directory.Delete(skinPack.Directory, true);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                GetParent().ReloadSkinPacks();
            }
        }
    }
}
