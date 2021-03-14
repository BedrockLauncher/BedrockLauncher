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
        private InstallLocationPage InstallLocationPage = new InstallLocationPage();
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
            // this is an example of bad code, if u know how to make it better, contact me
            if (MainFrame.Content.GetType().Name.ToString() == "WelcomePage") 
            {
                MainFrame.Navigate(licenseAgreementPage);
                BackBtn.IsEnabled = true;
                if (licenseAgreementPage.acceptRadioBtn.IsChecked == false) { NextBtn.IsEnabled = false; }
            }
            if (MainFrame.Content.GetType().Name.ToString() == "LicenseAgreementPage")
            {
                MainFrame.Navigate(InstallLocationPage);
                NextBtn.Content = "Install";
            }
            if (MainFrame.Content.GetType().Name.ToString() == "InstallLocationPage")
            {
                if (!Directory.Exists(InstallLocationPage.installPathTextBox.Text)) 
                {
                    try
                    {
                        Directory.CreateDirectory(InstallLocationPage.installPathTextBox.Text);
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show(err.ToString());
                    }
                }
                MainFrame.Navigate(InstallLocationPage);
                NextBtn.Content = "Finish";
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content.GetType().Name.ToString() == "LicenseAgreementPage")
            {
                MainFrame.Navigate(welcomePage);
                BackBtn.IsEnabled = false;
                NextBtn.IsEnabled = true;
            }
            if (MainFrame.Content.GetType().Name.ToString() == "InstallLocationPage")
            {
                MainFrame.Navigate(licenseAgreementPage);
                NextBtn.Content = "Next";
            }
        }
    }
}
