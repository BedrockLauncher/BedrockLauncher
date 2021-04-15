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

namespace BedrockLauncher.Pages
{
    /// <summary>
    /// Логика взаимодействия для PlayScreenPage.xaml
    /// </summary>
    public partial class NewsScreenPage : Page
    {
        private LauncherUpdater updater;
        private const string RSS_Feed = @"https://www.minecraft.net/en-us/feeds/community-content/rss";
        public NewsScreenPage(LauncherUpdater updater)
        {
            InitializeComponent();
            this.updater = updater;
        }

        private async void UpdateRSSContent()
        {
            Dispatcher.Invoke(() => { OfficalNewsFeed.Items.Clear(); });

            var feed = await FeedReader.ReadAsync(RSS_Feed);

            Dispatcher.Invoke(() => {
                foreach (FeedItem item in feed.Items)
                {
                    MCNetFeedItem new_item = new MCNetFeedItem(item);
                    OfficalNewsFeed.Items.Add(new_item);
                }
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            buildVersion.Text = "v" + updater.getLatestTag();
            buildChanges.Text = updater.getLatestTagDescription();
            UpdateRSSContent();
        }

        private void FeedItemButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            MCNetFeedItem item = button.DataContext as MCNetFeedItem;
            Process.Start(new ProcessStartInfo(item.Link));
        }
    }
}
