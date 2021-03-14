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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace Installer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WelcomePage welcomePage = new WelcomePage();
        private LicenseAgreementPage licenseAgreementPage = new LicenseAgreementPage();
        private InstallLocationPage installLocationPage = new InstallLocationPage();
        private InstallationProgressPage installationProgressPage = new InstallationProgressPage();
        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) { this.DragMove(); }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            // if u know how to make it better, contact me
            switch (MainFrame.Content.GetType().Name.ToString())
            {
                case "WelcomePage":
                    MainFrame.Navigate(licenseAgreementPage);
                    break;
                case "LicenseAgreementPage":
                    MainFrame.Navigate(installLocationPage);
                    break;
                case "InstallLocationPage":
                    LauncherInstaller launcherInstaller = new LauncherInstaller(installLocationPage.installPathTextBox.Text, installationProgressPage);
                    break;
                default:
                    break;
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (MainFrame.Content.GetType().Name.ToString())
            {
                case "LicenseAgreementPage":
                    MainFrame.Navigate(welcomePage);
                    break;
                case "InstallLocationPage":
                    MainFrame.Navigate(licenseAgreementPage);
                    break;
                default:
                    break;
            }
        }
    }
}
