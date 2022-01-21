using BedrockLauncher.Classes;
using BedrockLauncher.Extensions;
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

namespace BedrockLauncher.Pages.Play
{
    public partial class PlayScreenPage : Page
    {



        public PlayScreenPage()
        {
            InitializeComponent();
        }

        private string GetLatestImage()
        {
            return Constants.Themes.First().Value;
        }
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {

                string packUri = string.Empty;
                string currentTheme = Properties.LauncherSettings.Default.CurrentTheme;

                bool isBugRock = Handlers.RuntimeHandler.IsBugRockOfTheWeek();
                if (isBugRock)
                {
                    BedrockLogo.Visibility = Visibility.Collapsed;
                    BugrockLogo.Visibility = Visibility.Visible;
                    BugrockOfTheWeekLogo.Visibility = Visibility.Visible;
                }
                else
                {
                    BedrockLogo.Visibility = Visibility.Visible;
                    BugrockLogo.Visibility = Visibility.Collapsed;
                    BugrockOfTheWeekLogo.Visibility = Visibility.Collapsed;
                }

                switch (currentTheme)
                {
                    case "LatestUpdate":
                        packUri = GetLatestImage();
                        break;
                    default:
                        if (Constants.Themes.ContainsKey(currentTheme)) packUri = Constants.Themes.Where(x => x.Key == currentTheme).FirstOrDefault().Value;
                        else packUri = Constants.Themes.Where(x => x.Key == "Original").FirstOrDefault().Value;
                        break;
                }

                var bmp = new BitmapImage(new Uri(packUri, UriKind.Absolute));
                ImageBrush.ImageSource = bmp;
            });
        }
        private void MainPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainViewModel.Default.PackageManager.isGameRunning) MainViewModel.Default.KillGame();
            else
            {
                var i = InstallationsList.SelectedItem as BLInstallation;
                bool KeepLauncherOpen = Properties.LauncherSettings.Default.KeepLauncherOpen;
                MainViewModel.Default.Play(ViewModels.MainViewModel.Default.Config.CurrentProfile, i, KeepLauncherOpen);
            }
        }
    }
}
