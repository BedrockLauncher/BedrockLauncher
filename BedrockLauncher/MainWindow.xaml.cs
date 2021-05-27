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
            this.updateButton.ClickBase.Click += LauncherModel.Updater.UpdateButton_Click;
            ButtonManager_Base(BedrockEditionButton.Name, false);
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

        private async void Navigate(object content, bool animate = true)
        {
            if (!animate)
            {
                await MainWindowFrame.Dispatcher.InvokeAsync(() => MainWindowFrame.Navigate(content));
                return;
            }
            int CurrentPageIndex = ViewModels.LauncherModel.Default.CurrentPageIndex;
            int LastPageIndex = ViewModels.LauncherModel.Default.LastPageIndex;
            if (CurrentPageIndex == LastPageIndex) return;

            ExpandDirection direction;

            if (CurrentPageIndex > LastPageIndex) direction = ExpandDirection.Down;
            else direction = ExpandDirection.Up;

            await Task.Run(() => Components.PageAnimator.FrameSwipe(MainWindowFrame, content, direction));
        }

        public void ResetButtonManager(string buttonName)
        {
            this.Dispatcher.Invoke(() =>
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
            });

        }
        public async void ButtonManager(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                var toggleButton = sender as ToggleButton;
                string name = toggleButton.Name;
                Task.Run(() => ButtonManager_Base(name));
            });
        }
        public void ButtonManager_Base(string senderName, bool animate = true)
        {
            this.Dispatcher.Invoke(() =>
            {
                ResetButtonManager(senderName);

                if (senderName == BedrockEditionButton.Name) NavigateToMainPage(animate);
                else if (senderName == NewsButton.Name) NavigateToNewsPage(animate);
                else if (senderName == JavaEditionButton.Name) NavigateToJavaLauncher();
                else if (senderName == ExternalLauncherButton.Name) NavigateToExternalLauncher();
                else if (senderName == CommunityButton.Name) NavigateToCommunityScreen(animate);
                else if (senderName == SettingsButton.Name) NavigateToSettings(animate);
            });

        }
        public void NavigateToNewsPage(bool animate = true)
        {
            LauncherModel.Default.UpdatePageIndex(0);
            NewsButton.Button.IsChecked = true;
            Task.Run(() => Navigate(newsScreenPage, animate));
        }

        public void NavigateToMainPage(bool animate = true)
        {
            LauncherModel.Default.UpdatePageIndex(1);
            BedrockEditionButton.Button.IsChecked = true;
            Task.Run(() => Navigate(mainPage, animate));
        }

        public void NavigateToCommunityScreen(bool animate = true)
        {
            LauncherModel.Default.UpdatePageIndex(2);
            CommunityButton.Button.IsChecked = true;
            Task.Run(() => Navigate(communityPage, animate));

        }
        public void NavigateToSettings(bool animate = true)
        {
            LauncherModel.Default.UpdatePageIndex(3);
            SettingsButton.Button.IsChecked = true;
            Task.Run(() => Navigate(settingsScreenPage, animate));
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
            LauncherModel.Default.SetOverlayFrame(new AddProfilePage(), true, false);
        }

        #endregion
    }
}
