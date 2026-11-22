using BedrockLauncher.ViewModels;
using FolderBrowserEx;
using System.Windows;
using System.Windows.Controls;

namespace BedrockLauncher.Pages.Welcome
{
    /// <summary>
    /// Interaction logic for WelcomePageThree.xaml
    /// </summary>
    public partial class WelcomePageThree : Page
    {
        public WelcomePagesSwitcher pageSwitcher = new WelcomePagesSwitcher();
        private bool isInit = false;

        public WelcomePageThree()
        {
            InitializeComponent();
            UpdateDirectoryPathTextbox();
            isInit = true;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => pageSwitcher.MoveToPage(2);

        private void NextButton_Click(object sender, RoutedEventArgs e) => pageSwitcher.MoveToPage(4);

        private void UpdateDirectoryPathTextbox()
        {
            if (Properties.LauncherSettings.Default.PortableMode)
            {
                FixedRadioButton.IsChecked = false;
                PortableRadioButton.IsChecked = true;
                StorageDirectoryTextBox.IsEnabled = false;
                StorageDirectoryTextBox.Text = "%PORTABLE%";
                PathBox.IsEnabled = false;
            }
            else
            {
                var fixedDir = Properties.LauncherSettings.Default.FixedDirectory;
                var defaultDir = MainDataModel.Default.FilePaths.DefaultLocation;

                FixedRadioButton.IsChecked = true;
                PortableRadioButton.IsChecked = false;
                StorageDirectoryTextBox.IsEnabled = true;
                PathBox.IsEnabled = true;

                StorageDirectoryTextBox.Text = string.IsNullOrEmpty(fixedDir) ? defaultDir : fixedDir;

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
