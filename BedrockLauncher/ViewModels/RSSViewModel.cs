using BedrockLauncher.Classes.Launcher;
using BedrockLauncher.Enums;
using CodeHollow.FeedReader;
using PostSharp.Patterns.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BedrockLauncher.ViewModels
{
    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]
    public class RSSViewModel
    {
        public static RSSViewModel MinecraftForums { get; set; } = new RSSViewModel(ForumsFeed_RSS, RSSType.RSS);
        private const string MinecraftFeed_RSS = @"https://www.minecraft.net/en-us/feeds/community-content/rss";

        public static RSSViewModel MinecraftCommunity { get; set; } = new RSSViewModel(MinecraftFeed_RSS, RSSType.MinecraftRSS);
        private const string ForumsFeed_RSS = @"https://www.minecraftforum.net/news.rss";

        public ObservableCollection<NewsItem_RSS> FeedItems { get; set; } = new ObservableCollection<NewsItem_RSS>();
        public RSSType RSSType { get; set; } = RSSType.RSS;
        public string RSS_URL = string.Empty;

        public RSSViewModel(string rssUrl, RSSType type)
        {
            RSS_URL = rssUrl;
            RSSType = type;
        }

        public async Task UpdateFeed() => await Downloaders.NewsDownloader.UpdateRSSFeed(this);
    }
}
