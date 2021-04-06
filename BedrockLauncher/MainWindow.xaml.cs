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
using BedrockLauncher.Pages.GameScreen;
using BedrockLauncher.Pages.NoContentScreen;
using BedrockLauncher.Pages.PlayScreen;
using BedrockLauncher.Pages.ServersScreen;
using BedrockLauncher.Pages.SettingsScreen;
using BedrockLauncher.Pages.FirstLaunch;
using BedrockLauncher.Pages.ErrorScreen;
using BedrockLauncher.Pages.InstallationsScreen;
using BedrockLauncher.Pages.NewsScreen;
using BedrockLauncher.Pages.ProfileManagementScreen;
using System.Windows.Media.Animation;
using ServerTab;
using BedrockLauncher.Core;
using BedrockLauncher.Pages.SkinsScreen;

using Version = BedrockLauncher.Classes.Version;

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

        // servers dll stuff
        private static ServersTab serversTab = new ServersTab();

        // load pages to not create new in memory after
        private GamePage mainPage = new GamePage();
        private GeneralSettingsPage generalSettingsPage = new GeneralSettingsPage();
        private SettingsScreen settingsScreenPage = new SettingsScreen();
        private NewsScreenPage newsScreenPage = new NewsScreenPage(Updater);
        private PlayScreenPage playScreenPage = new PlayScreenPage();
        private InstallationsScreen installationsScreen = new InstallationsScreen();
        private ServersScreenPage serversScreenPage = new ServersScreenPage(serversTab);
        private SkinsPage skinsPage = new SkinsPage();

        private NoContentPage noContentPage = new NoContentPage();
        #endregion

        #region Init

        public MainWindow()
        {
            InitializeComponent();
            Panel.SetZIndex(MainWindowOverlayFrame, 0);
            //serversTab.readServers();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            // show first launch window if no profile
            if (Properties.Settings.Default.CurrentProfile == "") MainWindowOverlayFrame.Navigate(new WelcomePage());
            Init();
        }
        private void Init()
        {
            ConfigManager.Init();
            ConfigManager.GameManager.GameStateChanged += GameManager_GameStateChanged;
            ConfigManager.ConfigStateChanged += ConfigManager_ConfigStateChanged;
            ConfigManager.OnConfigStateChanged(this, ConfigManager.ConfigStateArgs.Empty);
        }

        #endregion

        #region UI

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
                       var selected = InstallationsList.SelectedItem as Installation;
                       if (selected == null)
                       {
                           PlayButtonText.SetResourceReference(TextBlock.TextProperty, "MainPage_PlayButton");
                           MainPlayButton.IsEnabled = false;
                           return;
                       }

                       if (ConfigManager.GameManager.IsDownloading || ConfigManager.GameManager.HasLaunchTask)
                       {
                           if (ConfigManager.GameManager.IsDownloading) ProgressBarShowAnim();

                           MainPlayButton.IsEnabled = false;
                           InstallationsList.IsEnabled = false;
                       }
                       else
                       {
                           ProgressBarGrid.Visibility = Visibility.Collapsed;
                           MainPlayButton.IsEnabled = true;
                           InstallationsList.IsEnabled = true;
                       }


                       if (selected.Version?.IsInstalled ?? false) PlayButtonText.SetResourceReference(TextBlock.TextProperty, "MainPage_PlayButton");
                       else PlayButtonText.SetResourceReference(TextBlock.TextProperty, "MainPage_PlayButton");
                   }));
            });
        }
        private void ProgressBarShowAnim()
        {
            ProgressBarGrid.Visibility = Visibility.Visible;
            ProgressBarText.Visibility = Visibility.Hidden;
            progressbarcontent.Visibility = Visibility.Hidden;
            ProgressBarText.Visibility = Visibility.Hidden;
            
            Storyboard storyboard = new Storyboard();
            ThicknessAnimation animation = new ThicknessAnimation
            {
                From = new Thickness(0, 70, 0, 55),
                To = new Thickness(0,-62,0,55),
                Duration = new Duration(TimeSpan.FromMilliseconds(350))
            };
            storyboard.Children.Add(animation);
            Storyboard.SetTargetProperty(animation, new PropertyPath(ProgressBar.MarginProperty));
            Storyboard.SetTarget(animation, ProgressBarGrid);
            storyboard.Completed += new EventHandler(ShowProgressBarContent);
            storyboard.Begin();
        }
        private void ShowProgressBarContent(object sender, EventArgs e)
        {
            ProgressBarText.Visibility = Visibility.Visible;
            progressbarcontent.Visibility = Visibility.Visible;
            ProgressBarText.Visibility = Visibility.Visible;
        }
        private void MainPlayButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CurrentInstallation = InstallationsList.SelectedIndex;
            Properties.Settings.Default.Save();
            var i = InstallationsList.SelectedItem as Installation;
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
        }
        private void GameManager_GameStateChanged(object sender, EventArgs e)
        {
            UpdatePlayButton();
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

        public void ResetButtonManager(ToggleButton toggleButton)
        {
            // just all buttons list
            // ya i know this is really bad, i need to learn mvvm instead of doing this shit
            // but this works fine, at least
            List<ToggleButton> toggleButtons = new List<ToggleButton>() { 
                // mainwindow
                ServersButton,
                NewsButton,
                BedrockEditionButton,
                JavaEditionButton,
                SettingsButton, 
                
                // mainpage (gonna be deleted)
                mainPage.PlayButton,
                mainPage.InstallationsButton,
                mainPage.SkinsButton,
                mainPage.PatchNotesButton,
                
                // settings screen lol
                settingsScreenPage.GeneralButton,
                settingsScreenPage.AccountsButton,
                settingsScreenPage.AboutButton
            };

            foreach (ToggleButton button in toggleButtons) { button.IsChecked = false; }

            PlayScreenBorder.Visibility = Visibility.Collapsed;

            toggleButton.IsChecked = true;
        }
        public void ButtonManager(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            ResetButtonManager(toggleButton);

            if (toggleButton.Name == BedrockEditionButton.Name) NavigateToMainPage();
            else if (toggleButton.Name == NewsButton.Name) NavigateToNewsPage();
            else if (toggleButton.Name == JavaEditionButton.Name) NavigateToJavaLauncher();
            else if (toggleButton.Name == ServersButton.Name) NavigateToServersScreen();
            else if (toggleButton.Name == SettingsButton.Name) NavigateToSettings();

            // MainPageButtons
            else if (toggleButton.Name == mainPage.PlayButton.Name) NavigateToPlayScreen();
            else if (toggleButton.Name == mainPage.InstallationsButton.Name) NavigateToInstallationsPage();
            else if (toggleButton.Name == mainPage.SkinsButton.Name) NavigateToSkinsPage();
            else if (toggleButton.Name == mainPage.PatchNotesButton.Name) NavigateToPatchNotes();
            else NavigateToPlayScreen();

        }

        public void NavigateToMainPage(bool rooted = false)
        {
            BedrockEditionButton.IsChecked = true;
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
                ServersButton.IsChecked = true;
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
            mainPage.LastButtonName = mainPage.SkinsButton.Name;
        }
        public void NavigateToPatchNotes()
        {
            NavigateToMainPage(true);
            mainPage.PatchNotesButton.IsChecked = true;
            mainPage.MainPageFrame.Navigate(noContentPage);
            mainPage.LastButtonName = mainPage.PatchNotesButton.Name;
        }

        public void NavigateToNewProfilePage()
        {
            MainWindowOverlayFrame.Navigate(new AddProfilePage());
        }


        #endregion
    }
}
