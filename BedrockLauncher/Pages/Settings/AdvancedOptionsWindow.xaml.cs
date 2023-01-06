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
using System.Windows.Shapes;
using BedrockLauncher.Extensions;
using BedrockLauncher.Controls.Various;
using BedrockLauncher.ViewModels;
using BedrockLauncher.UI.Controls.Misc;
using FolderBrowserEx;

namespace BedrockLauncher.Pages.Settings
{
    /// <summary>
    /// Interaction logic for AdvancedOptionsWindow.xaml
    /// </summary>
    public partial class AdvancedOptionsWindow : Window
    {
        private bool RestartNeeded = false;

        public AdvancedOptionsWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            portableModeCheckBox.IsChecked = Properties.LauncherSettings.Default.PortableMode;
            UpdateDirectoryPathTextbox();
        }

        private void useFixedInstallLocation_Click(object sender, RoutedEventArgs e)
        {
            // get and save value of checkbox
            switch (portableModeCheckBox.IsChecked)
            {
                case true:
                    Properties.LauncherSettings.Default.PortableMode = true;
                    Properties.LauncherSettings.Default.Save();
                    break;
                case false:
                    Properties.LauncherSettings.Default.PortableMode = false;
                    Properties.LauncherSettings.Default.Save();
                    break;
            }

            RestartNeeded = true;

            UpdateDirectoryPathTextbox();
        }

        private void UpdateDirectoryPathTextbox()
        {
            if (Properties.LauncherSettings.Default.PortableMode)
            {
                StorageDirectoryTextBox.IsEnabled = false;
                StorageDirectoryTextBox.Text = "%PORTABLE%";
            }
            else
            {
                StorageDirectoryTextBox.IsEnabled = true;

                if (Properties.LauncherSettings.Default.FixedDirectory != string.Empty)
                {
                    StorageDirectoryTextBox.Text = Properties.LauncherSettings.Default.FixedDirectory;
                }
                else
                {
                    StorageDirectoryTextBox.Text = MainViewModel.Default.FilePaths.DefaultLocation;
                }
            }
        }

        private void BrowseForDirectory()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog()
            {
                InitialFolder = StorageDirectoryTextBox.Text
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Properties.LauncherSettings.Default.FixedDirectory = dialog.SelectedFolder;
                Properties.LauncherSettings.Default.Save();
            }
        }

        private void ResetDirectoryToDefault()
        {
            Properties.LauncherSettings.Default.FixedDirectory = string.Empty;
            Properties.LauncherSettings.Default.Save();
        }

        private void BrowseDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            RestartNeeded = true;
            BrowseForDirectory();
            UpdateDirectoryPathTextbox();
        }

        private void ResetDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            RestartNeeded = true;
            ResetDirectoryToDefault();
            UpdateDirectoryPathTextbox();
        }

        private void RestartLauncher()
        {
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            if (RestartNeeded) RestartLauncher();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (RestartNeeded) RestartLauncher();
        }
    }
}
