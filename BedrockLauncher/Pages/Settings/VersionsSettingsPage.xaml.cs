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

namespace BedrockLauncher.Pages.Settings
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
            ConfigManager.OnConfigStateChanged(this, Events.ConfigStateArgs.Empty);
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
