using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeHollow.FeedReader;

namespace BedrockLauncher.Core.Classes
{

    public class LauncherNewsFeed
    {
        public int version { get; set; }
        public List<NewsItem_Launcher> entries { get; set; }
    }

    public class NewsItem_Launcher : NewsItem
    {

        public class Dimensions
        {
            public double width { get; set; }
            public double height { get; set; }
        }

        public class ImageDefinitions
        {
            public string title { get; set; }
            public string url { get; set; }
            public Dimensions dimensions { get; set; }
        }

        public class LinkButton
        {
            public string primary { get; set; }
            public string label { get; set; }
            public string url { get; set; }
        }

        public string title { get; set; }
        public string tag { get; set; }
        public string category { get; set; }
        public DateTime date { get; set; }
        public string text { get; set; }
        public ImageDefinitions playPageImage { get; set; }
        public ImageDefinitions newsPageImage { get; set; }
        public string readMoreLink { get; set; }
        public bool cardBorder { get; set; }
        public string articleBody { get; set; }
        public LinkButton linkButton { get; set; }
        public List<string> newsType { get; set; }
        public string entitlement { get; set; }

        private double GetWidth()
        {
            double? width = newsPageImage?.dimensions?.width ?? null;
            if (width == null) return 220;
            else
            {
                if (width != 0) return width.Value / 2;
                else return width.Value;
            }
        }

        private double GetHeight()
        {
            double? height = newsPageImage?.dimensions?.height ?? null;
            if (height == null) return 220;
            else
            {
                if (height != 0) return height.Value / 2;
                else return height.Value;
            }
        }


        public override string ImageUrl { get => @"https://launchercontent.mojang.com/" + newsPageImage.url; }
        public override double ImageWidth { get => GetWidth(); }
        public override double ImageHeight { get => GetHeight(); }
        public override string Title { get => title; }
        public override string Link { get => readMoreLink; }
        public override string Tag { get => category; }
        public override string Date { get => date.ToString(); }
    }
}
