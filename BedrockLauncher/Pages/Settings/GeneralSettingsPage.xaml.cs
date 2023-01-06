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
using BedrockLauncher.Extensions;
using Microsoft.Win32;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Settings
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

        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            Update();
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Handlers.BackupHandler.BackupOriginalSaveData);
        }

        private void AdvancedSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            AdvancedOptionsWindow advancedOptionsWindow = new AdvancedOptionsWindow();
            advancedOptionsWindow.ShowDialog();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void ExternalLauncherPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.LauncherSettings.Default.Save();
        }

        private void BrowseLauncherButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                InitialDirectory = ExternalLauncherPathTextBox.Text
            };
            if (dialog.ShowDialog().Value)
            {
                Properties.LauncherSettings.Default.ExternalLauncherPath = dialog.FileName;
                Properties.LauncherSettings.Default.Save();
                ExternalLauncherPathTextBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
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
                string fileToUse = MainViewModel.Default.FilePaths.AddImageToIconCache(dialog.FileName);
                Properties.LauncherSettings.Default.ExternalLauncherIconPath = fileToUse;
                Properties.LauncherSettings.Default.Save();
            }
        }   

        private void ResetExternalLauncherIcon_Button_Click(object sender, RoutedEventArgs e)
        {
            Properties.LauncherSettings.Default.ExternalLauncherIconPath = string.Empty;
            Properties.LauncherSettings.Default.Save();
        }

        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            Properties.LauncherSettings.Default.Save();
        }

    }
}
