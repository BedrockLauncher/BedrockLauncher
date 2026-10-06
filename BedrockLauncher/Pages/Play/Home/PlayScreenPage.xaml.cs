using BedrockLauncher.Classes;
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
using System.Diagnostics;
using System.IO;

namespace BedrockLauncher.Pages.Play.Home
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

        private string GetCustomImage(string result)
        {
            DirectoryInfo directoryInfo = Directory.CreateDirectory(MainDataModel.Default.FilePaths.ThemesFolder);
            foreach (var file in directoryInfo.GetFiles())
            {
                if (file.Name == result) return file.FullName;
            }
            return Constants.Themes.Where(x => x.Key == "Original").FirstOrDefault().Value;
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


                if (currentTheme.StartsWith(Constants.ThemesCustomPrefix))
                {
                    packUri = GetCustomImage(currentTheme.Remove(0, Constants.ThemesCustomPrefix.Length));
                }
                else
                {
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
                }



                try
                {
                    var bmp = new BitmapImage(new Uri(packUri, UriKind.Absolute));
                    ImageBrush.ImageSource = bmp;
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }

            });
        }
        private void MainPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainDataModel.Default.PackageManager.isGameRunning) MainDataModel.Default.KillGame();
            else
            {
                var i = InstallationsList.SelectedItem as BLInstallation;
                bool KeepLauncherOpen = Properties.LauncherSettings.Default.KeepLauncherOpen;
                MainDataModel.Default.Play(ViewModels.MainDataModel.Default.Config.CurrentProfile, i, KeepLauncherOpen, false);
            }
        }
    }
}