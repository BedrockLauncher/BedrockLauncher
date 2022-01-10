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
    /// Interaction logic for News_JavaDungeons_Page.xaml
    /// </summary>
    public partial class News_JavaDungeons_Page : Page
    {
        private const string JSON_Feed = @"https://launchercontent.mojang.com/news.json";
        private ObservableCollection<NewsItem> FeedItems { get; set; } = new ObservableCollection<NewsItem>();
        private bool hasPreloaded = false;

        public News_JavaDungeons_Page()
        {
            InitializeComponent();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
             Task.Run(() => UpdateContent(true));
        }

        public async Task UpdateJSONContent()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                this.FeedItems.Clear();
            });

            LauncherNewsFeed result = null;
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var json = await httpClient.GetStringAsync(JSON_Feed);
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<LauncherNewsFeed>(json);
                }
                catch
                {
                    result = new LauncherNewsFeed();
                }

            }
            if (result == null) result = new LauncherNewsFeed();
            if (result.entries == null) result.entries = new List<NewsItem_Launcher>();

            await Dispatcher.InvokeAsync(() => {
                foreach (NewsItem_Launcher item in result.entries)
                {
                    if (item.newsType != null && item.newsType.Contains("News page")) FeedItems.Add(item);
                }
            });
        }

        private void Page_Loaded(object sender, EventArgs e)
        {
            if (!hasPreloaded)
            {
                Task.Run(() => UpdateContent(true));
                hasPreloaded = true;
            }
        }

        private void OfficalNewsFeed_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (OfficalNewsFeed.SelectedItem != null)
                {
                    var item = OfficalNewsFeed.SelectedItem as NewsItem_Launcher;
                    FeedItem_JavaDungeons.LoadArticle(item);
                }
            }
        }

        private async void UpdateContent(bool ForceUpdate)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                OfficalNewsFeed.Items.Clear();
                NothingFound.Visibility = Visibility.Visible;
                NothingFound.PanelType = Controls.ResultPanelType.Loading;
            });

            if (ForceUpdate) await UpdateJSONContent();

            await Dispatcher.InvokeAsync(() => {
                foreach (var item in FeedItems.Where(x => OfficalNewsFeed_FeedFilter(x))) OfficalNewsFeed.Items.Add(item);
                if (OfficalNewsFeed.Items.Count == 0) NothingFound.PanelType = Controls.ResultPanelType.NoNews;
                else NothingFound.Visibility = Visibility.Collapsed;
            });
        }

        private bool OfficalNewsFeed_FeedFilter(object obj)
        {
            if (!(obj is NewsItem_Launcher)) return false;
            else
            {
                var item = (obj as NewsItem_Launcher);
                if (item.newsType != null && item.newsType.Contains("News page"))
                {
                    if (item.category == "Minecraft: Java Edition" && ShowJavaContent.IsChecked.Value) return ContainsText(item);
                    else if (item.category == "Minecraft Dungeons" && ShowDungeonsContent.IsChecked.Value) return ContainsText(item);
                    else return false;
                }
                else return false;
            }

            bool ContainsText(NewsItem_Launcher _item)
            {
                string searchParam = SearchBox.Text;
                if (string.IsNullOrEmpty(searchParam) || _item.title.Contains(searchParam, StringComparison.OrdinalIgnoreCase)) return true;
                else return false;
            }
        }

        private async void CheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;
            await Task.Run(() => UpdateContent(false));
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.IsInitialized) return;
            await Task.Run(() => UpdateContent(false));
        }
    }
}
