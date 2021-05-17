using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Events;
using BedrockLauncher.Classes;
using BedrockLauncher.Components;
using System.Windows;
using System.Windows.Input;
using BedrockLauncher.Methods;
using System.Windows.Controls;
using BedrockLauncher.Pages;
using BedrockLauncher.Pages.Common;
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
using BedrockLauncher.Downloaders;

namespace BedrockLauncher.ViewModels
{
    public class LauncherModel : NotifyPropertyChangedBase
    {
        public void Init()
        {
            bool isFirstLaunch = Properties.LauncherSettings.Default.CurrentProfile == "" || Properties.LauncherSettings.Default.IsFirstLaunch;

            MainFrame_KeyboardNavigationMode_Default = KeyboardNavigation.GetTabNavigation(ConfigManager.MainThread.MainFrame);

            ConfigManager.Init();
            ConfigManager.ConfigStateChanged += ConfigManager_ConfigStateChanged;
            ConfigManager.OnConfigStateChanged(this, Events.ConfigStateArgs.Empty);

            // show first launch window if no profile
            if (isFirstLaunch) SetOverlayFrame(new WelcomePage(), false);
            NavigateToPlayScreen();

            ConfigManager.MainThread.updateButton.ClickBase.Click += Updater.UpdateButton_Click;
            ConfigManager.ViewModel.ProgressBarStateChanged += ViewModel_ProgressBarStateChanged;
        }

        #region Pages

        // updater to check for updates (loaded in mainwindow init)
        public static LauncherUpdater Updater = new LauncherUpdater();
        private static ChangelogDownloader PatchNotesDownloader = new ChangelogDownloader();

        // load pages to not create new in memory after
        private GameTabs mainPage = new GameTabs();
        private GeneralSettingsPage generalSettingsPage = new GeneralSettingsPage();
        private SettingsTabs settingsScreenPage = new SettingsTabs();
        private NewsScreenTabs newsScreenPage = new NewsScreenTabs(Updater);
        private PlayScreenPage playScreenPage = new PlayScreenPage();
        private InstallationsScreen installationsScreen = new InstallationsScreen();
        private SkinsPage skinsPage = new SkinsPage();
        private PatchNotesPage patchNotesPage = new PatchNotesPage(PatchNotesDownloader);
        private CommunityPage communityPage = new CommunityPage();

        private NoContentPage noContentPage = new NoContentPage();

        #endregion

        #region UI

        private void ViewModel_ProgressBarStateChanged(object sender, EventArgs e)
        {
            Events.ProgressBarState es = e as Events.ProgressBarState;
            if (es.isVisible) ProgressBarShowAnim();
            else ProgressBarHideAnim();
        }
        private void ConfigManager_ConfigStateChanged(object sender, EventArgs e)
        {
            RefreshInstallationControls();
            RefreshVersionControls();
        }

        private void UpdateProgressBarContent(StateChange value)
        {
            Application.Current.Dispatcher.Invoke(() => {
                switch (value)
                {
                    case StateChange.isInitializing:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Downloading");
                        break;
                    case StateChange.isDownloading:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Downloading");
                        break;
                    case StateChange.isExtracting:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Extracting");
                        break;
                    case StateChange.isRegisteringPackage:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_RegisteringPackage");
                        break;
                    case StateChange.isRemovingPackage:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_RemovingPackage");
                        break;
                    case StateChange.isUninstalling:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Uninstalling");
                        break;
                    case StateChange.isLaunching:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Launching");
                        break;
                    case StateChange.isBackingUp:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_BackingUp");
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
            bool isIndeterminate = (CurrentState == StateChange.isInitializing || CurrentState == StateChange.isUninstalling || CurrentState == StateChange.isLaunching);

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ConfigManager.MainThread != null)
                {
                    ConfigManager.MainThread.progressSizeHack.Value = downloadedBytes;
                    ConfigManager.MainThread.progressSizeHack.Maximum = totalSize;

                    ConfigManager.MainThread.BedrockEditionButton.progressSizeHack.Value = downloadedBytes;
                    ConfigManager.MainThread.BedrockEditionButton.progressSizeHack.Maximum = totalSize;

                    ConfigManager.MainThread.ProgressBarText.Text = DisplayStatus;

                    ConfigManager.MainThread.progressSizeHack.IsIndeterminate = isIndeterminate;
                    ConfigManager.MainThread.BedrockEditionButton.progressSizeHack.IsIndeterminate = isIndeterminate;
                }
            });
        }

        private async void ProgressBarShowAnim()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ShowProgressBarContent_Pre();
                Storyboard storyboard = new Storyboard();
                DoubleAnimation animation = new DoubleAnimation
                {
                    From = 0,
                    To = 72,
                    Duration = new Duration(TimeSpan.FromMilliseconds(350))
                };
                storyboard.Children.Add(animation);
                Storyboard.SetTargetProperty(animation, new PropertyPath(ProgressBar.HeightProperty));
                Storyboard.SetTarget(animation, ConfigManager.MainThread.ProgressBarGrid);
                storyboard.Completed += new EventHandler(ShowProgressBarContent);
                storyboard.Begin();
            });
        }
        private async void ProgressBarHideAnim()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                HideProgressBarContent_Pre();

                Storyboard storyboard = new Storyboard();
                DoubleAnimation animation = new DoubleAnimation
                {
                    From = 72,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(350))
                };
                storyboard.Children.Add(animation);
                Storyboard.SetTargetProperty(animation, new PropertyPath(ProgressBar.HeightProperty));
                Storyboard.SetTarget(animation, ConfigManager.MainThread.ProgressBarGrid);
                storyboard.Completed += new EventHandler(HideProgressBarContent);
                storyboard.Begin();
            });
        }
        private void HideProgressBarContent_Pre()
        {
            ConfigManager.MainThread.BedrockEditionButton.progressSizeHack.Visibility = Visibility.Hidden;
            ConfigManager.MainThread.ProgressBarGrid.Visibility = Visibility.Visible;
            ConfigManager.MainThread.ProgressBarText.Visibility = Visibility.Hidden;
            ConfigManager.MainThread.progressbarcontent.Visibility = Visibility.Hidden;
        }
        private void HideProgressBarContent(object sender, EventArgs e)
        {
            ConfigManager.MainThread.BedrockEditionButton.progressSizeHack.Visibility = Visibility.Hidden;
            ConfigManager.MainThread.ProgressBarGrid.Visibility = Visibility.Collapsed;
            ConfigManager.MainThread.ProgressBarText.Visibility = Visibility.Hidden;
            ConfigManager.MainThread.progressbarcontent.Visibility = Visibility.Hidden;
        }
        private void ShowProgressBarContent_Pre()
        {
            ConfigManager.MainThread.BedrockEditionButton.progressSizeHack.Visibility = Visibility.Visible;
            ConfigManager.MainThread.ProgressBarGrid.Visibility = Visibility.Visible;
            ConfigManager.MainThread.ProgressBarText.Visibility = Visibility.Hidden;
            ConfigManager.MainThread.progressbarcontent.Visibility = Visibility.Hidden;
        }
        private void ShowProgressBarContent(object sender, EventArgs e)
        {
            ConfigManager.MainThread.BedrockEditionButton.progressSizeHack.Visibility = Visibility.Visible;
            ConfigManager.MainThread.ProgressBarGrid.Visibility = Visibility.Visible;
            ConfigManager.MainThread.ProgressBarText.Visibility = Visibility.Visible;
            ConfigManager.MainThread.progressbarcontent.Visibility = Visibility.Visible;
        }

        public void UpdateUI()
        {
            OnPropertyChanged(nameof(IsGameRunning));
            OnPropertyChanged(nameof(ShowProgressBar));
            OnPropertyChanged(nameof(PlayButtonString));
            OnPropertyChanged(nameof(AllowPlaying));
            OnPropertyChanged(nameof(AllowEditing));
        }
        public void RefreshSkinsPage()
        {
            skinsPage.ReloadSkinPacks();
        }
        public void RefreshVersionControls()
        {
            settingsScreenPage.versionsSettingsPage.VersionsList.ItemsSource = ConfigManager.Versions;
            var view = CollectionViewSource.GetDefaultView(settingsScreenPage.versionsSettingsPage.VersionsList.ItemsSource) as CollectionView;
            view.Filter = ConfigManager.Filter_VersionList;
        }
        public void SetVersionPageSelection(object item)
        {
            settingsScreenPage.versionsSettingsPage.VersionsList.SelectedItem = item;
        }
        public void SetInstallationPageSelection(object item)
        {
            installationsScreen.InstallationsList.SelectedItem = item;
        }
        public void RefreshConfig()
        {
            installationsScreen.RefreshInstallationsList();
        }
        public void RefreshInstallationControls()
        {
            installationsScreen.InstallationsList.ItemsSource = ConfigManager.CurrentInstallations;
            var view = CollectionViewSource.GetDefaultView(installationsScreen.InstallationsList.ItemsSource) as CollectionView;
            view.Filter = ConfigManager.Filter_InstallationList;

            ConfigManager.MainThread.InstallationsList.Items.Cast<MCInstallation>().ToList().ForEach(x => x.Update());

            ConfigManager.MainThread.InstallationsList.ItemsSource = ConfigManager.CurrentInstallations;
            if (ConfigManager.MainThread.InstallationsList.SelectedItem == null) ConfigManager.MainThread.InstallationsList.SelectedItem = ConfigManager.CurrentInstallations.First();
        }

        #endregion

        #region Prompts

        private bool AllowedToCloseWithGameOpen { get; set; } = false;
        private KeyboardNavigationMode MainFrame_KeyboardNavigationMode_Default { get; set; }
        private async void ShowPrompt_ClosingWithGameStillOpened(Action successAction)
        {
            await Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(async () =>
            {
                var title = ConfigManager.MainThread.FindResource("Dialog_CloseGame_Title") as string;
                var content = ConfigManager.MainThread.FindResource("Dialog_CloseGame_Text") as string;

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
                ConfigManager.MainThread.Close();
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
        public void SetDialogFrame(object content, bool useFade = true)
        {
            bool isEmpty = content == null;
            var focusMode = (isEmpty ? MainFrame_KeyboardNavigationMode_Default : KeyboardNavigationMode.None);
            KeyboardNavigation.SetTabNavigation(ConfigManager.MainThread.MainFrame, focusMode);
            KeyboardNavigation.SetTabNavigation(ConfigManager.MainThread.OverlayFrame, focusMode);
            Keyboard.ClearFocus();

            if (useFade)
            {
                if (isEmpty) FrameFadeOut(ConfigManager.MainThread.ErrorFrame, content);
                else FrameFadeIn(ConfigManager.MainThread.ErrorFrame, content);
            }
            else ConfigManager.MainThread.ErrorFrame.Navigate(content);
        }
        public void SetOverlayFrame(object content, bool useFade = true)
        {
            bool isEmpty = content == null;
            var focusMode = (isEmpty ? MainFrame_KeyboardNavigationMode_Default : KeyboardNavigationMode.None);
            KeyboardNavigation.SetTabNavigation(ConfigManager.MainThread.MainFrame, focusMode);
            Keyboard.ClearFocus();

            if (useFade)
            {
                if (isEmpty) FrameFadeOut(ConfigManager.MainThread.OverlayFrame, content);
                else FrameFadeIn(ConfigManager.MainThread.OverlayFrame, content);
            }
            else ConfigManager.MainThread.OverlayFrame.Navigate(content);

        }

        private void FrameFadeIn(Frame frame, object content)
        {
            frame.Opacity = 0;
            frame.Navigate(content);
            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(200))
            };

            storyboard.Children.Add(animation);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Frame.OpacityProperty));
            Storyboard.SetTarget(animation, frame);
            storyboard.Begin();
        }

        private void FrameFadeOut(Frame frame, object content)
        {
            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(200))
            };
            storyboard.Children.Add(animation);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Frame.OpacityProperty));
            Storyboard.SetTarget(animation, frame);
            storyboard.Completed += (sender, e) => frame.Navigate(content);
            storyboard.Begin();
        }

        #endregion

        #region Navigation

        public void ResetButtonManager(string buttonName)
        {
            // just all buttons list
            // ya i know this is really bad, i need to learn mvvm instead of doing this shit
            // but this works fine, at least
            List<ToggleButton> toggleButtons = new List<ToggleButton>() { 
                // main window
                ConfigManager.MainThread.CommunityButton.Button,
                ConfigManager.MainThread.NewsButton.Button,
                ConfigManager.MainThread.BedrockEditionButton.Button,
                ConfigManager.MainThread.JavaEditionButton.Button,
                ConfigManager.MainThread.SettingsButton.Button,

                // play tab
                mainPage.PlayButton,
                mainPage.InstallationsButton,
                mainPage.SkinsButton,
                mainPage.PatchNotesButton,
                
                // settings screen
                settingsScreenPage.GeneralButton,
                settingsScreenPage.AccountsButton,
                settingsScreenPage.VersionsButton,
                settingsScreenPage.AboutButton
            };

            foreach (ToggleButton button in toggleButtons) { button.IsChecked = false; }

            ConfigManager.MainThread.PlayScreenBorder.Visibility = Visibility.Collapsed;

            if (toggleButtons.Exists(x => x.Name == buttonName))
            {
                toggleButtons.Where(x => x.Name == buttonName).FirstOrDefault().IsChecked = true;
            }
        }
        public void ButtonManager(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            ButtonManager_Base(toggleButton.Name);
        }
        public void ButtonManager_Base(string senderName)
        {
            ResetButtonManager(senderName);

            if (senderName == ConfigManager.MainThread.BedrockEditionButton.Name) NavigateToMainPage();
            else if (senderName == ConfigManager.MainThread.NewsButton.Name) NavigateToNewsPage();
            else if (senderName == ConfigManager.MainThread.JavaEditionButton.Name) NavigateToJavaLauncher();
            else if (senderName == ConfigManager.MainThread.ExternalLauncherButton.Name) NavigateToExternalLauncher();
            else if (senderName == ConfigManager.MainThread.CommunityButton.Name) NavigateToCommunityScreen();
            else if (senderName == ConfigManager.MainThread.SettingsButton.Name) NavigateToSettings();

            // MainPageButtons
            else if (senderName == mainPage.PlayButton.Name) NavigateToPlayScreen();
            else if (senderName == mainPage.InstallationsButton.Name) NavigateToInstallationsPage();
            else if (senderName == mainPage.SkinsButton.Name) NavigateToSkinsPage();
            else if (senderName == mainPage.PatchNotesButton.Name) NavigateToPatchNotes();
            else NavigateToPlayScreen();

        }
        public void NavigateToMainPage(bool rooted = false)
        {
            ConfigManager.MainThread.BedrockEditionButton.Button.IsChecked = true;
            ConfigManager.MainThread.MainWindowFrame.Navigate(mainPage);

            if (!rooted)
            {
                if (mainPage.LastButtonName == mainPage.PlayButton.Name) NavigateToPlayScreen();
                else if (mainPage.LastButtonName == mainPage.InstallationsButton.Name) NavigateToInstallationsPage();
                else if (mainPage.LastButtonName == mainPage.SkinsButton.Name) NavigateToSkinsPage();
                else if (mainPage.LastButtonName == mainPage.PatchNotesButton.Name) NavigateToPatchNotes();
            }
        }
        public void NavigateToNewsPage()
        {
            ConfigManager.MainThread.MainWindowFrame.Navigate(newsScreenPage);
            ConfigManager.MainThread.NewsButton.Button.IsChecked = true;
        }
        public void NavigateToJavaLauncher()
        {
            Action action = new Action(() =>
            {
                try
                {
                    // Trying to find and open java launcher shortcut
                    string JavaPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + @"\Programs\Minecraft Launcher\Minecraft Launcher";
                    Process.Start(JavaPath);
                    Application.Current.MainWindow.Close();
                }
                catch
                {
                    NavigateToPlayScreen();
                    ErrorScreenShow.errormsg("CantFindJavaLauncher");
                }
            });

            NavigateToOtherLauncher(action);
        }
        public void NavigateToExternalLauncher()
        {
            Action action = new Action(() =>
            {
                try
                {
                    Process.Start(Properties.LauncherSettings.Default.ExternalLauncherPath);
                    Application.Current.MainWindow.Close();
                }
                catch
                {
                    NavigateToPlayScreen();
                    ErrorScreenShow.errormsg("CantFindExternalLauncher");
                }
            });

            NavigateToOtherLauncher(action);
        }
        public async void NavigateToOtherLauncher(Action action)
        {
            if (Properties.LauncherSettings.Default.CloseLauncherOnSwitch && ConfigManager.GameManager.GameProcess != null)
            {
                await Task.Run(() => ShowPrompt_ClosingWithGameStillOpened(action));
            }
            else action.Invoke();
        }
        public void NavigateToCommunityScreen()
        {
            ConfigManager.MainThread.MainWindowFrame.Navigate(communityPage);
            ConfigManager.MainThread.CommunityButton.Button.IsChecked = true;
        }
        public void NavigateToSettings()
        {
            settingsScreenPage.GeneralButton.IsChecked = true;
            ConfigManager.MainThread.SettingsButton.Button.IsChecked = true;
            ConfigManager.MainThread.MainWindowFrame.Navigate(settingsScreenPage);
            settingsScreenPage.SettingsScreenFrame.Navigate(generalSettingsPage);
        }
        public void NavigateToPlayScreen()
        {
            NavigateToMainPage(true);
            mainPage.PlayButton.IsChecked = true;
            mainPage.MainPageFrame.Navigate(playScreenPage);
            mainPage.LastButtonName = mainPage.PlayButton.Name;
            ConfigManager.MainThread.PlayScreenBorder.Visibility = Visibility.Visible;
        }
        public void NavigateToInstallationsPage()
        {
            NavigateToMainPage(true);
            mainPage.InstallationsButton.IsChecked = true;
            mainPage.MainPageFrame.Navigate(installationsScreen);
            mainPage.LastButtonName = mainPage.InstallationsButton.Name;
        }
        public void NavigateToSkinsPage()
        {
            NavigateToMainPage(true);
            mainPage.SkinsButton.IsChecked = true;
            mainPage.MainPageFrame.Navigate(skinsPage);
            skinsPage.ReloadSkinPacks();
            mainPage.LastButtonName = mainPage.SkinsButton.Name;
        }
        public void NavigateToPatchNotes()
        {
            NavigateToMainPage(true);
            mainPage.PatchNotesButton.IsChecked = true;
            mainPage.MainPageFrame.Navigate(patchNotesPage);
            mainPage.LastButtonName = mainPage.PatchNotesButton.Name;
        }
        public void NavigateToNewProfilePage()
        {
            SetOverlayFrame(new AddProfilePage());
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
                _currentState = value;
                OnPropertyChanged(nameof(DisplayStatus));
                UpdateProgressBarContent(value);
                ProgressBarUpdate();
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
