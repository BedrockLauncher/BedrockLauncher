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
            ((MainWindow)Application.Current.MainWindow).UpdateInstallationsList();

        }

        public void RefreshInstallationsList(object sender, RoutedEventArgs e)
        {
            RefreshInstallationsList();
        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var installation = button.DataContext as Classes.Installation;
            ((MainWindow)Application.Current.MainWindow).Play(installation);
        }

        private void NewInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Content = new AddInstallationScreen();
        }

        private void DeleteInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = sender as MenuItem;
            var installation = button.DataContext as Classes.Installation;
            ConfigManager configManager = new ConfigManager();
            configManager.DeleteInstallation(installation);
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
    }
}
