using BedrockLauncher.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace BedrockLauncher.Pages.FirstLaunch
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
            Properties.LauncherSettings.Default.IsFirstLaunch = false;
            Properties.LauncherSettings.Default.Save();
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
        public static WelcomePageFour pageFour;
        public void Init(WelcomePage page)
        {
            welcomePage = page;
        }
        public void MoveToPage(byte page, bool backup = false)
        {
            switch (page)
            {
                case 1:
                    Page1();
                    break;
                case 2:
                    Page2();
                    break;
                case 3:
                    Page3();
                    break;
                case 4:
                    Page4();
                    break;
                case 5:
                    Page5(backup);
                    break;
                case 6:
                    Page6();
                    break;
            }

            void Page1()
            {
                if (pageOne == null)
                {
                    pageOne = new WelcomePageOne();
                    welcomePage.WelcomePageFrame.Navigate(pageOne);
                }
                else { welcomePage.WelcomePageFrame.Navigate(pageOne); }
            }

            void Page2()
            {
                if (pageTwo == null)
                {
                    pageTwo = new WelcomePageTwo();
                    welcomePage.WelcomePageFrame.Navigate(pageTwo);
                }
                else { welcomePage.WelcomePageFrame.Navigate(pageTwo); }
            }

            void Page3()
            {
                if (ConfigManager.ProfileList.profiles.Count() != 0)
                {
                    Properties.LauncherSettings.Default.CurrentProfile = ConfigManager.ProfileList.profiles.FirstOrDefault().Key;
                    Properties.LauncherSettings.Default.Save();
                    MoveToPage(4);
                }
                else
                {
                    if (pageThree == null)
                    {
                        pageThree = new WelcomePageThree();
                        welcomePage.WelcomePageFrame.Navigate(pageThree);
                    }
                    else { welcomePage.WelcomePageFrame.Navigate(pageThree); }
                }
            }

            void Page4()
            {
                if (pageFour == null)
                {
                    pageFour = new WelcomePageFour();
                    welcomePage.WelcomePageFrame.Navigate(pageFour);
                }
                else { welcomePage.WelcomePageFrame.Navigate(pageFour); }
            }

            void Page5(bool _backup)
            {
                if (_backup) ConfigManager.GameManager.BackupGameData();
                ConfigManager.MainThread.SetOverlayFrame(null);
            }

            void Page6()
            {

            }
        }
    }
}
