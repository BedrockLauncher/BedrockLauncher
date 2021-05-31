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
    /// Interaction logic for Java_N_DungeonsNewsPage.xaml
    /// </summary>
    public partial class Java_N_DungeonsNewsPage : Page
    {

        private const string JSON_Feed = @"https://launchercontent.mojang.com/news.json";


        public async Task<MCNetLauncherFeed> GetJSON()
        {
            MCNetLauncherFeed result = null;
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var json = await httpClient.GetStringAsync(JSON_Feed);
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<MCNetLauncherFeed>(json);
                }
                catch
                {
                    result = new MCNetLauncherFeed();
                }

            }
            if (result == null) result = new MCNetLauncherFeed();
            return result;
        }

        public Java_N_DungeonsNewsPage()
        {
            InitializeComponent();
        }

        private async void UpdateRSSContent()
        {
            Dispatcher.Invoke(() => { OfficalNewsFeed.Items.Clear(); });

            var news = await GetJSON();

            Dispatcher.Invoke(() => {
                foreach (MCNetFeedItemJSON item in news.entries)
                {
                    if (item.newsType != null && item.newsType.Contains("News page")) OfficalNewsFeed.Items.Add(new JavaFeedItem(item));
                }
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateRSSContent();
        }

        private void OfficalNewsFeed_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (OfficalNewsFeed.SelectedItem != null)
                {
                    var item = OfficalNewsFeed.SelectedItem as MCNetFeedItemJSON;
                    JavaFeedItem.LoadArticle(item);
                }
            }
        }
    }
}
