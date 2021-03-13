using System;
using System.IO;
using System.Windows.Controls;


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
            //this.LicenseText.Content = File.ReadAllText("../LICENSE.txt");
        }
    }
}
