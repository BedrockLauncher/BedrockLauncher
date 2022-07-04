using BedrockLauncher.Classes.Launcher;
using BedrockLauncher.ViewModels;
using CodeHollow.FeedReader;
using MdXaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace BedrockLauncher.Downloaders
{
    public static class NewsDownloader
    {
        public static async Task UpdateOfficalFeed(NewsViewModel viewModel)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                viewModel.FeedItemsOffical.Clear();
            });

            NewsFeed_Offical result = null;
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var json = await httpClient.GetStringAsync(Constants.RSS_LAUNCHER_URL);
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<NewsFeed_Offical>(json);
                }
                catch
                {
                    result = new NewsFeed_Offical();
                }

            }
            if (result == null) result = new NewsFeed_Offical();
            if (result.entries == null) result.entries = new List<NewsItem_Offical>();

            await Application.Current.Dispatcher.InvokeAsync(() => {
                foreach (NewsItem_Offical item in result.entries)
                {
                    if (item.newsType != null && item.newsType.Contains("News page")) viewModel.FeedItemsOffical.Add(item);
                }
            });
        }
        public static async Task UpdateLauncherFeed(NewsViewModel viewModel)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                viewModel.LauncherNewsItems.Clear();
                bool isFirstItem = true;
                string latest_name = BedrockLauncher.Localization.Language.LanguageManager.GetResource("LauncherNewsPage_Title_Text").ToString();
                foreach (var item in MainViewModel.Updater.Notes)
                {
                    AppPatchNote newItem = new AppPatchNote(item);

                    if (isFirstItem) newItem.isLatest = true; isFirstItem = false;
                    newItem.isBeta = item.url.Contains(BedrockLauncher.Core.GithubAPI.BETA_URL);

                    viewModel.LauncherNewsItems.Add(newItem);
                }

            });
        }
        public static async Task UpdateRSSFeed(RSSViewModel viewModel)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    viewModel.FeedItems.Clear();
                    string rss = string.Empty;
                    using (var httpClient = new HttpClient()) rss = await httpClient.GetStringAsync(viewModel.RSS_URL);
                    Feed feed = FeedReader.ReadFromString(rss);
                    foreach (FeedItem item in feed.Items)
                    {
                        var new_item = new NewsItem_RSS(item, viewModel.RSSType);
                        viewModel.FeedItems.Add(new_item);
                    }
                }
                catch
                {

                }
            });

        }

    }
}
