using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CodeHollow.FeedReader;
using BedrockLauncher.Classes;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using BedrockLauncher.Handlers;
using BedrockLauncher.UI.Components;
using BedrockLauncher.Pages.News.RSS;
using BedrockLauncher.Pages.News.Offical;
using BedrockLauncher.Pages.News.Launcher;

namespace BedrockLauncher.Pages.News
{
    public partial class NewsScreenTabs : Page
    {


        private RSSNewsPage communityNewsPage = new RSSNewsPage(ViewModels.RSSViewModel.MinecraftCommunity);
        private OfficalNewsPage javaNewsPage = new OfficalNewsPage();
        private RSSNewsPage forumsNewsPage = new RSSNewsPage(ViewModels.RSSViewModel.MinecraftForums);
        private LauncherNewsPage launcherNewsPage;

        private Navigator Navigator { get; set; } = new Navigator();

        private string LastTabName;

        public NewsScreenTabs()
        {
            InitializeComponent();
            LastTabName = JavaTab.Name;
            launcherNewsPage = new LauncherNewsPage();
        }



        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
             Task.Run(() => ButtonManager_Base(LastTabName));
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
                JavaTab,
                ForumsTab,
                LauncherTab
            };

                foreach (ToggleButton button in toggleButtons) { button.IsChecked = false; }

                if (toggleButtons.Exists(x => x.Name == buttonName))
                {
                    toggleButtons.Where(x => x.Name == buttonName).FirstOrDefault().IsChecked = true;
                }
            });

        }

        public void ButtonManager(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                var toggleButton = sender as ToggleButton;
                string name = toggleButton.Name;
                ButtonManager_Base(name);
            });
        }

        public void ButtonManager_Base(string senderName)
        {
            this.Dispatcher.Invoke(() =>
            {
                ResetButtonManager(senderName);

                if (senderName == JavaTab.Name) NavigateToJavaNews();
                else if (senderName == ForumsTab.Name) NavigateToForumNews();
                else if (senderName == LauncherTab.Name) NavigateToLauncherNews();
            });
        }

        public void NavigateToJavaNews()
        {
            Navigator.UpdatePageIndex(1);
            Task.Run(() => Navigator.Navigate(ContentFrame, javaNewsPage));
            LastTabName = JavaTab.Name;
        }

        public void NavigateToForumNews()
        {
            Navigator.UpdatePageIndex(2);
            Task.Run(() => Navigator.Navigate(ContentFrame, forumsNewsPage));
            LastTabName = ForumsTab.Name;
        }
        public void NavigateToLauncherNews()
        {
            Navigator.UpdatePageIndex(3);
            Task.Run(() => Navigator.Navigate(ContentFrame, launcherNewsPage));
            LastTabName = LauncherTab.Name;
        }


        #endregion

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {

            if (LastTabName.Equals(LauncherTab.Name)) _ = launcherNewsPage.RefreshNews();
            else if (LastTabName.Equals(ForumsTab.Name)) forumsNewsPage.RefreshNews();
            else if (LastTabName.Equals(JavaTab.Name)) javaNewsPage.RefreshNews();
        }
    }
}
