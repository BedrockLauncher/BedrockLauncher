using BedrockLauncher.Classes;
using BedrockLauncher.Handlers;
using BedrockLauncher.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Threading;

namespace BedrockLauncher.Pages.Play.Home
{
    public partial class PlayScreenPage : Page
    {
        private readonly DispatcherTimer installStateRefreshTimer = new DispatcherTimer();
        private int installStateRefreshTicks = 0;

        public PlayScreenPage()
        {
            InitializeComponent();
            InstallationsList.SelectionChanged += CheckVersionAvailability;
            MainDataModel.Default.Versions.CollectionChanged += Versions_CollectionChanged;
            ((INotifyPropertyChanged)MainDataModel.Default.ProgressBarState).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainDataModel.Default.ProgressBarState.AllowPlaying))
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CheckVersionAvailability(s, e);
                    });
            };

            Loaded += PlayScreenPage_Loaded;
            installStateRefreshTimer.Interval = TimeSpan.FromMilliseconds(500);
            installStateRefreshTimer.Tick += InstallStateRefreshTimer_Tick;
        }

        private void PlayScreenPage_Loaded(object sender, RoutedEventArgs e)
        {
            installStateRefreshTicks = 0;
            installStateRefreshTimer.Start();
            RefreshPlayButtonSoon();
        }

        private void Versions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshPlayButtonSoon();
        }

        private void InstallStateRefreshTimer_Tick(object sender, EventArgs e)
        {
            installStateRefreshTicks++;
            CheckVersionAvailability(sender, e);

            if (installStateRefreshTicks >= 20)
                installStateRefreshTimer.Stop();
        }

        private void RefreshPlayButtonSoon()
        {
            Dispatcher.BeginInvoke(new Action(() => CheckVersionAvailability(this, EventArgs.Empty)), DispatcherPriority.Background);
        }

        private void CheckVersionAvailability(object _, EventArgs __)
        {
            BLInstallation selectedInstallation = GetSelectedPlayableInstallation();

            if (MainDataModel.Default.PackageManager.isGameRunning)
            {
                RestorePlayButtonText(true);
            }
            else if (selectedInstallation is not null && selectedInstallation.Version is null)
            {
                ShowNoPlayableInstallation();
            }
            else if (selectedInstallation?.IsPlayableInstalled != true)
            {
                ShowNoPlayableInstallation();
            }
            else
            {
                RestorePlayButtonText();
            }
        }

        private BLInstallation GetSelectedPlayableInstallation()
        {
            BLInstallation selectedInstallation = InstallationsList.SelectedItem as BLInstallation ?? MainDataModel.Default.Config.GetSelectedOrFirstPlayableInstallation();
            if (selectedInstallation?.IsPlayableInstalled == true && FilterSortingHandler.Filter_PlayInstallationList(selectedInstallation))
            {
                SelectInstallationInPlayList(selectedInstallation);
                return selectedInstallation;
            }

            BLInstallation playableInstallation = MainDataModel.Default.Config.EnsurePlayableInstallationSelected();

            if (playableInstallation != null)
                SelectInstallationInPlayList(playableInstallation);

            return playableInstallation ?? selectedInstallation;
        }

        private void SelectInstallationInPlayList(BLInstallation installation)
        {
            if (installation == null)
                return;

            MainDataModel.Default.Config.CurrentInstallationUUID = installation.InstallationUUID;
            InstallationsList.SelectedItem = installation;
            InstallationsList.SelectedValue = installation.InstallationUUID;
        }

        private void SetPlayButtonInstalledState(BLInstallation selectedInstallation)
        {
            if (selectedInstallation?.Version != null)
            {
                RestorePlayButtonText(forceEnabled: true);
            }
            else
            {
                ShowNoPlayableInstallation();
            }
        }

        private void ShowNoPlayableInstallation()
        {
            BindingOperations.ClearBinding(MainPlayButton, Button.IsEnabledProperty);
            MainPlayButton.IsEnabled = true;
            MainPlayButton.IsHitTestVisible = false;
            MainPlayButton.Focusable = false;
            MainPlayButton.Margin = new Thickness(286, 0, 286, 0);
            MainPlayButton.Style = (Style)FindResource("BigUnavailableButton");

            BindingOperations.ClearBinding(PlayButtonText, TextBlock.TextProperty);
            PlayButtonText.Text = Application.Current.FindResource("InstallationsPage_PlayButton").ToString();
            PlayButtonText.FontSize = 26;
            PlayButtonText.Margin = new Thickness(0, -1, 0, 0);
            PlayButtonText.TextTrimming = TextTrimming.None;
            PlayButtonText.Foreground = new SolidColorBrush(Color.FromRgb(242, 242, 242));
            PlayButtonText.Effect = null;
        }

        private void RestorePlayButtonText(bool forceEnabled = false)
        {
            MainPlayButton.Style = (Style)FindResource("BigGreenButton");
            MainPlayButton.Margin = new Thickness(286, -8, 286, 0);
            MainPlayButton.IsHitTestVisible = true;
            MainPlayButton.Focusable = true;

            if (forceEnabled)
            {
                BindingOperations.ClearBinding(MainPlayButton, Button.IsEnabledProperty);
                MainPlayButton.IsEnabled = true;
            }
            else
            {
                BindingOperations.SetBinding(MainPlayButton, Button.IsEnabledProperty, new Binding("ProgressBarState.AllowPlaying")
                {
                    Source = MainDataModel.Default,
                    Mode = BindingMode.OneWay
                });
            }

            BindingOperations.SetBinding(PlayButtonText, TextBlock.TextProperty, new Binding("ProgressBarState.PlayButtonString")
            {
                Source = MainDataModel.Default,
                Mode = BindingMode.OneWay
            });
            PlayButtonText.ClearValue(TextBlock.ForegroundProperty);
            PlayButtonText.ClearValue(TextBlock.FontSizeProperty);
            PlayButtonText.ClearValue(TextBlock.MarginProperty);
            PlayButtonText.ClearValue(TextBlock.TextTrimmingProperty);
            PlayButtonText.ClearValue(TextBlock.EffectProperty);
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
                var i = GetSelectedPlayableInstallation();
                if (i?.IsPlayableInstalled != true)
                    return;

                bool KeepLauncherOpen = Properties.LauncherSettings.Default.KeepLauncherOpen;
                MainDataModel.Default.Play(ViewModels.MainDataModel.Default.Config.CurrentProfile, i, KeepLauncherOpen, false);
            }
        }
    }
}
