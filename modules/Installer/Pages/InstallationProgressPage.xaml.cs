using System;
using System.IO;
using System.Windows.Controls;
using System.Windows;


namespace BedrockLauncher.Installer.Pages
{
    /// <summary>
    /// Логика взаимодействия для WelcomePage.xaml
    /// </summary>
    public partial class InstallationProgressPage : Page
    {
        private bool IsInit = false;
        private bool IsMainInit = false;

        public InstallationProgressPage()
        {
            InitializeComponent();
            launchOnExitCheckbox.IsChecked = BedrockLauncher.Installer.MainWindow.Installer.RunOnExit;
            IsMainInit = true;
        }

        private void IconCheckBoxes_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (IsInit && IsMainInit)
            {
                BedrockLauncher.Installer.MainWindow.Installer.RunOnExit = launchOnExitCheckbox.IsChecked.Value;
            }
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            IsInit = true;
        }
    }
}
