using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace BedrockLauncher
{
    /// <summary>
    /// Логика взаимодействия для WelcomePage.xaml
    /// </summary>
    public partial class WelcomePage : Page
    {
        public WelcomePagesSwitcher pageSwitcher = new WelcomePagesSwitcher();
        public WelcomePage()
        {
            InitializeComponent();
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            pageSwitcher.Init(this);
        }
    }

    public partial class WelcomePagesSwitcher
    {
        public static WelcomePage welcomePage;
        public static WelcomePageOne pageOne;
        public static WelcomePageTwo pageTwo;
        public static WelcomePageThree pageThree;
        public void Init(WelcomePage page)
        {
            welcomePage = page;
        }
        public void MoveToPage(byte page)
        {
            switch (page)
            {
                case 1:
                    if (pageOne == null)
                    {
                        pageOne = new WelcomePageOne();
                        welcomePage.WelcomePageFrame.Navigate(pageOne);
                    } else { welcomePage.WelcomePageFrame.Navigate(pageOne); }
                    break;
                case 2:
                    if (pageTwo == null)
                    {
                        pageTwo = new WelcomePageTwo();
                        welcomePage.WelcomePageFrame.Navigate(pageTwo);
                    }
                    else { welcomePage.WelcomePageFrame.Navigate(pageTwo); }
                    break;
                case 3:
                    if (pageThree == null)
                    {
                        pageThree = new WelcomePageThree();
                        welcomePage.WelcomePageFrame.Navigate(pageThree);
                    }
                    else { welcomePage.WelcomePageFrame.Navigate(pageThree); }
                    break;
            }
        }
    }
}
