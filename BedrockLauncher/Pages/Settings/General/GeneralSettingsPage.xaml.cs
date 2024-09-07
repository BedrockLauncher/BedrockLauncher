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
using Microsoft.Win32;
using BedrockLauncher.ViewModels;
using FolderBrowserEx;
using System.Diagnostics;
using System.IO;

namespace BedrockLauncher.Pages.Settings.General
{
    /// <summary>
    /// Логика взаимодействия для GeneralSettingsPage.xaml
    /// </summary>
    public partial class GeneralSettingsPage : Page
    {


        private bool TEMP_PortableModeState;
        private string TEMP_FixedDirectoryState = string.Empty;

        public GeneralSettingsPage()
        {
            InitializeComponent();
        }

        private void Page_Initialized(object sender, EventArgs e)
        {

        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Handlers.BackupHandler.BackupReleaseSaveData);
        }

        private void BackupPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Handlers.BackupHandler.BackupPreviewSaveData);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TEMP_PortableModeState = Properties.LauncherSettings.Default.PortableMode;
            TEMP_FixedDirectoryState = Properties.LauncherSettings.Default.FixedDirectory;

            portableModeCheckBox.IsChecked = TEMP_PortableModeState;

            UpdateDirectoryPathTextbox();
        }

        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            Properties.LauncherSettings.Default.Save();
        }

        private void useFixedInstallLocation_Click(object sender, RoutedEventArgs e)
        {
            // get and save value of checkbox
            switch (portableModeCheckBox.IsChecked)
            {
                case true:
                    TEMP_PortableModeState = true;
                    break;
                case false:
                    TEMP_PortableModeState = false;
                    break;
            }

            UpdateDirectoryPathTextbox();
        }

        private void UpdateDirectoryPathTextbox()
        {
            if (Properties.LauncherSettings.Default.PortableMode)
            {
                StorageDirectoryTextBox.IsEnabled = false;
                StorageDirectoryTextBox.Text = "%PORTABLE%";
                PathBox.IsEnabled = false;
            }
            else
            {
                StorageDirectoryTextBox.IsEnabled = true;
                PathBox.IsEnabled = true;

                if (TEMP_FixedDirectoryState != string.Empty)
                {
                    StorageDirectoryTextBox.Text = TEMP_FixedDirectoryState;
                }
                else
                {
                    StorageDirectoryTextBox.Text = MainDataModel.Default.FilePaths.DefaultLocation;
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
                TEMP_FixedDirectoryState = dialog.SelectedFolder;
            }
        }

        private void ResetDirectoryToDefault()
        {
            TEMP_FixedDirectoryState = string.Empty;
        }

        private void BrowseDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            BrowseForDirectory();
            UpdateDirectoryPathTextbox();
        }

        private void ResetDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            ResetDirectoryToDefault();
            UpdateDirectoryPathTextbox();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.LauncherSettings.Default.PortableMode = TEMP_PortableModeState;
            Properties.LauncherSettings.Default.FixedDirectory = TEMP_FixedDirectoryState;
            Properties.LauncherSettings.Default.Save();

            string currentDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string ParentDir = Directory.GetParent(currentDir).FullName;
            string path = System.IO.Path.Combine(ParentDir, "StartBedrockLauncher.exe");
            StartProcess(path);
            Trace.WriteLine(path);
            void StartProcess(string path)
            {
                var startInfo = new ProcessStartInfo(path)
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
        }
    }
}
