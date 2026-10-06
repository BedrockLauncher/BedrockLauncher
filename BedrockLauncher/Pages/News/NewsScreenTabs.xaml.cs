using BedrockLauncher.Classes;
using BedrockLauncher.Handlers;
using BedrockLauncher.Pages.News.Launcher;
using BedrockLauncher.Pages.News.Official;
using BedrockLauncher.Pages.News.RSS;
using BedrockLauncher.UI.Components;
using CodeHollow.FeedReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;

namespace BedrockLauncher.Pages.News
{
    public partial class NewsScreenTabs : Page
    {
        private RSSNewsPage communityNewsPage = new RSSNewsPage(ViewModels.RSSViewModel.MinecraftCommunity);
        private RSSNewsPage forumsNewsPage = new RSSNewsPage(ViewModels.RSSViewModel.MinecraftForums);
        private LauncherNewsPage launcherNewsPage;

        private Navigator Navigator { get; set; } = new Navigator();

        private string LastTabName;

        public NewsScreenTabs()
        {
            InitializeComponent();

            LastTabName = ForumsTab.Name;
            launcherNewsPage = new LauncherNewsPage();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) => Task.Run(() => ButtonManager_Base(LastTabName));


        #region Navigation

        public void ResetButtonManager(string buttonName) => this.Dispatcher.Invoke(() =>
                                                                      {
                                                                          List<ToggleButton> toggleButtons = new List<ToggleButton>()
                                                                          {
                    ForumsTab,
                    LauncherTab
                                                                          };

                                                                          foreach (ToggleButton button in toggleButtons)
                                                                          {
                                                                              button.IsChecked = false;
                                                                          }

                                                                          ToggleButton selectedButton = toggleButtons
                                                                              .FirstOrDefault(x => x.Name == buttonName);

                                                                          if (selectedButton != null)
                                                                          {
                                                                              selectedButton.IsChecked = true;
                                                                          }
                                                                      });


        public void ButtonManager(object sender, RoutedEventArgs e) => this.Dispatcher.Invoke(() =>
                                                                                {
                                                                                    ToggleButton toggleButton = sender as ToggleButton;

                                                                                    if (toggleButton != null)
                                                                                    {
                                                                                        ButtonManager_Base(toggleButton.Name);
                                                                                    }
                                                                                });


        public void ButtonManager_Base(string senderName) => this.Dispatcher.Invoke(() =>
                                                                      {
                                                                          ResetButtonManager(senderName);

                                                                          if (senderName == ForumsTab.Name)
                                                                          {
                                                                              NavigateToForumNews();
                                                                          }
                                                                          else if (senderName == LauncherTab.Name)
                                                                          {
                                                                              NavigateToLauncherNews();
                                                                          }
                                                                      });


        public void NavigateToForumNews()
        {
            Navigator.UpdatePageIndex(1);

            Task.Run(() =>
                Navigator.Navigate(ContentFrame, forumsNewsPage)
            );

            LastTabName = ForumsTab.Name;
        }


        public void NavigateToLauncherNews()
        {
            Navigator.UpdatePageIndex(2);

            Task.Run(() =>
                Navigator.Navigate(ContentFrame, launcherNewsPage)
            );

            LastTabName = LauncherTab.Name;
        }


        #endregion


        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (LastTabName.Equals(LauncherTab.Name))
            {
                _ = launcherNewsPage.RefreshNews();
            }
            else if (LastTabName.Equals(ForumsTab.Name))
            {
                forumsNewsPage.RefreshNews();
            }
        }
    }
}