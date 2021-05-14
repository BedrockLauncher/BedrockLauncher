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
using BedrockLauncher.Pages.Preview;

namespace BedrockLauncher.Pages.Play
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
            ConfigManager.OnConfigStateChanged(this, Events.ConfigStateArgs.Empty);
        }

        public void RefreshInstallationsList(object sender, RoutedEventArgs e)
        {
            RefreshInstallationsList();
        }

        private void NewInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.SetOverlayFrame(new EditInstallationScreen());
        }



        private void Page_Initialized(object sender, EventArgs e)
        {

        }


    }
}
