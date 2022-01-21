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
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.Pages.Common;
using BedrockLauncher.Classes.SkinPack;
using BedrockLauncher.UI.Pages.Common;

namespace BedrockLauncher.Controls.Items.Skins
{
    /// <summary>
    /// Interaction logic for SkinItem.xaml
    /// </summary>
    public partial class SkinItem : UserControl
    {
        public SkinItem()
        {
            InitializeComponent();
        }

        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void ContextMenu_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {

        }

        private Pages.Play.SkinsPage GetParent()
        {
            return this.Tag as Pages.Play.SkinsPage;
        }

        public void OpenContextMenu()
        {
            control.ContextMenu.PlacementTarget = control;
            control.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            control.ContextMenu.IsOpen = true;
        }

        private void EditSkinButton_Click(object sender, RoutedEventArgs e)
        {
            var skinPack = GetParent().LoadedSkinPacks.SelectedItem as MCSkinPack;
            var skin = GetParent().SkinPreviewList.SelectedItem as MCSkin;
            int index = GetParent().SkinPreviewList.SelectedIndex;

            Keyboard.ClearFocus();

            ViewModels.MainViewModel.Default.SetOverlayFrame(new EditSkinScreen(skinPack, skin, index));
        }

        private async void DeleteSkinButton_Click(object sender, RoutedEventArgs e)
        {
            var skinPack = GetParent().LoadedSkinPacks.SelectedItem as MCSkinPack;
            var skin = GetParent().SkinPreviewList.SelectedItem as MCSkin;
            int index = GetParent().SkinPreviewList.SelectedIndex;

            if (skin != null && skinPack != null)
            {
                var title = this.FindResource("Dialog_DeleteItem_Title") as string;
                var content = this.FindResource("Dialog_DeleteItem_Text") as string;
                var item = this.FindResource("Dialog_Item_Skin_Text") as string;

                var result = await DialogPrompt.ShowDialog_YesNo(title, content, item, skinPack.GetLocalizedSkinName(skin.localization_name));

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    skinPack.RemoveSkin(index);
                    GetParent().ReloadSkinPacks();
                }
            }
        }
    }
}
