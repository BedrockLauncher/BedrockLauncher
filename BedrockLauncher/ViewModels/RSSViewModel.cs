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
        public static RSSViewModel MinecraftForums { get; set; } = new RSSViewModel(Constants.RSS_FORUMS_URL, RSSType.RSS);
        public static RSSViewModel MinecraftCommunity { get; set; } = new RSSViewModel(Constants.RSS_COMMUNITY_URL, RSSType.MinecraftRSS);


        public ObservableCollection<News_RssItem> FeedItems { get; set; } = new ObservableCollection<News_RssItem>();
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
