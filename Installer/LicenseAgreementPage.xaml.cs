using System;
using System.Windows.Controls;
using System.Windows;


namespace Installer
{
    /// <summary>
    /// Логика взаимодействия для WelcomePage.xaml
    /// </summary>
    public partial class LicenseAgreementPage : Page
    {
        public LicenseAgreementPage()
        {
            InitializeComponent();
            this.LicenseText.Text = Properties.Resources.LICENSE;
        }

        private void RadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).NextBtn.IsEnabled = true;
        }

        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).NextBtn.IsEnabled = false;
        }
    }
}
