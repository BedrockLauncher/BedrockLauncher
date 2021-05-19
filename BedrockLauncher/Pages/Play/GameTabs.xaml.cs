using System;
using System.Collections.Generic;
using System.Linq;
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
            MainPageFrame.Navigating += MainPageFrame_Navigating;
        }

        private void MainPageFrame_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            int CurrentPageIndex = ViewModels.LauncherModel.Default.CurrentPageIndex_Play;
            int LastPageIndex = ViewModels.LauncherModel.Default.LastPageIndex_Play;
            if (CurrentPageIndex == LastPageIndex) return;

            ExpandDirection direction;

            if (CurrentPageIndex > LastPageIndex) direction = ExpandDirection.Right;
            else direction = ExpandDirection.Left;

            Components.PageAnimator.FrameSwipe(MainPageFrame, MainPageFrame.Content, direction);
        }



        #region Navigation

        public void ResetButtonManager(ToggleButton toggleButton)
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
            if (toggleButton != null) toggleButton.IsChecked = true;
        }

        public void ButtonManager2(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            ResetButtonManager(toggleButton);

            if (toggleButton.Name == PlayButton.Name) NavigateToPlayScreen();
            else if (toggleButton.Name == InstallationsButton.Name) NavigateToInstallationsPage();
            else if (toggleButton.Name == SkinsButton.Name) NavigateToSkinsPage();
            else if (toggleButton.Name == PatchNotesButton.Name) NavigateToPatchNotes();
        }

        public void NavigateToPlayScreen()
        {
            ViewModels.LauncherModel.Default.UpdatePlayPageIndex(0);
            PlayButton.IsChecked = true;
            MainPageFrame.Navigate(playScreenPage);

        }
        public void NavigateToInstallationsPage()
        {
            ViewModels.LauncherModel.Default.UpdatePlayPageIndex(1);
            InstallationsButton.IsChecked = true;
            MainPageFrame.Navigate(installationsScreen);
        }
        public void NavigateToSkinsPage()
        {
            ViewModels.LauncherModel.Default.UpdatePlayPageIndex(2);
            SkinsButton.IsChecked = true;
            MainPageFrame.Navigate(skinsPage);
        }
        public void NavigateToPatchNotes()
        {
            ViewModels.LauncherModel.Default.UpdatePlayPageIndex(3);
            PatchNotesButton.IsChecked = true;
            MainPageFrame.Navigate(patchNotesPage);
        }

        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ResetButtonManager(null);
            NavigateToPlayScreen();
        }
    }
}
