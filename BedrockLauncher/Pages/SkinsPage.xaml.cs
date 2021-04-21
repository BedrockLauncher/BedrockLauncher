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
using BedrockLauncher.Classes.SkinPack;
using BedrockLauncher.Core;
using BedrockLauncher.Methods;
using System.IO;
using System.Data;

namespace BedrockLauncher.Pages
{
    /// <summary>
    /// Interaction logic for SkinsPage.xaml
    /// </summary>
    public partial class SkinsPage : Page
    {

        public List<MCSkinPack> SkinPacks { get; set; } = new List<MCSkinPack>();

        public SkinsPage()
        {
            InitializeComponent();
        }

        public void ReloadSkinPacks()
        {
            LoadedSkinPacks.Items.Clear();
            SkinPacks.Clear();


            string InstallationPath = Filepaths.GetInstallationsFolderPath(ConfigManager.CurrentProfile, ConfigManager.CurrentInstallation.DirectoryName);
            string normal_folder = Filepaths.GetSkinPacksFolderPath(InstallationPath, false);
            string dev_folder = Filepaths.GetSkinPacksFolderPath(InstallationPath, true);

            if (Directory.Exists(normal_folder)) AddPacks(normal_folder);
            if (Directory.Exists(dev_folder)) AddPacks(dev_folder);

            void AddPacks(string _SourceFolder)
            {
                var SourceFolder = new DirectoryInfo(_SourceFolder);
                var FoundFolders = SourceFolder.GetDirectories();
                foreach (var PossiblePack in FoundFolders)
                {
                    if (PossiblePack.GetFiles().ToList().Exists(x => x.Name == "manifest.json"))
                    {
                        var result = MCSkinPack.ValidatePack(PossiblePack.FullName);
                        if (result != null)
                        {
                            SkinPacks.Add(result);
                            LoadedSkinPacks.Items.Add(result);
                        }
                    }


                }
            }
        }

        private void ReloadSkins()
        {
            if (!this.IsInitialized) return;
            SkinPreviewList.Items.Clear();
            var selected_item = LoadedSkinPacks.SelectedItem as MCSkinPack;
            if (selected_item != null)
            {
                foreach (var skin in selected_item.Content.skins)
                {
                    SkinPreviewList.Items.Add(skin);
                }
            }


        }

        private void UpdateAddSkinButton()
        {
            var selected_item = LoadedSkinPacks.SelectedItem as MCSkinPack;
            AddSkinButton.IsEnabled = selected_item != null;
        }

        private void UpdateCurrentSkin()
        {
            var selected_skin = SkinPreviewList.SelectedItem as MCSkin;
            var selected_item = LoadedSkinPacks.SelectedItem as MCSkinPack;

            if (selected_skin != null && selected_item != null)
            {
                CurrentSkinNameTextBlock.Text = selected_item.GetLocalizedSkinName(selected_skin.localization_name);
                SkinPreviewPanel.UpdateSkin(selected_skin);
            }
            else
            {
                CurrentSkinNameTextBlock.Text = "NULL";
                SkinPreviewPanel.UpdateSkin();
            }
        }

        private void Page_Initialized(object sender, EventArgs e)
        {

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadSkinPacks();
            UpdateAddSkinButton();
        }

        private void LoadedSkinPacks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReloadSkins();
            UpdateCurrentSkin();
            UpdateAddSkinButton();
        }


        private void SkinPreviewList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCurrentSkin();
            UpdateAddSkinButton();
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var skinPack = button.DataContext as MCSkinPack;
            LoadedSkinPacks.SelectedItem = skinPack;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.DataContext = skinPack;
            button.ContextMenu.IsOpen = true;
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            LoadedSkinPacks.SelectedItem = null;
        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var skinPack = button.DataContext as MCSkinPack;
            ConfigManager.GameManager.OpenFolder(skinPack);
        }

        private void EditSkinButton_Click(object sender, RoutedEventArgs e)
        {
            var skinPack = LoadedSkinPacks.SelectedItem as MCSkinPack;
            var skin = SkinPreviewList.SelectedItem as MCSkin;
            int index = SkinPreviewList.SelectedIndex;

            ConfigManager.MainThread.MainWindowOverlayFrame.Content = new EditSkinScreen(skinPack, skin, index);
        }

        private void DeleteSkinButton_Click(object sender, RoutedEventArgs e)
        {
            var skinPack = LoadedSkinPacks.SelectedItem as MCSkinPack;
            var skin = SkinPreviewList.SelectedItem as MCSkin;
            int index = SkinPreviewList.SelectedIndex;

            if (skin != null && skinPack != null)
            {
                skinPack.RemoveSkin(index);
                ReloadSkinPacks();
            }
        }

        private void EditSkinPackButton_Click(object sender, RoutedEventArgs e)
        {
            var skinPack = LoadedSkinPacks.SelectedItem as MCSkinPack;
            int index = LoadedSkinPacks.SelectedIndex;
            ConfigManager.MainThread.MainWindowOverlayFrame.Content = new EditSkinPackScreen(skinPack, index);
        }

        private void DeleteSkinPackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var skinPack = LoadedSkinPacks.SelectedItem as MCSkinPack;
                Directory.Delete(skinPack.Directory, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            ReloadSkinPacks();
        }

        private void AddSkinButton_Click(object sender, RoutedEventArgs e)
        {
            var skinPack = LoadedSkinPacks.SelectedItem as MCSkinPack;
            ConfigManager.MainThread.MainWindowOverlayFrame.Content = new EditSkinScreen(skinPack);
        }

        private void NewSkinPackButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.MainWindowOverlayFrame.Content = new EditSkinPackScreen();
        }
    }
}
