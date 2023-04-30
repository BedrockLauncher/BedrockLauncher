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

            News_OfficalFeed result = null;
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var json = await httpClient.GetStringAsync(Constants.RSS_LAUNCHER_URL);
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<News_OfficalFeed>(json);
                }
                catch
                {
                    result = new News_OfficalFeed();
                }

            }
            if (result == null) result = new News_OfficalFeed();
            if (result.entries == null) result.entries = new List<News_OfficalItem>();

            await Application.Current.Dispatcher.InvokeAsync(() => {
                foreach (News_OfficalItem item in result.entries)
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
                foreach (var item in MainDataModel.Updater.Notes)
                {
                    PatchNote_Launcher newItem = new PatchNote_Launcher(item);

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
                        var new_item = new News_RssItem(item, viewModel.RSSType);
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
