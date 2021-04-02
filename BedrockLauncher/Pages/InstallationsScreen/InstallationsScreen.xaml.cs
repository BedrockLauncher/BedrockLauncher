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
using BedrockLauncher.Methods;
using BedrockLauncher.Core;

namespace BedrockLauncher.Pages.InstallationsScreen
{
    /// <summary>
    /// Логика взаимодействия для InstallationsScreen.xaml
    /// </summary>
    public partial class InstallationsScreen : Page
    {
        public InstallationsScreen()
        {
            InitializeComponent();
        }

        public void RefreshInstallationsList()
        {
            ConfigManager.OnConfigStateChanged(this, ConfigManager.ConfigStateArgs.Empty);
        }

        public void RefreshInstallationsList(object sender, RoutedEventArgs e)
        {
            RefreshInstallationsList();
        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var installation = button.DataContext as Classes.Installation;
            ConfigManager.GameManager.OpenFolder(installation);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var installation = button.DataContext as Classes.Installation;
            ConfigManager.GameManager.Play(installation);
        }

        private void NewInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.MainWindowOverlayFrame.Content = new ManageInstallationScreen();
        }

        private void DeleteInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = sender as MenuItem;
            var installation = button.DataContext as Classes.Installation;
            ConfigManager.DeleteInstallation(installation);
            RefreshInstallationsList();
        }

        private void More_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var installation = button.DataContext as Classes.Installation;
            InstallationsList.SelectedItem = installation;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.DataContext = installation;
            button.ContextMenu.IsOpen = true;
        }

        private void Page_Initialized(object sender, EventArgs e)
        {

        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            InstallationsList.SelectedItem = null;
        }

        private void EditInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = sender as MenuItem;
            var installation = button.DataContext as Classes.Installation;
            int index = ConfigManager.CurrentInstallations.IndexOf(installation);
            ConfigManager.MainThread.MainWindowOverlayFrame.Content = new ManageInstallationScreen(index, installation);
        }
    }
}
