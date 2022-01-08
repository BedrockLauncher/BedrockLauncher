using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using PostSharp.Patterns.Model;
using BedrockLauncher.Enums;

namespace BedrockLauncher.Events
{
    [NotifyPropertyChanged]
    public class LauncherState
    {
        private static MainWindow MainThread
        {
            get
            {
                return App.Current.Dispatcher.Invoke(() =>
                {
                    return (MainWindow)App.Current.MainWindow;
                });
            }
        }


        private bool _ShowProgressBar = false;
        private LauncherStateChange _currentState = LauncherStateChange.None;
        public const long DeploymentMaximum = 100;

        public bool AllowCancel { get; set; }
        public bool IsGameRunning { get; set; }
        public ICommand CancelCommand { get; set; }
        public string DeploymentPackageName { get; set; }
        public long ProgressBar_CurrentProgress { get; set; }
        public long ProgressBar_TotalProgress { get; set; }
        public LauncherStateChange ProgressBar_CurrentState
        {
            get
            {
                return _currentState;
            }
            set
            {
                bool IsAdvancedDetail = value == LauncherStateChange.isRegisteringPackage || value == LauncherStateChange.isRemovingPackage;
                if (!Properties.LauncherSettings.Default.ShowAdvancedInstallDetails && IsAdvancedDetail) return;
                else _currentState = value;
            }
        }
        public bool ProgressBar_IsIndeterminate
        {
            get
            {
                Depends.On(ProgressBar_CurrentState);
                bool isAdvanced = Properties.LauncherSettings.Default.ShowAdvancedInstallDetails;
                return (ProgressBar_CurrentState == LauncherStateChange.isInitializing || ProgressBar_CurrentState == LauncherStateChange.isUninstalling || ProgressBar_CurrentState == LauncherStateChange.isLaunching || (!isAdvanced ? (ProgressBar_CurrentState == LauncherStateChange.isRemovingPackage || ProgressBar_CurrentState == LauncherStateChange.isRegisteringPackage) : false));
            }
        }
        public object ProgressBar_Content
        {
            get
            {
                Depends.On(ProgressBar_CurrentState);
                switch (ProgressBar_CurrentState)
                {
                    case LauncherStateChange.isInitializing:
                        return Application.Current.TryFindResource("ProgressBar_Downloading");
                    case LauncherStateChange.isDownloading:
                        return Application.Current.TryFindResource("ProgressBar_Downloading");
                    case LauncherStateChange.isExtracting:
                        return Application.Current.TryFindResource("ProgressBar_Extracting");
                    case LauncherStateChange.isRegisteringPackage:
                        return Application.Current.TryFindResource("ProgressBar_RegisteringPackage");
                    case LauncherStateChange.isRemovingPackage:
                        return Application.Current.TryFindResource("ProgressBar_RemovingPackage");
                    case LauncherStateChange.isUninstalling:
                        return Application.Current.TryFindResource("ProgressBar_Uninstalling");
                    case LauncherStateChange.isLaunching:
                        return Application.Current.TryFindResource("ProgressBar_Launching");
                    case LauncherStateChange.isBackingUp:
                        return Application.Current.TryFindResource("ProgressBar_BackingUp");
                    default:
                        return null;
                }
            }
        }
        public bool ProgressBar_Show
        {
            get
            {
                return _ShowProgressBar;
            }
            set
            {
                _ShowProgressBar = value;
                if (_ShowProgressBar) ProgressBarShowAnim();
                else ProgressBarHideAnim();
            }
        }
        public string ProgressBar_DisplayStatus
        {
            get
            {
                Depends.On(ProgressBar_CurrentState);
                Depends.On(ProgressBar_CurrentProgress);
                Depends.On(ProgressBar_TotalProgress);
                Depends.On(DeploymentPackageName);

                switch (ProgressBar_CurrentState)
                {
                    case LauncherStateChange.isInitializing:
                        return "";
                    case LauncherStateChange.isDownloading:
                        return DownloadStatus();
                    case LauncherStateChange.isExtracting:
                        return ExtractingStatus();
                    case LauncherStateChange.isRegisteringPackage:
                        return DeploymentStatus();
                    case LauncherStateChange.isRemovingPackage:
                        return DeploymentStatus();
                    case LauncherStateChange.isUninstalling:
                        return "";
                    case LauncherStateChange.isLaunching:
                        return "";
                    case LauncherStateChange.isBackingUp:
                        return BackupStatus();
                    case LauncherStateChange.None:
                        return "";
                    default:
                        return "";
                }

                string BackupStatus()
                {
                    return string.Format("{0} / {1}", ProgressBar_CurrentProgress, ProgressBar_TotalProgress);
                }

                string DeploymentStatus()
                {
                    return ProgressBar_CurrentProgress + "%" + string.Format(" [ {0} ]", DeploymentPackageName);
                }

                string DownloadStatus()
                {
                    return (Math.Round((double)ProgressBar_CurrentProgress / 1024 / 1024, 2)).ToString() + " MB / " + (Math.Round((double)ProgressBar_TotalProgress / 1024 / 1024, 2)).ToString() + " MB";

                }

                string ExtractingStatus()
                {
                    long percent = 0;
                    if (ProgressBar_TotalProgress != 0) percent = (100 * ProgressBar_CurrentProgress) / ProgressBar_TotalProgress;
                    return percent + "%";
                }
            }
        }
        public string PlayButtonString
        {
            get
            {
                Depends.On(IsGameRunning);
                if (IsGameRunning) return App.Current.FindResource("GameTab_PlayButton_Kill_Text").ToString();
                else return App.Current.FindResource("GameTab_PlayButton_Text").ToString();
            }
        }
        public bool AllowEditing
        {
            get
            {
                Depends.On(AllowPlaying);
                Depends.On(IsGameRunning);
                Depends.On(ProgressBar_Show);
                return AllowPlaying && !IsGameRunning && !ProgressBar_Show;
            }
        }
        public bool AllowPlaying
        {
            get
            {
                Depends.On(ProgressBar_CurrentState);
                Depends.On(ProgressBar_Show);
                return ProgressBar_CurrentState == LauncherStateChange.None && !ProgressBar_Show;
            }
        }


        private async void ProgressBarShowAnim()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MainThread.BedrockEditionButton.progressSizeHack.Visibility = Visibility.Visible;
                MainThread.ProgressBarGrid.Visibility = Visibility.Visible;
                MainThread.ProgressBarText.Visibility = Visibility.Hidden;
                MainThread.progressbarcontent.Visibility = Visibility.Hidden;

                Storyboard storyboard = new Storyboard();
                DoubleAnimation animation = new DoubleAnimation
                {
                    From = 0,
                    To = 72,
                    Duration = new Duration(TimeSpan.FromMilliseconds(350))
                };
                storyboard.Children.Add(animation);
                Storyboard.SetTargetProperty(animation, new System.Windows.PropertyPath(ProgressBar.HeightProperty));
                Storyboard.SetTarget(animation, MainThread.ProgressBarGrid);
                storyboard.Completed += new EventHandler(ShowProgressBarContent);
                storyboard.Begin();
            });
        }
        private async void ProgressBarHideAnim()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MainThread.BedrockEditionButton.progressSizeHack.Visibility = Visibility.Hidden;
                MainThread.ProgressBarGrid.Visibility = Visibility.Visible;
                MainThread.ProgressBarText.Visibility = Visibility.Hidden;
                MainThread.progressbarcontent.Visibility = Visibility.Hidden;

                Storyboard storyboard = new Storyboard();
                DoubleAnimation animation = new DoubleAnimation
                {
                    From = 72,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(350))
                };
                storyboard.Children.Add(animation);
                Storyboard.SetTargetProperty(animation, new System.Windows.PropertyPath(ProgressBar.HeightProperty));
                Storyboard.SetTarget(animation, MainThread.ProgressBarGrid);
                storyboard.Completed += new EventHandler(HideProgressBarContent);
                storyboard.Begin();
            });
        }
        private void HideProgressBarContent(object sender, EventArgs e)
        {
            MainThread.BedrockEditionButton.progressSizeHack.Visibility = Visibility.Hidden;
            MainThread.ProgressBarGrid.Visibility = Visibility.Collapsed;
            MainThread.ProgressBarText.Visibility = Visibility.Hidden;
            MainThread.progressbarcontent.Visibility = Visibility.Hidden;
        }
        private void ShowProgressBarContent(object sender, EventArgs e)
        {
            MainThread.BedrockEditionButton.progressSizeHack.Visibility = Visibility.Visible;
            MainThread.ProgressBarGrid.Visibility = Visibility.Visible;
            MainThread.ProgressBarText.Visibility = Visibility.Visible;
            MainThread.progressbarcontent.Visibility = Visibility.Visible;
        }
    }
}
