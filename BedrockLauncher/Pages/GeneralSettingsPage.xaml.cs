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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BedrockLauncher.Methods;
using Microsoft.Win32;

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

        private void Update()
        {
            // Set checkboxes
            keepLauncherOpenCheckBox.IsChecked = Properties.Settings.Default.KeepLauncherOpen;
            experiementalDataSaveRedirection.IsChecked = Properties.Settings.Default.SaveRedirection;
            hideJavaLauncherButtonCheckbox.IsChecked = Properties.Settings.Default.HideJavaShortcut;
            showExternalLauncherButtonCheckbox.IsChecked = Properties.Settings.Default.ShowExternalLauncher;
            closeLauncherOnSwitchCheckbox.IsChecked = Properties.Settings.Default.CloseLauncherOnSwitch;
            betaVersionsButtonCheckbox.IsChecked = Properties.Settings.Default.UseBetaBuilds;
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            Update();
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

        private void AdvancedSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            AdvancedOptionsWindow advancedOptionsWindow = new AdvancedOptionsWindow();
            advancedOptionsWindow.ShowDialog();
        }

        private void hideJavaLauncherButtonCheckbox_Click(object sender, RoutedEventArgs e)
        {
            switch (hideJavaLauncherButtonCheckbox.IsChecked)
            {
                case true:
                    Properties.Settings.Default.HideJavaShortcut = true;
                    Properties.Settings.Default.Save();
                    break;
                case false:
                    Properties.Settings.Default.HideJavaShortcut = false;
                    Properties.Settings.Default.Save();
                    break;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void ExternalLauncherPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void BrowseLauncherButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                InitialDirectory = ExternalLauncherPathTextBox.Text
            };
            if (dialog.ShowDialog().Value)
            {
                Properties.Settings.Default.ExternalLauncherPath = dialog.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void BrowseExternalLauncherIcon_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Filter = "PNG Files (*.png) | *.png"
            };
            if (dialog.ShowDialog().Value)
            {
                string fileToUse = Methods.Filepaths.AddImageToIconCache(dialog.FileName);
                Properties.Settings.Default.ExternalLauncherIconPath = fileToUse;
                Properties.Settings.Default.Save();
            }
        }

        private void ResetExternalLauncherIcon_Button_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ExternalLauncherIconPath = string.Empty;
            Properties.Settings.Default.Save();
        }

        private void showExternalLauncherButtonCheckbox_Click(object sender, RoutedEventArgs e)
        {
            switch (showExternalLauncherButtonCheckbox.IsChecked)
            {
                case true:
                    Properties.Settings.Default.ShowExternalLauncher = true;
                    Properties.Settings.Default.Save();
                    break;
                case false:
                    Properties.Settings.Default.ShowExternalLauncher = false;
                    Properties.Settings.Default.Save();
                    break;
            }
        }

        private void closeLauncherOnSwitchCheckbox_Click(object sender, RoutedEventArgs e)
        {
            switch (closeLauncherOnSwitchCheckbox.IsChecked)
            {
                case true:
                    Properties.Settings.Default.CloseLauncherOnSwitch = true;
                    Properties.Settings.Default.Save();
                    break;
                case false:
                    Properties.Settings.Default.CloseLauncherOnSwitch = false;
                    Properties.Settings.Default.Save();
                    break;
            }
        }

        private void betaVersionsButtonCheckbox_Click(object sender, RoutedEventArgs e)
        {
            switch (betaVersionsButtonCheckbox.IsChecked)
            {
                case true:
                    Properties.Settings.Default.UseBetaBuilds = true;
                    Properties.Settings.Default.Save();
                    break;
                case false:
                    Properties.Settings.Default.UseBetaBuilds = false;
                    Properties.Settings.Default.Save();
                    break;
            }
        }
    }
}
