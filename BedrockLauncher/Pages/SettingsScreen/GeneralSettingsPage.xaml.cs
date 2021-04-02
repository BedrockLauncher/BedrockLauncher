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
            keepLauncherOpenCheckBox.IsChecked = Properties.Settings.Default.KeepLauncherOpenCheckBox;
            usedFixedInstallLocation.IsChecked = Properties.Settings.Default.FixedInstallFolder;
        }

        private void keepLauncherOpenCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // get and save value of checkbox
            switch (keepLauncherOpenCheckBox.IsChecked)
            {
                case true:
                    Properties.Settings.Default.KeepLauncherOpenCheckBox = true;
                    Properties.Settings.Default.Save();
                    break;
                case false:
                    Properties.Settings.Default.KeepLauncherOpenCheckBox = false;
                    Properties.Settings.Default.Save();
                    break;
            }
        }

        private void usedFixedInstallLocation_Click(object sender, RoutedEventArgs e)
        {
            // get and save value of checkbox
            switch (usedFixedInstallLocation.IsChecked)
            {
                case true:
                    Properties.Settings.Default.FixedInstallFolder = true;
                    Properties.Settings.Default.Save();
                    break;
                case false:
                    Properties.Settings.Default.FixedInstallFolder = false;
                    Properties.Settings.Default.Save();
                    break;
            }
        }
    }
}
