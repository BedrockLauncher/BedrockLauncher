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
using System.Collections.ObjectModel;
using RestSharp;
using BedrockLauncher.Pages.Common;
using ExtensionsDotNET.HTTP2;

namespace BedrockLauncher.Pages.News
{
    /// <summary>
    /// Interaction logic for News_Minecraft_Page.xaml
    /// </summary>
    public partial class News_Minecraft_Page : Page
    {

        private const string RSS_Feed = "https://www.minecraft.net/en-us/feeds/community-content/rss";

        private ObservableCollection<NewsItem> FeedItems { get; set; } = new ObservableCollection<NewsItem>();
        private bool hasPreloaded = false;


        public News_Minecraft_Page()
        {
            InitializeComponent();
            OfficalNewsFeed.ItemsSource = FeedItems;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(UpdateRSSContent);
        }

        private async void UpdateRSSContent()
        {

            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    NothingFound.PanelType = Controls.ResultPanelType.Loading;
                    NothingFound.Visibility = Visibility.Visible;
                    FeedItems.Clear();
                });


                string rss = string.Empty;
                using (var httpClient = new HttpClient(new Http2Handler())) rss = await httpClient.GetStringAsync(RSS_Feed);
                Feed feed = FeedReader.ReadFromString(rss);

                await Dispatcher.InvokeAsync(() =>
                {
                    foreach (FeedItem item in feed.Items)
                    {
                        NewsItem_MinecraftRSS new_item = new NewsItem_MinecraftRSS(item);
                        FeedItems.Add(new_item);
                        NothingFound.Visibility = Visibility.Collapsed;
                    }

                    if (FeedItems.Count == 0) NothingFound.Visibility = Visibility.Visible;
                });
            }
            catch
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    NothingFound.PanelType = Controls.ResultPanelType.Error;
                    NothingFound.Visibility = Visibility.Visible;
                });
            }

        }

        private void Page_Loaded(object sender, EventArgs e)
        {
            if (!hasPreloaded)
            {
                Task.Run(UpdateRSSContent);
                hasPreloaded = true;
            }

        }

        private void OfficalNewsFeed_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (OfficalNewsFeed.SelectedItem != null)
                {
                    var item = OfficalNewsFeed.SelectedItem as NewsItem_MinecraftRSS;
                    FeedItem_Minecraft.LoadArticle(item);
                }
            }
        }
    }
}
