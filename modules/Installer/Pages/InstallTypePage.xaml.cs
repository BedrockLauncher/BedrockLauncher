using System;
using System.IO;
using System.Windows.Controls;
using System.Windows;


namespace BL_Setup.Pages
{
    /// <summary>
    /// Логика взаимодействия для WelcomePage.xaml
    /// </summary>
    public partial class InstallTypePage : Page
    {
        private bool IsInit = false;
        private bool IsFullInit = false;
        public InstallTypePage()
        {
            InitializeComponent();
        }

        private void RadioButton_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (IsInit && IsFullInit)
            {
                BL_Setup.MainWindow.Installer.IsBeta = BetaRadioButton.IsChecked.Value;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            BetaRadioButton.IsChecked = BL_Setup.MainWindow.Installer.IsBeta;
            IsFullInit = true;
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            IsInit = true;
        }
    }
}
