using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media.Animation;
using BedrockLauncher.Classes;
using BedrockLauncher.Downloaders;
using BedrockLauncher.Pages.Play.CreatorTools;
using BedrockLauncher.Pages.Play.Home;
using BedrockLauncher.Pages.Play.Installations;
using BedrockLauncher.Pages.Play.PatchNotes;
using BedrockLauncher.UI.Components;

namespace BedrockLauncher.Pages.Play
{
    public partial class GameTabs : Page
    {
        private static ChangelogDownloader PatchNotesDownloader = new ChangelogDownloader();

        public PlayScreenPage playScreenPage = new PlayScreenPage();
        public InstallationsScreen installationsScreen = new InstallationsScreen();
        public CreatorToolsPage creatorToolsPage = new CreatorToolsPage();
        public PatchNotesPage patchNotesPage = new PatchNotesPage(PatchNotesDownloader);

        private Navigator Navigator { get; set; } = new Navigator();

        public GameTabs()
        {
            InitializeComponent();
        }



        #region Navigation

        public void ResetButtonManager(string buttonName)
        {
            this.Dispatcher.Invoke(() =>
            {
                // just all buttons list
                // ya i know this is really bad, i need to learn mvvm instead of doing this shit
                // but this works fine, at least
                ToggleButton[] toggleButtons = new ToggleButton[] {
                PlayButton,
                CreatorToolsButton,
                InstallationsButton,
                PatchNotesButton
            };

                foreach (ToggleButton button in toggleButtons)
                {
                    button.IsChecked = button.Name == buttonName;
                }
            });

        }

        public void ButtonManager2(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
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
                else if (senderName == CreatorToolsButton.Name) NavigateToCreatorToolsPage();
                else if (senderName == PatchNotesButton.Name) NavigateToPatchNotes();
            });
        }

        public void NavigateToPlayScreen()
        {
            Navigator.UpdatePageIndex(0);
            Task.Run(() => Navigator.Navigate(MainPageFrame, playScreenPage));

        }
        public void NavigateToInstallationsPage()
        {
            Navigator.UpdatePageIndex(1);
            Task.Run(() => Navigator.Navigate(MainPageFrame, installationsScreen));
        }

        public void NavigateToCreatorToolsPage()
        {
            Navigator.UpdatePageIndex(2);
            Task.Run(() => Navigator.Navigate(MainPageFrame, creatorToolsPage));
        }

        public void NavigateToPatchNotes()
        {
            Navigator.UpdatePageIndex(3);
            Task.Run(() => Navigator.Navigate(MainPageFrame, patchNotesPage));
        }

        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ResetButtonManager(null);
            ButtonManager_Base(PlayButton.Name);
        }
    }
}
