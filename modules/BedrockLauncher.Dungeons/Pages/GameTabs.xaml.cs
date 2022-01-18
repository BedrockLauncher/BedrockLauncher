using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media.Animation;
using BedrockLauncher.UI.Pages.Common;

namespace BedrockLauncher.Dungeons.Pages
{
    public partial class GameTabs : Page
    {
        public int CurrentPageIndex_Play { get; set; } = -1;
        public int LastPageIndex_Play { get; set; } = -1;

        public void UpdatePlayPageIndex(int index)
        {
            LastPageIndex_Play = CurrentPageIndex_Play;
            CurrentPageIndex_Play = index;
        }

        public PlayPage playPage = new PlayPage();
        public InstallationPage installationPage = new InstallationPage();
        public ModsPage modsPage = new ModsPage();
        public PatchNotesPage patchNotesPage = new PatchNotesPage(new Downloaders.ChangelogDownloader());

        public GameTabs()
        {
            InitializeComponent();
        }

        private async void Navigate(object content)
        {
            bool animate = true;

            if (!animate)
            {
                await MainPageFrame.Dispatcher.InvokeAsync(() => MainPageFrame.Navigate(content));
                return;
            }
            int CurrentPageIndex = CurrentPageIndex_Play;
            int LastPageIndex = LastPageIndex_Play;
            if (CurrentPageIndex == LastPageIndex) return;

            ExpandDirection direction;

            if (CurrentPageIndex > LastPageIndex) direction = ExpandDirection.Right;
            else direction = ExpandDirection.Left;

            await Task.Run(() => BedrockLauncher.UI.Components.PageAnimator.FrameSwipe(MainPageFrame, content, direction));
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
                InstallationButton,
                ModsButton,
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

                if (senderName == PlayButton.Name) NavigateToPlayPage();
                else if (senderName == InstallationButton.Name) NavigateToInstallationPage();
                else if (senderName == ModsButton.Name) NavigateToModsPage();
                else if (senderName == PatchNotesButton.Name) NavigateToPatchNotes();
            });
        }

        public void NavigateToPlayPage()
        {
            UpdatePlayPageIndex(0);
            PlayButton.IsChecked = true;
            Task.Run(() => Navigate(playPage));

        }
        public void NavigateToInstallationPage()
        {
            UpdatePlayPageIndex(1);
            InstallationButton.IsChecked = true;
            Task.Run(() => Navigate(installationPage));
        }
        public void NavigateToModsPage()
        {
            UpdatePlayPageIndex(2);
            ModsButton.IsChecked = true;
            Task.Run(() => Navigate(modsPage));
        }
        public void NavigateToPatchNotes()
        {
            UpdatePlayPageIndex(3);
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
