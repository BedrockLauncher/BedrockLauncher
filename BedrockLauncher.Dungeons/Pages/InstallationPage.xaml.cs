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
using BL_Core.Controls;

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
            FolderSelectDialog dialog = new FolderSelectDialog();
            if (dialog.Show())
            {
                Properties.DungeonSettings.Default.InstallLocation = dialog.FileName;
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
        }

        private void ResetDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.DungeonSettings.Default.InstallLocation = string.Empty;
            Properties.DungeonSettings.Default.Save();
        }

        private void BrowseModDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            FolderSelectDialog dialog = new FolderSelectDialog();
            if (dialog.Show())
            {
                Properties.DungeonSettings.Default.ModsLocation = dialog.FileName;
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
    }
}
