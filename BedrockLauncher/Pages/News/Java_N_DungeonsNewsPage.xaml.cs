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
using ExtensionsDotNET;

namespace BedrockLauncher.Pages.News
{
    /// <summary>
    /// Interaction logic for Java_N_DungeonsNewsPage.xaml
    /// </summary>
    public partial class Java_N_DungeonsNewsPage : Page
    {
        private const string JSON_Feed = @"https://launchercontent.mojang.com/news.json";
        private List<MCNetFeedItem> FeedItems { get; set; } = new List<MCNetFeedItem>();

        public Java_N_DungeonsNewsPage()
        {
            InitializeComponent();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await UpdateJSONContent();
            await Task.Run(UpdateContent);
        }

        public async Task UpdateJSONContent()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                this.FeedItems.Clear();
            });

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
            if (result.entries == null) result.entries = new List<MCNetFeedItemJSON>();

            await Dispatcher.InvokeAsync(() => {
                foreach (MCNetFeedItemJSON item in result.entries)
                {
                    if (item.newsType != null && item.newsType.Contains("News page")) FeedItems.Add(item);
                }
            });
        }

        private async void Page_Loaded(object sender, EventArgs e)
        {
            await Task.Run(() => RefreshButton_Click(sender, null));
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

        private async void UpdateContent()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                OfficalNewsFeed.Items.Clear();
                foreach (var item in FeedItems.Where(x => OfficalNewsFeed_FeedFilter(x))) OfficalNewsFeed.Items.Add(item);

                if (OfficalNewsFeed.Items.Count == 0) NothingFound.Visibility = Visibility.Visible;
                else NothingFound.Visibility = Visibility.Collapsed;
            });
        }

        private bool OfficalNewsFeed_FeedFilter(object obj)
        {
            if (!(obj is MCNetFeedItemJSON)) return false;
            else
            {
                var item = (obj as MCNetFeedItemJSON);
                if (item.newsType != null && item.newsType.Contains("News page"))
                {
                    if (item.category == "Minecraft: Java Edition" && ShowJavaContent.IsChecked.Value) return ContainsText(item);
                    else if (item.category == "Minecraft Dungeons" && ShowDungeonsContent.IsChecked.Value) return ContainsText(item);
                    else return false;
                }
                else return false;
            }

            bool ContainsText(MCNetFeedItemJSON _item)
            {
                string searchParam = SearchBox.Text;
                if (string.IsNullOrEmpty(searchParam) || _item.title.Contains(searchParam, StringComparison.OrdinalIgnoreCase)) return true;
                else return false;
            }
        }

        private async void CheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;
            await Task.Run(UpdateContent);
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.IsInitialized) return;
            await Task.Run(UpdateContent);
        }
    }
}
