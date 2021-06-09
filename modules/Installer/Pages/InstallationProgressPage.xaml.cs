using System;
using System.IO;
using System.Windows.Controls;
using System.Windows;


namespace BL_Setup.Pages
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
            launchOnExitCheckbox.IsChecked = BL_Setup.MainWindow.Installer.RunOnExit;
            IsMainInit = true;
        }

        private void IconCheckBoxes_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (IsInit && IsMainInit)
            {
                BL_Setup.MainWindow.Installer.RunOnExit = launchOnExitCheckbox.IsChecked.Value;
            }
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            IsInit = true;
        }
    }
}
