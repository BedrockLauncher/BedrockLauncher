using System.Windows;
using System.Windows.Controls;

namespace BedrockLauncher.Pages.Welcome
{
    /// <summary>
    /// Interaction logic for WelcomePageTwo.xaml
    /// </summary>
    public partial class WelcomePageTwo : Page
    {
        public WelcomePagesSwitcher pageSwitcher = new WelcomePagesSwitcher();
        public WelcomePageTwo()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => pageSwitcher.MoveToPage(1);

        private void NextButton_Click(object sender, RoutedEventArgs e) => pageSwitcher.MoveToPage(3);
    }
}
