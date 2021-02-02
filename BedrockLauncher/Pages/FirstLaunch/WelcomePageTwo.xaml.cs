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

namespace BedrockLauncher
{
    /// <summary>
    /// Логика взаимодействия для WelcomePageOne.xaml
    /// </summary>
    public partial class WelcomePageTwo : Page
    {
        public WelcomePage welcomePage = new WelcomePage();
        public WelcomePageOne pageOne = new WelcomePageOne();
        public WelcomePageTwo()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            welcomePage.WelcomePageFrame.Navigate(pageOne);
            ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Navigate(welcomePage);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Visibility = Visibility.Hidden;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            welcomePage.WelcomePageFrame.Content = null;
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Properties.Settings.Default.GithubPage);
        }
    }
}
