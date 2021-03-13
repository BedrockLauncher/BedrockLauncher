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

namespace Installer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WelcomePage welcomePage = new WelcomePage();
        private LicenseAgreementPage licenseAgreementPage = new LicenseAgreementPage();
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
            if (MainFrame.Content.GetType().Name.ToString() == "WelcomePage") 
            {
                MainFrame.Navigate(licenseAgreementPage);
                BackBtn.IsEnabled = true;
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content.GetType().Name.ToString() == "LicenseAgreementPage")
            {
                MainFrame.Navigate(welcomePage);
                BackBtn.IsEnabled = false;
            }
        }
    }
}
