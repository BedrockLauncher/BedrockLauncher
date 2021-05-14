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

namespace BedrockLauncher.Pages.News
{
    /// <summary>
    /// Логика взаимодействия для News.xaml
    /// </summary>
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
            ButtonManager_Base(LastTabName);
        }

        #region Navigation

        public void ResetButtonManager(string buttonName)
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
        }

        public void ButtonManager(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            ButtonManager_Base(toggleButton.Name);
        }

        public void ButtonManager_Base(string senderName)
        {
            ResetButtonManager(senderName);

            if (senderName == LauncherTab.Name) NavigateToLauncherNews();
            else if (senderName == OfficalTab.Name) NavigateToCommunityNews();
        }

        public void NavigateToLauncherNews()
        {
            ContentFrame.Navigate(launcherNewsPage);
            LastTabName = LauncherTab.Name;
        }

        public void NavigateToCommunityNews()
        {
            ContentFrame.Navigate(communityNewsPage);
            LastTabName = OfficalTab.Name;
        }


        #endregion
    }
}
