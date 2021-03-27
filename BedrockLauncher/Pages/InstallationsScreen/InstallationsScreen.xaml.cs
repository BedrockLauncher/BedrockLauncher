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

        public void RefreshInstallationsList(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).UpdateVersionsList();
        }
    }
}
