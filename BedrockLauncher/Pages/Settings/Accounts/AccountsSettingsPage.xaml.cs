using BedrockLauncher.UpdateProcessor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace BedrockLauncher.Pages.Settings.Accounts
{
    /// <summary>
    /// Логика взаимодействия для AccountsSettingsPage.xaml
    /// </summary>
    public partial class AccountsSettingsPage : Page
    {
        public AccountsSettingsPage()
        {
            InitializeComponent();
        }

        private void XboxInsiderLegacy_Click(object sender, RoutedEventArgs e)
        {
            JemExtensions.WebExtensions.LaunchWebLink("xbox-insider://");
        }
        private void XboxInsiderNew_Click(object sender, RoutedEventArgs e)
        {
            JemExtensions.WebExtensions.LaunchWebLink("xbox-insider2://");
        }

        private void MSAccounts_Click(object sender, RoutedEventArgs e)
        {
            JemExtensions.WebExtensions.LaunchWebLink("ms-settings:emailandaccounts");
        }

        private void Page_Initialized(object sender, RoutedEventArgs e)
        {

        }
    }
}
