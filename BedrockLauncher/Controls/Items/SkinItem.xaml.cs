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
    public partial class SkinItem : UserControl
    {
        public SkinItem()
        {
            InitializeComponent();
        }

        private SkinsPage GetParent()
        {
            return this.Tag as SkinsPage;
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

            ConfigManager.MainThread.SetOverlayFrame(new EditSkinScreen(skinPack, skin, index));
        }

        private void DeleteSkinButton_Click(object sender, RoutedEventArgs e)
        {
            var skinPack = GetParent().LoadedSkinPacks.SelectedItem as MCSkinPack;
            var skin = GetParent().SkinPreviewList.SelectedItem as MCSkin;
            int index = GetParent().SkinPreviewList.SelectedIndex;

            if (skin != null && skinPack != null)
            {
                skinPack.RemoveSkin(index);
                GetParent().ReloadSkinPacks();
            }
        }
    }
}
