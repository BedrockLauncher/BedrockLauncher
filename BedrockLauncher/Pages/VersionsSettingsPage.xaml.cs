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

namespace BedrockLauncher.Pages
{
    /// <summary>
    /// Логика взаимодействия для InstallationsScreen.xaml
    /// </summary>
    public partial class VersionsSettingsPage : Page
    {
        public VersionsSettingsPage()
        {
            InitializeComponent();
        }

        public void RefreshVersionsList()
        {
            ConfigManager.OnConfigStateChanged(this, ConfigManager.ConfigStateArgs.Empty);
        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var version = button.DataContext as Classes.MCVersion;
            ConfigManager.GameManager.OpenFolder(version);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var version = button.DataContext as Classes.MCVersion;
            ConfigManager.GameManager.Remove(version);
            RefreshVersionsList();
        }

        private void More_Click(object sender, RoutedEventArgs e)
        {
            /*
            Button button = sender as Button;
            var installation = button.DataContext as Classes.Installation;
            InstallationsList.SelectedItem = installation;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.DataContext = installation;
            button.ContextMenu.IsOpen = true;
            */
        }

        private void Page_Initialized(object sender, EventArgs e)
        {

        }

        private void RefreshVersionsList(object sender, RoutedEventArgs e)
        {
            RefreshVersionsList();
        }
    }
}
