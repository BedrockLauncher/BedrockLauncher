using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.ViewModels;
using FolderBrowserEx;

namespace BedrockLauncher.Pages.Welcome
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
            pageSwitcher.MoveToPage(3);
        }

        private void UpdateDirectoryPathTextbox()
        {
            if (Properties.LauncherSettings.Default.PortableMode)
            {
                FixedRadioButton.IsChecked = false;
                PortableRadioButton.IsChecked = true;
            }
            else
            {
                FixedRadioButton.IsChecked = true;
                PortableRadioButton.IsChecked = false;
            }
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
