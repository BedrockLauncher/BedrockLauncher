using BedrockLauncher.Methods;
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

namespace BedrockLauncher.Pages.FirstLaunch
{
    /// <summary>
    /// Логика взаимодействия для WelcomePageOne.xaml
    /// </summary>
    public partial class WelcomePageTwo : Page
    {
        public WelcomePagesSwitcher pageSwitcher = new WelcomePagesSwitcher();
        private bool isInit = false;

        public WelcomePageTwo()
        {
            InitializeComponent();
            UpdateDirectoryPathTextbox();
            isInit = true;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            pageSwitcher.MoveToPage(1);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.Init();
            ConfigManager.OnConfigStateChanged(this, Events.ConfigStateArgs.Empty);
            pageSwitcher.MoveToPage(3);
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
                PathBox.IsEnabled = true;
                StorageDirectoryTextBox.IsEnabled = true;

                if (Properties.LauncherSettings.Default.FixedDirectory != string.Empty)
                {
                    StorageDirectoryTextBox.Text = Properties.LauncherSettings.Default.FixedDirectory;
                }
                else
                {
                    StorageDirectoryTextBox.Text = BedrockLauncher.Methods.FilepathManager.DefaultLocation;
                }
            }
        }

        private void BrowseForDirectory()
        {
            Controls.FolderSelectDialog dialog = new Controls.FolderSelectDialog()
            {
                InitialDirectory = StorageDirectoryTextBox.Text
            };
            if (dialog.Show(new WindowInteropHelper(Application.Current.MainWindow).Handle))
            {
                Properties.LauncherSettings.Default.FixedDirectory = dialog.FileName;
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
            BrowseForDirectory();
            UpdateDirectoryPathTextbox();
        }

        private void ResetDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            ResetDirectoryToDefault();
            UpdateDirectoryPathTextbox();
        }

        private void FixedRadioButton_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (isInit)
            {
                Properties.LauncherSettings.Default.PortableMode = PortableRadioButton.IsChecked.Value;
                Properties.LauncherSettings.Default.Save();
                UpdateDirectoryPathTextbox();
            }

        }
    }
}
