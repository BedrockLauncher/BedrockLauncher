using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms.Design;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Data;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Core;
using Windows.Management.Deployment;
using Windows.System;
using BedrockLauncher.Interfaces;
using BedrockLauncher.Methods;
using BedrockLauncher.Classes;
using System.Windows.Media.Animation;
using BedrockLauncher.Pages;
using BedrockLauncher.Pages.FirstLaunch;
using BedrockLauncher.ViewModels;

using Version = BedrockLauncher.Classes.MCVersion;

namespace BedrockLauncher
{
    //TODO: Improve backup overwrite response (to avoid having to click it forever)
    //TODO: Add the insider account control to the First Time Welcome Screen
    //TODO: Add translation support to the remaining entries in GameManager.cs
    //TODO: (Later On) Better Animations
    //TODO: (Later On) Community Content / Personal Donations Section


    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        #region Definitions
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

        private NoContentPage noContentPage = new NoContentPage();
        #endregion

        #region Extra Variables

        private KeyboardNavigationMode MainFrame_KeyboardNavigationMode_Default { get; set; }
        private bool AllowedToCloseWithGameOpen { get; set; } = false;

        #endregion

        #region Init

        public MainWindow()
        {
            InitializeComponent();
           if (LicenseManager.UsageMode != LicenseUsageMode.Designtime) Init();
        }

        private void Init()
        {
            this.DataContext = ConfigManager.ViewModel;
            ConfigManager.ViewModel.ProgressBarStateChanged += ViewModel_ProgressBarStateChanged;


            updateButton.ClickBase.Click += Updater.UpdateButton_Click;
            MainFrame_KeyboardNavigationMode_Default = KeyboardNavigation.GetTabNavigation(MainFrame);

            Panel.SetZIndex(OverlayFrame, 0);
            Panel.SetZIndex(ErrorFrame, 1);
            Panel.SetZIndex(updateButton, 2);
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            // show first launch window if no profile
            if (Properties.LauncherSettings.Default.CurrentProfile == "" || Properties.LauncherSettings.Default.IsFirstLaunch) SetOverlayFrame(new WelcomePage());
            NavigateToPlayScreen();


            ConfigManager.Init();
            ConfigManager.ConfigStateChanged += ConfigManager_ConfigStateChanged;
            ConfigManager.OnConfigStateChanged(this, Events.ConfigStateArgs.Empty);
        }

        #endregion

        #region UI

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

        public void RefreshInstallationList()
        {
            installationsScreen.RefreshInstallationsList();
        }

        public void RefreshInstallationControls()
        {
            installationsScreen.InstallationsList.ItemsSource = ConfigManager.CurrentInstallations;
            var view = CollectionViewSource.GetDefaultView(installationsScreen.InstallationsList.ItemsSource) as CollectionView;
            view.Filter = ConfigManager.Filter_InstallationList;

            InstallationsList.ItemsSource = ConfigManager.CurrentInstallations;
            if (InstallationsList.SelectedItem == null) InstallationsList.SelectedItem = ConfigManager.CurrentInstallations.First();
        }

        private void ViewModel_ProgressBarStateChanged(object sender, EventArgs e)
        {
            Events.ProgressBarState es = e as Events.ProgressBarState;
            if (es.isVisible) ProgressBarShowAnim();
            else ProgressBarHideAnim();
        }

        private async void ProgressBarShowAnim()
        {
            if (ProgressBarGrid.Visibility == Visibility.Visible) return;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                BedrockEditionButton.progressSizeHack.Visibility = Visibility.Visible;
                ProgressBarGrid.Visibility = Visibility.Visible;
                ProgressBarText.Visibility = Visibility.Hidden;
                progressbarcontent.Visibility = Visibility.Hidden;

                Storyboard storyboard = new Storyboard();
                DoubleAnimation animation = new DoubleAnimation
                {
                    From = 0,
                    To = 62,
                    Duration = new Duration(TimeSpan.FromMilliseconds(350))
                };
                storyboard.Children.Add(animation);
                Storyboard.SetTargetProperty(animation, new PropertyPath(ProgressBar.HeightProperty));
                Storyboard.SetTarget(animation, ProgressBarGrid);
                storyboard.Completed += new EventHandler(ShowProgressBarContent);
                storyboard.Begin();
            });
        }
        private async void ProgressBarHideAnim()
        {
            if (ProgressBarGrid.Visibility == Visibility.Collapsed) return;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ProgressBarGrid.Visibility = Visibility.Collapsed;
                ProgressBarText.Visibility = Visibility.Hidden;

                Storyboard storyboard = new Storyboard();
                DoubleAnimation animation = new DoubleAnimation
                {
                    From = 62,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(350))
                };
                storyboard.Children.Add(animation);
                Storyboard.SetTargetProperty(animation, new PropertyPath(ProgressBar.HeightProperty));
                Storyboard.SetTarget(animation, ProgressBarGrid);
                storyboard.Completed += new EventHandler(HideProgressBarContent);
                storyboard.Begin();
            });
        }

        private void HideProgressBarContent(object sender, EventArgs e)
        {
            progressbarcontent.Visibility = Visibility.Hidden;
            BedrockEditionButton.progressSizeHack.Visibility = Visibility.Collapsed;
        }
        private void ShowProgressBarContent(object sender, EventArgs e)
        {
            ProgressBarText.Visibility = Visibility.Visible;
            progressbarcontent.Visibility = Visibility.Visible;
        }
        private void MainPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigManager.GameManager.GameProcess != null)
            {
                ConfigManager.GameManager.KillGame();
            }
            else
            {
                var i = InstallationsList.SelectedItem as MCInstallation;
                ConfigManager.GameManager.Play(i);
            }

        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }
        private void Window_Initialized(object sender, EventArgs e)
        {

        }
        private void ConfigManager_ConfigStateChanged(object sender, EventArgs e)
        {
            RefreshInstallationControls();
            RefreshVersionControls();
        }
        private void InstallationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            skinsPage.ReloadPage();
        }

        #endregion

        #region Navigation

        public void SetDialogFrame(object content)
        {
            bool isEmpty = content == null;
            var focusMode = (isEmpty ? MainFrame_KeyboardNavigationMode_Default : KeyboardNavigationMode.None);
            KeyboardNavigation.SetTabNavigation(MainFrame, focusMode);
            KeyboardNavigation.SetTabNavigation(OverlayFrame, focusMode);
            Keyboard.ClearFocus();
            ErrorFrame.Navigate(content);
        }

        public void SetOverlayFrame(object content)
        {
            bool isEmpty = content == null;
            var focusMode = (isEmpty ? MainFrame_KeyboardNavigationMode_Default : KeyboardNavigationMode.None);
            KeyboardNavigation.SetTabNavigation(MainFrame, focusMode);
            Keyboard.ClearFocus();
            OverlayFrame.Navigate(content);
        } 

        public void ResetButtonManager(string buttonName)
        {
            // just all buttons list
            // ya i know this is really bad, i need to learn mvvm instead of doing this shit
            // but this works fine, at least
            List<ToggleButton> toggleButtons = new List<ToggleButton>() { 
                // main window
                CommunityButton.Button,
                NewsButton.Button,
                BedrockEditionButton.Button,
                JavaEditionButton.Button,
                SettingsButton.Button,

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

            PlayScreenBorder.Visibility = Visibility.Collapsed;

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

            if (senderName == BedrockEditionButton.Name) NavigateToMainPage();
            else if (senderName == NewsButton.Name) NavigateToNewsPage();
            else if (senderName == JavaEditionButton.Name) NavigateToJavaLauncher();
            else if (senderName == ExternalLauncherButton.Name) NavigateToExternalLauncher();
            else if (senderName == CommunityButton.Name) NavigateToCommunityScreen();
            else if (senderName == SettingsButton.Name) NavigateToSettings();

            // MainPageButtons
            else if (senderName == mainPage.PlayButton.Name) NavigateToPlayScreen();
            else if (senderName == mainPage.InstallationsButton.Name) NavigateToInstallationsPage();
            else if (senderName == mainPage.SkinsButton.Name) NavigateToSkinsPage();
            else if (senderName == mainPage.PatchNotesButton.Name) NavigateToPatchNotes();
            else NavigateToPlayScreen();

        }

        public void NavigateToMainPage(bool rooted = false)
        {
            BedrockEditionButton.Button.IsChecked = true;
            MainWindowFrame.Navigate(mainPage);

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
            MainWindowFrame.Navigate(newsScreenPage);
            NewsButton.Button.IsChecked = true;
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

        public void NavigateToCommunityScreen()
        {
            CommunityButton.Button.IsChecked = true;
            MainWindowFrame.Navigate(noContentPage);
            settingsScreenPage.SettingsScreenFrame.Navigate(noContentPage);
        }
        public void NavigateToSettings()
        {
            settingsScreenPage.GeneralButton.IsChecked = true;
            SettingsButton.Button.IsChecked = true;
            MainWindowFrame.Navigate(settingsScreenPage);
            settingsScreenPage.SettingsScreenFrame.Navigate(generalSettingsPage);
        }

        public void NavigateToPlayScreen()
        {
            NavigateToMainPage(true);
            mainPage.PlayButton.IsChecked = true;
            mainPage.MainPageFrame.Navigate(playScreenPage);
            mainPage.LastButtonName = mainPage.PlayButton.Name;
            PlayScreenBorder.Visibility = Visibility.Visible;
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

        #region Closing Stuff

        private async void ShowPrompt_ClosingWithGameStillOpened(Action successAction)
        {
            await Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(async () =>
            {
                var title = this.FindResource("Dialog_CloseGame_Title") as string;
                var content = this.FindResource("Dialog_CloseGame_Text") as string;

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
        public async void NavigateToOtherLauncher(Action action)
        {
            if (Properties.LauncherSettings.Default.CloseLauncherOnSwitch && ConfigManager.GameManager.GameProcess != null)
            {
                await Task.Run(() => ShowPrompt_ClosingWithGameStillOpened(action));
            }
            else action.Invoke();
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Action action = new Action(() =>
            {
                this.Close();
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


        #endregion


    }
}
