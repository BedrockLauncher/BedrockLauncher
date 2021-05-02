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
using BedrockLauncher.Methods;

namespace BedrockLauncher.Pages
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
            portableModeCheckBox.IsChecked = Properties.Settings.Default.PortableMode;
            UpdateDirectoryPathTextbox();
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

            RestartNeeded = true;

            UpdateDirectoryPathTextbox();

            ConfigManager.Init();
        }

        private void UpdateDirectoryPathTextbox()
        {
            if (Properties.Settings.Default.PortableMode)
            {
                StorageDirectoryTextBox.IsEnabled = false;
                StorageDirectoryTextBox.Text = "%PORTABLE%";
            }
            else
            {
                StorageDirectoryTextBox.IsEnabled = true;

                if (Properties.Settings.Default.FixedDirectory != string.Empty)
                {
                    StorageDirectoryTextBox.Text = Properties.Settings.Default.FixedDirectory;
                }
                else
                {
                    StorageDirectoryTextBox.Text = BedrockLauncher.Methods.Filepaths.DefaultLocation;
                }
            }
        }

        private void BrowseForDirectory()
        {
            Controls.FolderSelectDialog dialog = new Controls.FolderSelectDialog()
            {
                InitialDirectory = StorageDirectoryTextBox.Text
            };
            if (dialog.Show(new WindowInteropHelper(this).Handle))
            {
                Properties.Settings.Default.FixedDirectory = dialog.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void ResetDirectoryToDefault()
        {
            Properties.Settings.Default.FixedDirectory = string.Empty;
            Properties.Settings.Default.Save();
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
