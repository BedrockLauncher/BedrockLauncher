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
using BedrockLauncher.Core;

namespace BedrockLauncher.Pages.SettingsScreen
{
    /// <summary>
    /// Логика взаимодействия для GeneralSettingsPage.xaml
    /// </summary>
    public partial class GeneralSettingsPage : Page
    {
        public GeneralSettingsPage()
        {
            InitializeComponent();
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            // Set checkboxes
            keepLauncherOpenCheckBox.IsChecked = Properties.Settings.Default.KeepLauncherOpen;
            useFixedInstallLocation.IsChecked = !Properties.Settings.Default.PortableInstalls;
            useFixedProfileLocation.IsChecked = !Properties.Settings.Default.PortableProfiles;
            experiementalDataSaveRedirection.IsChecked = Properties.Settings.Default.EnableExperiementalSaveRedirection;
        }

        private void keepLauncherOpenCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // get and save value of checkbox
            switch (keepLauncherOpenCheckBox.IsChecked)
            {
                case true:
                    Properties.Settings.Default.KeepLauncherOpen = true;
                    Properties.Settings.Default.Save();
                    break;
                case false:
                    Properties.Settings.Default.KeepLauncherOpen = false;
                    Properties.Settings.Default.Save();
                    break;
            }
        }

        private void useFixedInstallLocation_Click(object sender, RoutedEventArgs e)
        {
            // get and save value of checkbox
            switch (useFixedInstallLocation.IsChecked)
            {
                case true:
                    Properties.Settings.Default.PortableInstalls = false;
                    Properties.Settings.Default.Save();
                    break;
                case false:
                    Properties.Settings.Default.PortableInstalls = true;
                    Properties.Settings.Default.Save();
                    break;
            }

            ConfigManager.ReloadVersions();
        }

        private void useFixedProfileLocation_Click(object sender, RoutedEventArgs e)
        {
            // get and save value of checkbox
            switch (useFixedProfileLocation.IsChecked)
            {
                case true:
                    Properties.Settings.Default.PortableProfiles = false;
                    Properties.Settings.Default.Save();
                    break;
                case false:
                    Properties.Settings.Default.PortableProfiles = true;
                    Properties.Settings.Default.Save();
                    break;
            }

            ConfigManager.ReloadProfiles();
        }

        private void experiementalDataSaveRedirection_Click(object sender, RoutedEventArgs e)
        {
            switch (experiementalDataSaveRedirection.IsChecked)
            {
                case true:
                    Properties.Settings.Default.EnableExperiementalSaveRedirection = true;
                    Properties.Settings.Default.Save();
                    break;
                case false:
                    Properties.Settings.Default.EnableExperiementalSaveRedirection = false;
                    Properties.Settings.Default.Save();
                    break;
            }
        }
    }
}
