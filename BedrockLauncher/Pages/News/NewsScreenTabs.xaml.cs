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


        private News_Minecraft_Page communityNewsPage = new News_Minecraft_Page();
        private News_JavaDungeons_Page javaNewsPage = new News_JavaDungeons_Page();
        private News_MinecraftForums_Page forumsNewsPage = new News_MinecraftForums_Page();
        private News_Launcher_Page launcherNewsPage;

        private string LastTabName;

        public NewsScreenTabs(LauncherUpdater updater)
        {
            InitializeComponent();
            LastTabName = OfficalTab.Name;
            launcherNewsPage = new News_Launcher_Page(updater);
        }



        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => ButtonManager_Base(LastTabName));
        }

        #region Navigation

        private async void Navigate(object content)
        {
            bool animate = Properties.LauncherSettings.Default.AnimatePageTransitions;

            if (!animate)
            {
                await ContentFrame.Dispatcher.InvokeAsync(() => ContentFrame.Navigate(content));
                return;
            }

            int CurrentPageIndex = ViewModels.MainViewModel.Default.CurrentPageIndex_News;
            int LastPageIndex = ViewModels.MainViewModel.Default.LastPageIndex_News;
            if (CurrentPageIndex == LastPageIndex) return;

            ExpandDirection direction;

            if (CurrentPageIndex > LastPageIndex) direction = ExpandDirection.Right;
            else direction = ExpandDirection.Left;

            await Task.Run(() => BedrockLauncher.Components.PageAnimator.FrameSwipe(ContentFrame, content, direction));
        }

        public async void ResetButtonManager(string buttonName)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                // just all buttons list
                // ya i know this is really bad, i need to learn mvvm instead of doing this shit
                // but this works fine, at least
                List<ToggleButton> toggleButtons = new List<ToggleButton>() {
                OfficalTab,
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

        public async void ButtonManager(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                var toggleButton = sender as ToggleButton;
                string name = toggleButton.Name;
                ButtonManager_Base(name);
            });
        }

        public async void ButtonManager_Base(string senderName)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                ResetButtonManager(senderName);

                if (senderName == OfficalTab.Name) NavigateToCommunityNews();
                else if (senderName == JavaTab.Name) NavigateToJavaNews();
                else if (senderName == ForumsTab.Name) NavigateToForumNews();
                else if (senderName == LauncherTab.Name) NavigateToLauncherNews();
            });
        }



        public async void NavigateToCommunityNews()
        {
            ViewModels.MainViewModel.Default.UpdateNewsPageIndex(0);
            await Task.Run(() => Navigate(communityNewsPage));
            LastTabName = OfficalTab.Name;
        }

        public async void NavigateToJavaNews()
        {
            ViewModels.MainViewModel.Default.UpdateNewsPageIndex(1);
            await Task.Run(() => Navigate(javaNewsPage));
            LastTabName = JavaTab.Name;
        }

        public async void NavigateToForumNews()
        {
            ViewModels.MainViewModel.Default.UpdateNewsPageIndex(2);
            await Task.Run(() => Navigate(forumsNewsPage));
            LastTabName = ForumsTab.Name;
        }
        public async void NavigateToLauncherNews()
        {
            ViewModels.MainViewModel.Default.UpdateNewsPageIndex(3);
            await Task.Run(() => Navigate(launcherNewsPage));
            LastTabName = LauncherTab.Name;
        }


        #endregion
    }
}
