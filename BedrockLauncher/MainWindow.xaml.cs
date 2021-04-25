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
using ServerTab;
using BedrockLauncher.Core;
using BedrockLauncher.Pages;
using BedrockLauncher.Pages.FirstLaunch;

using Version = BedrockLauncher.Classes.MCVersion;

namespace BedrockLauncher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        #region Definitions
        // updater to check for updates (loaded in mainwindow init)
        private static LauncherUpdater Updater = new LauncherUpdater();
        private static ChangelogDownloader PatchNotesDownloader = new ChangelogDownloader();

        // servers dll stuff
        private static ServersTab serversTab = new ServersTab();

        // load pages to not create new in memory after
        private GameTabs mainPage = new GameTabs();
        private GeneralSettingsPage generalSettingsPage = new GeneralSettingsPage();
        private SettingsTabs settingsScreenPage = new SettingsTabs();
        private NewsScreenTabs newsScreenPage = new NewsScreenTabs(Updater);
        private PlayScreenPage playScreenPage = new PlayScreenPage();
        private InstallationsScreen installationsScreen = new InstallationsScreen();
        private ServersPage serversScreenPage = new ServersPage(serversTab);
        private SkinsPage skinsPage = new SkinsPage();
        private PatchNotesPage patchNotesPage = new PatchNotesPage(PatchNotesDownloader);

        private NoContentPage noContentPage = new NoContentPage();
        #endregion

        #region Init

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            Panel.SetZIndex(MainWindowOverlayFrame, 0);
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            // show first launch window if no profile
            if (Properties.Settings.Default.CurrentProfile == "") MainWindowOverlayFrame.Navigate(new WelcomePage());
            NavigateToPlayScreen();


            ConfigManager.Init();
            ConfigManager.GameManager.GameStateChanged += GameManager_GameStateChanged;
            ConfigManager.ConfigStateChanged += ConfigManager_ConfigStateChanged;
            ConfigManager.OnConfigStateChanged(this, ConfigManager.ConfigStateArgs.Empty);
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
        public void RefreshInstallationControls()
        {
            installationsScreen.InstallationsList.ItemsSource = ConfigManager.CurrentInstallations;
            var view = CollectionViewSource.GetDefaultView(installationsScreen.InstallationsList.ItemsSource) as CollectionView;
            view.Filter = ConfigManager.Filter_InstallationList;

            InstallationsList.ItemsSource = ConfigManager.CurrentInstallations;
            InstallationsList.SelectedIndex = Properties.Settings.Default.CurrentInstallation;
            if (InstallationsList.SelectedItem == null) InstallationsList.SelectedItem = ConfigManager.CurrentInstallations.First();
        }
        private void UpdatePlayButton()
        {
            Task.Run(async () =>
            {
                   await Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                   {
                       var selected = InstallationsList.SelectedItem as MCInstallation;
                       if (selected == null)
                       {
                           PlayButtonText.SetResourceReference(TextBlock.TextProperty, "MainPage_PlayButton");
                           MainPlayButton.IsEnabled = false;
                           return;
                       }

                       if (ConfigManager.GameManager.IsDownloading || ConfigManager.GameManager.HasLaunchTask)
                       {
                           ProgressBarShowAnim();
                           MainPlayButton.IsEnabled = false;
                           InstallationsList.IsEnabled = false;
                       }
                       else
                       {
                           ProgressBarHideAnim();
                           MainPlayButton.IsEnabled = true;
                           InstallationsList.IsEnabled = true;
                       }

                       if (selected.Version?.IsInstalled ?? false) PlayButtonText.SetResourceReference(TextBlock.TextProperty, "MainPage_PlayButton");
                       else PlayButtonText.SetResourceReference(TextBlock.TextProperty, "MainPage_PlayButton");
                   }));
            });
        }
        private async void ProgressBarShowAnim()
        {
            if (ProgressBarGrid.Visibility == Visibility.Visible) return;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
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
            ProgressBarGrid.Visibility = Visibility.Collapsed;
            ProgressBarText.Visibility = Visibility.Hidden;
            progressbarcontent.Visibility = Visibility.Hidden;
        }

        private void ShowProgressBarContent(object sender, EventArgs e)
        {
            ProgressBarText.Visibility = Visibility.Visible;
            progressbarcontent.Visibility = Visibility.Visible;
        }
        private void MainPlayButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CurrentInstallation = InstallationsList.SelectedIndex;
            Properties.Settings.Default.Save();
            var i = InstallationsList.SelectedItem as MCInstallation;
            ConfigManager.GameManager.Play(i);
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }
        private void Window_Initialized(object sender, EventArgs e)
        {
            LanguageManager.Init();
        }
        private void ConfigManager_ConfigStateChanged(object sender, EventArgs e)
        {
            RefreshInstallationControls();
            RefreshVersionControls();
        }
        private void GameManager_GameStateChanged(object sender, EventArgs e)
        {
            UpdatePlayButton();
            RefreshVersionControls();
        }
        private void GameStateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePlayButton();
        }
        private void GameStateChanged(object sender, EventArgs e)
        {
            UpdatePlayButton();
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
                ServersButton.Button,
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
            else if (senderName == ServersButton.Name) NavigateToServersScreen();
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
            PlayScreenBorder.Visibility = Visibility.Visible;

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
        }
        public void NavigateToServersScreen()
        {
            string file = System.IO.Path.Combine(System.Reflection.Assembly.GetEntryAssembly().Location, "servers_paid.json");
            if (File.Exists(file))
            {
                MainWindowFrame.Navigate(serversScreenPage);
                ServersButton.Button.IsChecked = true;
            }
            else
            {
                NavigateToPlayScreen();
                ErrorScreenShow.errormsg("CantFindPaidServerList");
            }
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
            MainWindowOverlayFrame.Navigate(new AddProfilePage());
        }


        #endregion
    }
}
