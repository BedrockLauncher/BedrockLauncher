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
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.FirstLaunch
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
            pageSwitcher.MoveToPage(4);
        }
    }
}
