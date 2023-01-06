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
using BedrockLauncher.Dungeons.Methods;
using BedrockLauncher.UI.Controls.Misc;
using FolderBrowserEx;

namespace BedrockLauncher.Dungeons.Pages
{
    /// <summary>
    /// Interaction logic for InstallationPage.xaml
    /// </summary>
    public partial class InstallationPage : Page
    {
        public InstallationPage()
        {
            InitializeComponent();
        }

        private void BrowseDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Properties.DungeonSettings.Default.InstallLocation = dialog.SelectedFolder;
                Properties.DungeonSettings.Default.Save();
            }
        }

        private void OpenConfigButton_Click(object sender, RoutedEventArgs e)
        {
            GameManager.OpenConfigFolder();
        }

        private void RepairButton_Click(object sender, RoutedEventArgs e)
        {
            GameManager.RepairDungeons();
        }

        private void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            GameManager.RemoveModManagement();
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            GameManager.InstallModManagement();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.DungeonSettings.Default.Save();
            if (InstructionsTab.IsSelected) UpdateInstructions();
        }

        private void ResetDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.DungeonSettings.Default.InstallLocation = string.Empty;
            Properties.DungeonSettings.Default.Save();
        }

        private void BrowseModDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Properties.DungeonSettings.Default.ModsLocation = dialog.SelectedFolder;
                Properties.DungeonSettings.Default.Save();
            }
        }

        private void ResetModDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.DungeonSettings.Default.ModsLocation = string.Empty;
            Properties.DungeonSettings.Default.Save();
        }

        private void OpenSaveDataButton_Click(object sender, RoutedEventArgs e)
        {
            GameManager.OpenSaveDataFolder();
        }

        private void OpenContentButton_Click(object sender, RoutedEventArgs e)
        {
            GameManager.OpenContentFolder();
        }

        private void InstallStorePatch_Click(object sender, RoutedEventArgs e)
        {
            GameManager.InstallStorePatch(false);
        }

        private void UpdateStorePatch_Click(object sender, RoutedEventArgs e)
        {
            GameManager.InstallStorePatch(true);
        }

        private void TabItem_Selected(object sender, RoutedEventArgs e)
        {
            UpdateInstructions();
        }

        private void UpdateInstructions()
        {
            string documentation;
            switch (Properties.DungeonSettings.Default.GameVariant)
            {
                case Enums.GameVariant.Launcher: documentation = "Dungeons_LauncherInstructions.md"; break;
                case Enums.GameVariant.Store: documentation = "Dungeons_StoreInstructions.md"; break;
                case Enums.GameVariant.Steam: documentation = "Dungeons_SteamInstructions.md"; break;
                default: throw new NotImplementedException();
            }

            if (BedrockLauncher.Localization.Language.LanguageManager.TryGetResource(documentation, out string contents))
                Markdownview.Markdown = contents;

        }
    }
}
