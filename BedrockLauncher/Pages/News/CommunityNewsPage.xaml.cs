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
using BedrockLauncher.Controls.Items;
using System.Net;
using System.Net.Http;

namespace BedrockLauncher.Pages.News
{
    /// <summary>
    /// Interaction logic for CommunityNewsPage.xaml
    /// </summary>
    public partial class CommunityNewsPage : Page
    {

        private const string RSS_Feed = @"https://www.minecraft.net/en-us/feeds/community-content/rss";


        public CommunityNewsPage()
        {
            InitializeComponent();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => UpdateRSSContent());
        }

        private async void UpdateRSSContent()
        {
            try
            {
                await Dispatcher.InvokeAsync(() => {
                    NothingFound.Visibility = Visibility.Collapsed;
                    OfficalNewsFeed.Items.Clear(); 
                });

                var feed = await FeedReader.ReadAsync(RSS_Feed);

                await Dispatcher.InvokeAsync(() => {
                    foreach (FeedItem item in feed.Items)
                    {
                        MCNetFeedItemRSS new_item = new MCNetFeedItemRSS(item);
                        OfficalNewsFeed.Items.Add(new_item);
                    }
                });
            }
            catch
            {
                await Dispatcher.InvokeAsync(() => {
                    NothingFound.Visibility = Visibility.Visible;
                });
            }

        }

        private async void Page_Loaded(object sender, EventArgs e)
        {
            await Task.Run(() => UpdateRSSContent());
        }

        private void OfficalNewsFeed_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (OfficalNewsFeed.SelectedItem != null)
                {
                    var item = OfficalNewsFeed.SelectedItem as MCNetFeedItemRSS;
                    CommunityFeedItem.LoadArticle(item);
                }
            }
        }
    }
}
