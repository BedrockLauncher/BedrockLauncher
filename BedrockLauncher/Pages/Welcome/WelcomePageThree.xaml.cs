using System;
using System.Windows.Controls;

namespace BedrockLauncher.Pages.Welcome
{
    /// <summary>
    /// Логика взаимодействия для WelcomePageOne.xaml
    /// </summary>
    public partial class WelcomePageThree : Page
    {
        public WelcomePagesSwitcher pageSwitcher = new WelcomePagesSwitcher();
        public WelcomePageThree()
        {
            InitializeComponent();
        }

        private void ProfileControl_GoBack(object sender, EventArgs e)
        {
            pageSwitcher.MoveToPage(2);
        }

        private void ProfileControl_Confirm(object sender, EventArgs e)
        {
            pageSwitcher.MoveToPage(5);
        }
    }
}
