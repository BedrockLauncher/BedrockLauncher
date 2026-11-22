using System;
using System.Windows.Controls;

namespace BedrockLauncher.Pages.Welcome
{
    /// <summary>
    /// Interaction logic for WelcomePageFour.xaml
    /// </summary>
    public partial class WelcomePageFour : Page
    {
        public WelcomePagesSwitcher pageSwitcher = new WelcomePagesSwitcher();
        public WelcomePageFour()
        {
            InitializeComponent();
        }

        private void ProfileControl_GoBack(object sender, EventArgs e) => pageSwitcher.MoveToPage(3);

        private void ProfileControl_Confirm(object sender, EventArgs e) => pageSwitcher.MoveToPage(5);
    }
}
