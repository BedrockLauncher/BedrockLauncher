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
        private Java_N_DungeonsNewsPage javaNewsPage = new Java_N_DungeonsNewsPage();

        private string LastTabName;

        public NewsScreenTabs(LauncherUpdater updater)
        {
            InitializeComponent();
            LastTabName = OfficalTab.Name;
            launcherNewsPage = new LauncherNewsPage(updater);
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

            int CurrentPageIndex = ViewModels.LauncherModel.Default.CurrentPageIndex_News;
            int LastPageIndex = ViewModels.LauncherModel.Default.LastPageIndex_News;
            if (CurrentPageIndex == LastPageIndex) return;

            ExpandDirection direction;

            if (CurrentPageIndex > LastPageIndex) direction = ExpandDirection.Right;
            else direction = ExpandDirection.Left;

            await Task.Run(() => BL_Core.Components.PageAnimator.FrameSwipe(ContentFrame, content, direction));
        }

        public async void ResetButtonManager(string buttonName)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                // just all buttons list
                // ya i know this is really bad, i need to learn mvvm instead of doing this shit
                // but this works fine, at least
                List<ToggleButton> toggleButtons = new List<ToggleButton>() {
                LauncherTab,
                JavaTab,
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
                ButtonManager_Base(name);
            });
        }

        public async void ButtonManager_Base(string senderName)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                ResetButtonManager(senderName);

                if (senderName == LauncherTab.Name) NavigateToLauncherNews();
                else if (senderName == OfficalTab.Name) NavigateToCommunityNews();
                else if (senderName == JavaTab.Name) NavigateToJavaNews();
            });
        }

        public async void NavigateToLauncherNews()
        {
            ViewModels.LauncherModel.Default.UpdateNewsPageIndex(2);
            await Task.Run(() => Navigate(launcherNewsPage));
            LastTabName = LauncherTab.Name;
        }

        public async void NavigateToJavaNews()
        {
            ViewModels.LauncherModel.Default.UpdateNewsPageIndex(1);
            await Task.Run(() => Navigate(javaNewsPage));
            LastTabName = JavaTab.Name;
        }

        public async void NavigateToCommunityNews()
        {
            ViewModels.LauncherModel.Default.UpdateNewsPageIndex(0);
            await Task.Run(() => Navigate(communityNewsPage));
            LastTabName = OfficalTab.Name;
        }


        #endregion
    }
}
