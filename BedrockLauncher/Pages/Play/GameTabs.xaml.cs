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

        private async void Navigate(object content)
        {
            bool animate = Properties.LauncherSettings.Default.AnimatePageTransitions;

            if (!animate)
            {
                await MainPageFrame.Dispatcher.InvokeAsync(() => MainPageFrame.Navigate(content));
                return;
            }
            int CurrentPageIndex = ViewModels.MainViewModel.Default.CurrentPageIndex_Play;
            int LastPageIndex = ViewModels.MainViewModel.Default.LastPageIndex_Play;
            if (CurrentPageIndex == LastPageIndex) return;

            ExpandDirection direction;

            if (CurrentPageIndex > LastPageIndex) direction = ExpandDirection.Right;
            else direction = ExpandDirection.Left;

            await Task.Run(() => BedrockLauncher.Components.PageAnimator.FrameSwipe(MainPageFrame, content, direction));
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

        public void ButtonManager_Base(string senderName)
        {
            this.Dispatcher.Invoke(() =>
            {
                ResetButtonManager(senderName);

                if (senderName == PlayButton.Name) NavigateToPlayScreen();
                else if (senderName == InstallationsButton.Name) NavigateToInstallationsPage();
                else if (senderName == SkinsButton.Name) NavigateToSkinsPage();
                else if (senderName == PatchNotesButton.Name) NavigateToPatchNotes();
            });
        }

        public void NavigateToPlayScreen()
        {
            ViewModels.MainViewModel.Default.UpdatePlayPageIndex(0);
            PlayButton.IsChecked = true;
            Task.Run(() => Navigate(playScreenPage));

        }
        public void NavigateToInstallationsPage()
        {
            ViewModels.MainViewModel.Default.UpdatePlayPageIndex(1);
            InstallationsButton.IsChecked = true;
            Task.Run(() => Navigate(installationsScreen));
        }
        public void NavigateToSkinsPage()
        {
            ViewModels.MainViewModel.Default.UpdatePlayPageIndex(2);
            SkinsButton.IsChecked = true;
            Task.Run(() => Navigate(skinsPage));
        }
        public void NavigateToPatchNotes()
        {
            ViewModels.MainViewModel.Default.UpdatePlayPageIndex(3);
            PatchNotesButton.IsChecked = true;
            Task.Run(() => Navigate(patchNotesPage));
        }

        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ResetButtonManager(null);
            ButtonManager_Base(PlayButton.Name);
        }
    }
}
