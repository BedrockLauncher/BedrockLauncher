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
using BedrockLauncher.Pages.Settings;
using BedrockLauncher.Pages.Play;
using BedrockLauncher.Pages.News;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.Pages.Common;
using BedrockLauncher.Pages.Community;

namespace BedrockLauncher
{
    //TODO: Improve backup overwrite response (to avoid having to click it forever)
    //TODO: Add translation support to the remaining entries in GameManager.cs
    //TODO: (Later On) Better Animations
    //TODO: (Later On) Community Content / Personal Donations Section

    public partial class MainWindow : Window
    {
        private GameTabs mainPage = new GameTabs();
        private SettingsTabs settingsScreenPage = new SettingsTabs();
        private NewsScreenTabs newsScreenPage = new NewsScreenTabs(ViewModels.LauncherModel.Updater);
        private CommunityPage communityPage = new CommunityPage();

        private NoContentPage noContentPage = new NoContentPage();

        public MainWindow()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime) Init();
        }
        private void Init()
        {
            Panel.SetZIndex(OverlayFrame, 0);
            Panel.SetZIndex(ErrorFrame, 1);
            Panel.SetZIndex(updateButton, 2);

            BL_Core.Language.LanguageManager.Init();
            LauncherModel.Default.Init();
            this.MainWindowFrame.Navigating += MainWindowFrame_Navigating;
            this.updateButton.ClickBase.Click += LauncherModel.Updater.UpdateButton_Click;
        }

        private void MainWindowFrame_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            int CurrentPageIndex = ViewModels.LauncherModel.Default.CurrentPageIndex;
            int LastPageIndex = ViewModels.LauncherModel.Default.LastPageIndex;
            if (CurrentPageIndex == LastPageIndex) return;

            ExpandDirection direction;

            if (CurrentPageIndex > LastPageIndex) direction = ExpandDirection.Down;
            else direction = ExpandDirection.Up;

            Components.PageAnimator.FrameSwipe(MainWindowFrame, MainWindowFrame.Content, direction);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ViewModels.LauncherModel.Default.AttemptClose(sender, e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.GameManager.Cancel();
        }

        #region Navigation

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
            };

            foreach (ToggleButton button in toggleButtons) { button.IsChecked = false; }

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
        }
        public void NavigateToNewsPage()
        {
            LauncherModel.Default.UpdatePageIndex(0);
            MainWindowFrame.Navigate(newsScreenPage);
            NewsButton.Button.IsChecked = true;
        }

        public void NavigateToMainPage()
        {
            LauncherModel.Default.UpdatePageIndex(1);
            BedrockEditionButton.Button.IsChecked = true;
            MainWindowFrame.Navigate(mainPage);
        }

        public void NavigateToCommunityScreen()
        {
            LauncherModel.Default.UpdatePageIndex(2);
            CommunityButton.Button.IsChecked = true;
            MainWindowFrame.Navigate(communityPage);

        }
        public void NavigateToSettings()
        {
            LauncherModel.Default.UpdatePageIndex(3);
            SettingsButton.Button.IsChecked = true;
            MainWindowFrame.Navigate(settingsScreenPage);
        }

        public void NavigateToJavaLauncher()
        {
            Action action = new Action(() =>
            {
                try
                {
                    // Trying to find and open Java launcher shortcut
                    string JavaPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + @"\Programs\Minecraft Launcher\Minecraft Launcher";
                    Process.Start(JavaPath);
                    Application.Current.MainWindow.Close();
                }
                catch
                {
                    mainPage.NavigateToPlayScreen();
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
                    mainPage.NavigateToPlayScreen();
                    ErrorScreenShow.errormsg("CantFindExternalLauncher");
                }
            });

            NavigateToOtherLauncher(action);
        }
        public async void NavigateToOtherLauncher(Action action)
        {
            if (Properties.LauncherSettings.Default.CloseLauncherOnSwitch && ConfigManager.GameManager.GameProcess != null)
            {
                await Task.Run(() => LauncherModel.Default.ShowPrompt_ClosingWithGameStillOpened(action));
            }
            else action.Invoke();
        }
        public void NavigateToNewProfilePage()
        {
            LauncherModel.Default.SetOverlayFrame(new AddProfilePage());
        }

        #endregion
    }
}
