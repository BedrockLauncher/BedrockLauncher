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

        private const string OfficalFeed_RSS = @"https://launchercontent.mojang.com/news.json";

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
                    var json = await httpClient.GetStringAsync(OfficalFeed_RSS);
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
                string latest_name = Application.Current.FindResource("LauncherNewsPage_Title_Text").ToString();
                foreach (var item in MainViewModel.Updater.Notes)
                {
                    bool isBeta = item.url.Contains(BedrockLauncher.Core.GithubAPI.BETA_URL);
                    if (isFirstItem)
                    {
                        GenerateEntry(latest_name, item, isBeta, true);
                        isFirstItem = false;
                    }
                    else GenerateEntry(item.name, item, isBeta);
                }

                void GenerateEntry(string name, BedrockLauncher.Core.UpdateNote item, bool isBeta, bool isLatest = false)
                {
                    string body = item.body;
                    string tag = item.tag_name;

                    AppPatchNote launcherUpdateItem = new AppPatchNote();

                    body = body.Replace("\r\n", "\r\n\r\n");

                    Markdown engine = new Markdown();
                    engine.DocumentStyle = Application.Current.FindResource("FlowDocument_Style") as Style;
                    engine.NormalParagraphStyle = Application.Current.FindResource("FlowDocument_Style_Paragrath") as Style;
                    engine.CodeStyle = Application.Current.FindResource("FlowDocument_CodeBlock") as Style;
                    engine.CodeBlockStyle = Application.Current.FindResource("FlowDocument_CodeBlock") as Style;
                    FlowDocument document = engine.Transform(body);
                    string documentString = new TextRange(document.ContentStart, document.ContentEnd).Text;

                    if (isLatest) launcherUpdateItem.buildTitle_Foreground = Brushes.Goldenrod;
                    else if (isBeta) launcherUpdateItem.buildTitle_Foreground = Brushes.Gold;
                    else launcherUpdateItem.buildTitle_Foreground = Brushes.White;

                    if (tag == System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()) launcherUpdateItem.CurrentBox_Visibility = Visibility.Visible;

                    launcherUpdateItem.buildTitle = name;
                    launcherUpdateItem.buildVersion = string.Format("v{0}{1}", tag, (isBeta ? " (Beta)" : ""));
                    launcherUpdateItem.buildChanges = documentString;
                    launcherUpdateItem.buildDate = item.published_at.ToString();

                    viewModel.LauncherNewsItems.Add(launcherUpdateItem);
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
