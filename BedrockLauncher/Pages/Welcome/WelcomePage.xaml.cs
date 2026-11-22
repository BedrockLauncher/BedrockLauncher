using BedrockLauncher.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BedrockLauncher.Pages.Welcome
{
    public partial class WelcomePage : Page
    {
        public WelcomePagesSwitcher pageSwitcher = new WelcomePagesSwitcher();

        public WelcomePage()
        {
            InitializeComponent();
        }

        private void Page_Initialized(object sender, EventArgs e) => pageSwitcher.Init(this);
    }

    public partial class WelcomePagesSwitcher
    {
        public static WelcomePage welcomePage;
        public static WelcomePageOne pageOne;
        public static WelcomePageTwo pageTwo;
        public static WelcomePageThree pageThree;
        public static WelcomePageFour pageFour;
        public static WelcomePageFive pageFive;

        public void Init(WelcomePage page) => welcomePage = page;

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
                    Page5();
                    break;
                case 6:
                    Page6(backup);
                    break;
            }

            void Page1() => welcomePage.WelcomePageFrame.Navigate(pageOne ??= new WelcomePageOne());

            void Page2() => welcomePage.WelcomePageFrame.Navigate(pageTwo ??= new WelcomePageTwo());

            void Page3() => welcomePage.WelcomePageFrame.Navigate(pageThree ??= new WelcomePageThree());

            void Page4()
            {
                MainDataModel.Default.LoadConfig();

                if (MainDataModel.Default.Config.profiles.Any())
                {
                    Properties.LauncherSettings.Default.CurrentProfileUUID = MainDataModel.Default.Config.profiles.FirstOrDefault().Key;
                    Properties.LauncherSettings.Default.Save();
                    MoveToPage(5);
                }
                else
                {
                    welcomePage.WelcomePageFrame.Navigate(pageFour ??= new WelcomePageFour());
                }
            }

            void Page5() => welcomePage.WelcomePageFrame.Navigate(pageFive ??= new WelcomePageFive());

            void Page6(bool _backup)
            {
                if (_backup)
                {
                    // BackupHandler removed
                }

                Task.Run(Program.OnApplicationRefresh);
                ViewModels.MainViewModel.Default.SetOverlayFrame(null, true);
                Properties.LauncherSettings.Default.IsFirstLaunch = false;
                Properties.LauncherSettings.Default.Save();
            }
        }
    }
}