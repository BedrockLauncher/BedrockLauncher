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

namespace BedrockLauncher.Pages.FirstLaunch
{
    /// <summary>
    /// Логика взаимодействия для WelcomePageOne.xaml
    /// </summary>
    public partial class WelcomePageTwo : Page
    {
        public WelcomePagesSwitcher pageSwitcher = new WelcomePagesSwitcher();
        public WelcomePageTwo()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            pageSwitcher.MoveToPage(1);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            pageSwitcher.MoveToPage(3);
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Properties.Settings.Default.GithubPage);
        }
    }
}
