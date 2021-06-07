using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Events;
using BedrockLauncher.Classes;
using System.Windows;
using System.Windows.Input;
using BedrockLauncher.Methods;
using System.Windows.Controls;
using BedrockLauncher.Pages;
using BedrockLauncher.Pages.Community;
using BedrockLauncher.Pages.Settings;
using BedrockLauncher.Pages.News;
using BedrockLauncher.Pages.Play;
using System.Windows.Data;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.Pages.FirstLaunch;
using System.Windows.Media.Animation;
using BL_Core.Components;
using BL_Core.Pages.Common;
using BedrockLauncher.Downloaders;

namespace BedrockLauncher.ViewModels
{
    public class LauncherModel : NotifyPropertyChangedBase, IDialogHander
    {
        #region Init

        public static LauncherModel Default { get; set; } = new LauncherModel();
        public static MainWindow MainThread => (MainWindow)System.Windows.Application.Current.MainWindow;
        public static LauncherUpdater Updater { get; set; } = new LauncherUpdater();

        public void Init()
        {
            bool isFirstLaunch = Properties.LauncherSettings.Default.CurrentProfile == "" || Properties.LauncherSettings.Default.IsFirstLaunch;

            MainFrame_KeyboardNavigationMode_Default = KeyboardNavigation.GetTabNavigation(MainThread.MainFrame);

            ConfigManager.Init();
            ConfigManager.OnConfigStateChanged(this, Events.ConfigStateArgs.Empty);

            // show first launch window if no profile
            if (isFirstLaunch) SetOverlayFrame_Strict(new WelcomePage());
            MainThread.NavigateToMainPage();
            ProgressBarStateChanged += ViewModel_ProgressBarStateChanged;

            ErrorScreenShow.SetHandler(this);
            DialogPrompt.SetHandler(this);
        }

        #endregion

        #region Page Indexes

        public int CurrentPageIndex { get; private set; } = -2;
        public int LastPageIndex { get; private set; } = -1;

        public int CurrentPageIndex_News { get; private set; } = -2;
        public int LastPageIndex_News { get; private set; } = -1;

        public int CurrentPageIndex_Play { get; private set; } = -2;
        public int LastPageIndex_Play { get; private set; } = -1;

        public int CurrentPageIndex_Settings { get; private set; } = -2;
        public int LastPageIndex_Settings { get; private set; } = -1;

        public void UpdatePageIndex(int index)
        {
            LastPageIndex = CurrentPageIndex;
            CurrentPageIndex = index;
        }

        public void UpdatePlayPageIndex(int index)
        {
            LastPageIndex_Play = CurrentPageIndex_Play;
            CurrentPageIndex_Play = index;
        }

        public void UpdateNewsPageIndex(int index)
        {
            LastPageIndex_News = CurrentPageIndex_News;
            CurrentPageIndex_News = index;
        }

        public void UpdateSettingsPageIndex(int index)
        {
            LastPageIndex_Settings = CurrentPageIndex_Settings;
            CurrentPageIndex_Settings = index;
        }

        #endregion

        #region UI

        private void ViewModel_ProgressBarStateChanged(object sender, EventArgs e)
        {
            Events.ProgressBarState es = e as Events.ProgressBarState;
            if (es.isVisible) ProgressBarShowAnim();
            else ProgressBarHideAnim();
        }


        private void UpdateProgressBarContent(StateChange value)
        {
            Application.Current.Dispatcher.Invoke(() => {
                switch (value)
                {
                    case StateChange.isInitializing:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Downloading");
                        break;
                    case StateChange.isDownloading:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Downloading");
                        break;
                    case StateChange.isExtracting:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Extracting");
                        break;
                    case StateChange.isRegisteringPackage:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_RegisteringPackage");
                        break;
                    case StateChange.isRemovingPackage:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_RemovingPackage");
                        break;
                    case StateChange.isUninstalling:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Uninstalling");
                        break;
                    case StateChange.isLaunching:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Launching");
                        break;
                    case StateChange.isBackingUp:
                        MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_BackingUp");
                        break;
                }

            });
        }
        private void ProgressBarUpdate()
        {
            ProgressBarUpdate(_CurrentProgress, _TotalProgress);
        }
        private void ProgressBarUpdate(double downloadedBytes, double totalSize)
        {
            bool isAdvanced = Properties.LauncherSettings.Default.ShowAdvancedInstallDetails;
            bool isIndeterminate = (CurrentState == StateChange.isInitializing || 
                CurrentState == StateChange.isUninstalling || 
                CurrentState == StateChange.isLaunching || 
                (!isAdvanced ? (CurrentState == StateChange.isRemovingPackage || CurrentState == StateChange.isRegisteringPackage) : false));

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (MainThread != null)
                {
                    MainThread.progressSizeHack.Value = downloadedBytes;
                    MainThread.progressSizeHack.Maximum = totalSize;

                    MainThread.BedrockEditionButton.progressSizeHack.Value = downloadedBytes;
                    MainThread.BedrockEditionButton.progressSizeHack.Maximum = totalSize;

                    MainThread.ProgressBarText.Text = DisplayStatus;

                    MainThread.progressSizeHack.IsIndeterminate = isIndeterminate;
                    MainThread.BedrockEditionButton.progressSizeHack.IsIndeterminate = isIndeterminate;
                }
            });
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
                Storyboard.SetTargetProperty(animation, new PropertyPath(ProgressBar.HeightProperty));
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
                Storyboard.SetTargetProperty(animation, new PropertyPath(ProgressBar.HeightProperty));
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

        public void UpdateUI()
        {
            OnPropertyChanged(nameof(IsGameRunning));
            OnPropertyChanged(nameof(ShowProgressBar));
            OnPropertyChanged(nameof(PlayButtonString));
            OnPropertyChanged(nameof(AllowPlaying));
            OnPropertyChanged(nameof(AllowEditing));
        }


        #endregion

        #region Prompts

        private bool AllowedToCloseWithGameOpen { get; set; } = false;
        private KeyboardNavigationMode MainFrame_KeyboardNavigationMode_Default { get; set; }

        public async void ShowPrompt_ClosingWithGameStillOpened(Action successAction)
        {
            await Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(async () =>
            {
                var title = MainThread.FindResource("Dialog_CloseGame_Title") as string;
                var content = MainThread.FindResource("Dialog_CloseGame_Text") as string;

                var result = await DialogPrompt.ShowDialog_YesNoCancel(title, content);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    ConfigManager.GameManager.GameProcess.Kill();
                    AllowedToCloseWithGameOpen = true;
                    if (successAction != null) successAction.Invoke();
                }
                else if (result == System.Windows.Forms.DialogResult.No)
                {
                    AllowedToCloseWithGameOpen = true;
                    if (successAction != null) successAction.Invoke();
                }
                else if (result == System.Windows.Forms.DialogResult.Cancel)
                {
                    AllowedToCloseWithGameOpen = false;
                }

            }));
        }
        public void AttemptClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Action action = new Action(() =>
            {
                MainThread.Close();
            });

            if (Properties.LauncherSettings.Default.KeepLauncherOpen && ConfigManager.GameManager.GameProcess != null)
            {
                if (!AllowedToCloseWithGameOpen)
                {
                    e.Cancel = true;
                    ShowPrompt_ClosingWithGameStillOpened(action);
                }
            }
            else
            {
                e.Cancel = false;
            }
        }
        public async void SetDialogFrame(object content)
        {
            bool animate = Properties.LauncherSettings.Default.AnimatePageTransitions;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                bool isEmpty = content == null;
                var focusMode = (isEmpty ? MainFrame_KeyboardNavigationMode_Default : KeyboardNavigationMode.None);
                KeyboardNavigation.SetTabNavigation(MainThread.MainFrame, focusMode);
                KeyboardNavigation.SetTabNavigation(MainThread.OverlayFrame, focusMode);
                Keyboard.ClearFocus();

                if (animate)
                {
                    if (isEmpty) PageAnimator.FrameFadeOut(MainThread.ErrorFrame, content);
                    else PageAnimator.FrameFadeIn(MainThread.ErrorFrame, content);
                }
                else MainThread.ErrorFrame.Navigate(content);
            });
        }

        public async void SetOverlayFrame_Strict(object content)
        {
            await Task.Run(() => SetOverlayFrame_Base(content, false));
        }

        public async void SetOverlayFrame(object content)
        {
            bool animate = Properties.LauncherSettings.Default.AnimatePageTransitions;
            await Task.Run(() => SetOverlayFrame_Base(content, animate));
        }


        private async void SetOverlayFrame_Base(object content, bool animate)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                bool isEmpty = content == null;
                var focusMode = (isEmpty ? MainFrame_KeyboardNavigationMode_Default : KeyboardNavigationMode.None);
                KeyboardNavigation.SetTabNavigation(MainThread.MainFrame, focusMode);
                Keyboard.ClearFocus();

                if (animate)
                {
                    if (isEmpty) PageAnimator.FrameSwipe_OverlayOut(MainThread.OverlayFrame, content);
                    else PageAnimator.FrameSwipe_OverlayIn(MainThread.OverlayFrame, content);
                }
                else MainThread.OverlayFrame.Navigate(content);
            });
        }



        #endregion

        #region Enums
        public enum StateChange
        {
            None,
            isInitializing,
            isExtracting,
            isUninstalling,
            isLaunching,
            isDownloading,
            isBackingUp,
            isRemovingPackage,
            isRegisteringPackage
        }

        #endregion

        #region Events

        public event EventHandler ProgressBarStateChanged;
        protected virtual void OnProgressBarStateChanged(ProgressBarState e)
        {
            EventHandler handler = ProgressBarStateChanged;
            if (handler != null) handler(this, e);
        }

        #endregion

        #region One Way Bindings
        public string PlayButtonString
        {
            get
            {         
                if (IsGameRunning) return App.Current.FindResource("GameTab_PlayButton_Kill_Text").ToString();
                else return App.Current.FindResource("GameTab_PlayButton_Text").ToString();
            }
        }
        public bool AllowEditing
        {
            get
            {
                return AllowPlaying && !_IsGameRunning && !ShowProgressBar;
            }
        }
        public bool AllowPlaying
        {
            get
            {
                return CurrentState == StateChange.None && !ShowProgressBar;
            }
        }
        public string DisplayStatus
        {
            get
            {
                switch (CurrentState)
                {
                    case StateChange.isInitializing:
                        return "";
                    case StateChange.isDownloading:
                        return DownloadStatus;
                    case StateChange.isExtracting:
                        return ExtractingStatus;
                    case StateChange.isRegisteringPackage:
                        return DeploymentStatus;
                    case StateChange.isRemovingPackage:
                        return DeploymentStatus;
                    case StateChange.isUninstalling:
                        return "";
                    case StateChange.isLaunching:
                        return "";
                    case StateChange.isBackingUp:
                        return BackupStatus;
                    case StateChange.None:
                        return "";
                    default:
                        return "";
                }
            }
        }
        private string BackupStatus
        {
            get
            {
                return string.Format("{0} / {1}", CurrentProgress, TotalProgress);
            }
        }
        private string DeploymentStatus
        {
            get
            {
                return CurrentProgress + "%" + string.Format(" [ {0} ]", DeploymentPackageName);
            }
        }
        private string ExtractingStatus
        {
            get
            {
                long percent = 0;
                if (TotalProgress != 0) percent = (100 * CurrentProgress) / TotalProgress;
                return percent + "%";
            }
        }
        private string DownloadStatus
        {
            get
            {
                return (Math.Round((double)CurrentProgress / 1024 / 1024, 2)).ToString() + " MB / " + (Math.Round((double)TotalProgress / 1024 / 1024, 2)).ToString() + " MB";
            }
        }

        #endregion

        #region Bindings

        private bool _IsGameRunning = false;
        private bool _ShowProgressBar = false;
        private bool _AllowCancel = false;
        private long _CurrentProgress;
        private long _TotalProgress;
        private StateChange _currentState = StateChange.None;

        public StateChange CurrentState
        {
            get { return _currentState; }
            set
            {
                bool IsAdvancedDetail = value == StateChange.isRegisteringPackage || value == StateChange.isRemovingPackage;
                if (!Properties.LauncherSettings.Default.ShowAdvancedInstallDetails && IsAdvancedDetail) return;
                else
                {
                    _currentState = value;
                    OnPropertyChanged(nameof(DisplayStatus));
                    UpdateProgressBarContent(value);
                    ProgressBarUpdate();
                }
            }
        }
        public bool AllowCancel
        {
            get
            {
                return _AllowCancel;
            }
            set
            {
                _AllowCancel = value;
                OnPropertyChanged(nameof(AllowCancel));
                UpdateUI();
            }
        }
        public bool IsGameRunning
        {
            get
            {
                return _IsGameRunning;
            }
            set
            {
                _IsGameRunning = value;
                OnPropertyChanged(nameof(IsGameRunning));
                UpdateUI();
            }
        }
        public bool ShowProgressBar
        {
            get
            {
                return _ShowProgressBar;
            }
            set
            {
                _ShowProgressBar = value;
                OnProgressBarStateChanged(new ProgressBarState(value));
                OnPropertyChanged(nameof(ShowProgressBar));
                UpdateUI();
            }
        }
        public long CurrentProgress
        {
            get
            {
                return _CurrentProgress;
            }
            set
            {
                _CurrentProgress = value;
                ProgressBarUpdate(_CurrentProgress, _TotalProgress);
                OnPropertyChanged(nameof(CurrentProgress));
                OnPropertyChanged(nameof(DisplayStatus));
                UpdateUI();
            }
        }
        public long TotalProgress
        {
            get
            {
                return _TotalProgress;
            }
            set
            {
                _TotalProgress = value;
                OnPropertyChanged(nameof(TotalProgress));
                OnPropertyChanged(nameof(DisplayStatus));
                UpdateUI();
            }
        }

        #endregion

        #region Definitions

        public const long DeploymentMaximum = 100;
        public ICommand CancelCommand { get; set; }
        public string DeploymentPackageName { get; set; }

        #endregion
    }
}
