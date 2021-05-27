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
using BedrockLauncher.Methods;
using CodeHollow.FeedReader;
using BedrockLauncher.Classes;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using BedrockLauncher.Downloaders;

namespace BedrockLauncher.Pages.News
{
    public partial class NewsScreenTabs : Page
    {
        private CommunityNewsPage communityNewsPage = new CommunityNewsPage();
        private LauncherNewsPage launcherNewsPage;

        private string LastTabName;

        public NewsScreenTabs(LauncherUpdater updater)
        {
            InitializeComponent();
            LastTabName = OfficalTab.Name;
            launcherNewsPage = new LauncherNewsPage(updater);
        }



        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ButtonManager_Base(LastTabName, false);
        }

        #region Navigation

        private async void Navigate(object content, bool animate = true)
        {
            if (!animate)
            {
                await ContentFrame.Dispatcher.InvokeAsync(() => ContentFrame.Navigate(content));
                return;
            }

            int CurrentPageIndex = ViewModels.LauncherModel.Default.CurrentPageIndex_News;
            int LastPageIndex = ViewModels.LauncherModel.Default.LastPageIndex_News;
            if (CurrentPageIndex == LastPageIndex) return;

            ExpandDirection direction;

            if (CurrentPageIndex > LastPageIndex) direction = ExpandDirection.Right;
            else direction = ExpandDirection.Left;

            await Task.Run(() => Components.PageAnimator.FrameSwipe(ContentFrame, content, direction));
        }

        public void ResetButtonManager(string buttonName)
        {
            this.Dispatcher.Invoke(() =>
            {
                // just all buttons list
                // ya i know this is really bad, i need to learn mvvm instead of doing this shit
                // but this works fine, at least
                List<ToggleButton> toggleButtons = new List<ToggleButton>() {
                LauncherTab,
                OfficalTab
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

                if (senderName == LauncherTab.Name) NavigateToLauncherNews(animate);
                else if (senderName == OfficalTab.Name) NavigateToCommunityNews(animate);
            });
        }

        public void NavigateToLauncherNews(bool animate = true)
        {
            ViewModels.LauncherModel.Default.UpdateNewsPageIndex(1);
            Task.Run(() => Navigate(launcherNewsPage, animate));
            LastTabName = LauncherTab.Name;
        }

        public void NavigateToCommunityNews(bool animate = true)
        {
            ViewModels.LauncherModel.Default.UpdateNewsPageIndex(0);
            Task.Run(() => Navigate(communityNewsPage, animate));
            LastTabName = OfficalTab.Name;
        }


        #endregion
    }
}
