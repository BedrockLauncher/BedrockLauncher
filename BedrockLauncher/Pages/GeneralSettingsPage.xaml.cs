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

namespace BedrockLauncher.Pages
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
            portableModeCheckBox.IsChecked = Properties.Settings.Default.PortableMode;
            experiementalDataSaveRedirection.IsChecked = Properties.Settings.Default.SaveRedirection;
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
            switch (portableModeCheckBox.IsChecked)
            {
                case true:
                    Properties.Settings.Default.PortableMode = true;
                    Properties.Settings.Default.Save();
                    break;
                case false:
                    Properties.Settings.Default.PortableMode = false;
                    Properties.Settings.Default.Save();
                    break;
            }

            ConfigManager.Init();
        }

        private void experiementalDataSaveRedirection_Click(object sender, RoutedEventArgs e)
        {
            switch (experiementalDataSaveRedirection.IsChecked)
            {
                case true:
                    Properties.Settings.Default.SaveRedirection = true;
                    Properties.Settings.Default.Save();
                    break;
                case false:
                    Properties.Settings.Default.SaveRedirection = false;
                    Properties.Settings.Default.Save();
                    break;
            }
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.GameManager.ConvertToInstallation();
        }
    }
}
