using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media.Animation;
using BedrockLauncher.Classes;
using BedrockLauncher.Downloaders;
using BedrockLauncher.Methods;

namespace BedrockLauncher.Pages.Play
{
    public partial class GameTabs : Page
    {
        private static ChangelogDownloader PatchNotesDownloader = new ChangelogDownloader();

        public PlayScreenPage playScreenPage = new PlayScreenPage();
        public InstallationsScreen installationsScreen = new InstallationsScreen();
        public SkinsPage skinsPage = new SkinsPage();
        public PatchNotesPage patchNotesPage = new PatchNotesPage(PatchNotesDownloader);

        public GameTabs()
        {
            InitializeComponent();
        }

        private async void Navigate(object content, bool animate = true)
        {
            if (!animate)
            {
                await MainPageFrame.Dispatcher.InvokeAsync(() => MainPageFrame.Navigate(content));
                return;
            }
            int CurrentPageIndex = ViewModels.LauncherModel.Default.CurrentPageIndex_Play;
            int LastPageIndex = ViewModels.LauncherModel.Default.LastPageIndex_Play;
            if (CurrentPageIndex == LastPageIndex) return;

            ExpandDirection direction;

            if (CurrentPageIndex > LastPageIndex) direction = ExpandDirection.Right;
            else direction = ExpandDirection.Left;

            await Task.Run(() => Components.PageAnimator.FrameSwipe(MainPageFrame, content, direction));
        }



        #region Navigation

        public void ResetButtonManager(string buttonName)
        {
            this.Dispatcher.Invoke(() =>
            {
                // just all buttons list
                // ya i know this is really bad, i need to learn mvvm instead of doing this shit
                // but this works fine, at least
                List<ToggleButton> toggleButtons = new List<ToggleButton>() {
                PlayButton,
                InstallationsButton,
                SkinsButton,
                PatchNotesButton
            };

                foreach (ToggleButton button in toggleButtons) { button.IsChecked = false; }

                if (toggleButtons.Exists(x => x.Name == buttonName))
                {
                    toggleButtons.Where(x => x.Name == buttonName).FirstOrDefault().IsChecked = true;
                }
            });

        }

        public async void ButtonManager2(object sender, RoutedEventArgs e)
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

                if (senderName == PlayButton.Name) NavigateToPlayScreen(animate);
                else if (senderName == InstallationsButton.Name) NavigateToInstallationsPage(animate);
                else if (senderName == SkinsButton.Name) NavigateToSkinsPage(animate);
                else if (senderName == PatchNotesButton.Name) NavigateToPatchNotes(animate);
            });
        }

        public void NavigateToPlayScreen(bool animate = true)
        {
            ViewModels.LauncherModel.Default.UpdatePlayPageIndex(0);
            PlayButton.IsChecked = true;
            Task.Run(() => Navigate(playScreenPage, animate));

        }
        public void NavigateToInstallationsPage(bool animate = true)
        {
            ViewModels.LauncherModel.Default.UpdatePlayPageIndex(1);
            InstallationsButton.IsChecked = true;
            Task.Run(() => Navigate(installationsScreen, animate));
        }
        public void NavigateToSkinsPage(bool animate = true)
        {
            ViewModels.LauncherModel.Default.UpdatePlayPageIndex(2);
            SkinsButton.IsChecked = true;
            Task.Run(() => Navigate(skinsPage, animate));
        }
        public void NavigateToPatchNotes(bool animate = true)
        {
            ViewModels.LauncherModel.Default.UpdatePlayPageIndex(3);
            PatchNotesButton.IsChecked = true;
            Task.Run(() => Navigate(patchNotesPage, animate));
        }

        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ResetButtonManager(null);
            ButtonManager_Base(PlayButton.Name, false);
        }
    }
}
