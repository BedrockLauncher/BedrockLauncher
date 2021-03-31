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

namespace BedrockLauncher.Pages.VersionsScreen
{
    /// <summary>
    /// Логика взаимодействия для InstallationsScreen.xaml
    /// </summary>
    public partial class VersionsScreen : Page
    {
        public VersionsScreen()
        {
            InitializeComponent();
        }

        public void RefreshInstallationsList(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).UpdateVersionsList();
        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {

        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
