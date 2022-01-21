using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Enums;
using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;

namespace BedrockLauncher.Classes.Launcher
{
    public class NewsItem_RSS : NewsItem
    {
        private const string FallbackImageURL = @"/BedrockLauncher;component/resources/images/packs/invalid_pack.png";

        public string GetPrimaryTag()
        {
            switch (this.Type)
            {
                case RSSType.RSS:
                    return RSS();
                case RSSType.MinecraftRSS:
                    return MinecraftRSS();
                default:
                    return Default();
            }

            string RSS()
            {
                return "RSS";
            }
            string MinecraftRSS()
            {
                var attributes = this.SpecificItem.Element.Elements();
                if (attributes != null)
                {
                    if (attributes.ToList().Exists(x => x.Name.LocalName == "primaryTag"))
                    {
                        var result = attributes.Where(x => x.Name.LocalName == "primaryTag").FirstOrDefault();
                        return result.Value;
                    }
                }

                return "NULL";
            }
            string Default()
            {
                return "NULL";
            }
        }
        public string GetImageUrl()
        {
            switch (this.Type)
            {
                case RSSType.RSS:
                    return RSS();
                case RSSType.MinecraftRSS:
                    return MinecraftRSS();
                default:
                    return Default();
            }

            string RSS()
            {
                var elements = this.SpecificItem.Element.Elements();
                if (elements != null)
                {
                    if (elements.ToList().Exists(x => x.Name.LocalName == "imageURL"))
                    {
                        var result = elements.Where(x => x.Name.LocalName == "imageURL").FirstOrDefault();
                        return result.Value;
                    }
                }
                var attributes = this.SpecificItem.Element.Attributes();
                if (attributes != null)
                {
                    if (attributes.ToList().Exists(x => x.Name.LocalName == "image"))
                    {
                        var result = attributes.Where(x => x.Name.LocalName == "image").FirstOrDefault();
                        return result.Value;
                    }
                }

                return FallbackImageURL;
            }
            string MinecraftRSS()
            {
                var attributes = this.SpecificItem.Element.Elements();
                if (attributes != null)
                {
                    if (attributes.ToList().Exists(x => x.Name.LocalName == "imageURL"))
                    {
                        var result = attributes.Where(x => x.Name.LocalName == "imageURL").FirstOrDefault();
                        return @"https://www.minecraft.net/" + result.Value;
                    }
                }

                return FallbackImageURL;
            }
            string Default()
            {
                return "NULL";
            }
        }

        public NewsItem_RSS(FeedItem item, RSSType type) : base()
        {
            this.Type = type;
            this.Author = item.Author;
            this.Categories = item.Categories;
            this.Content = item.Content;
            this.Description = item.Description;
            this.Id = item.Id;
            this.Link = item.Link;
            this.PublishingDate = item.PublishingDate;
            this.PublishingDateString = item.PublishingDate.ToString();
            this.SpecificItem = item.SpecificItem;
            this.Title = item.Title;
        }

        public override string ImageUrl { get => GetImageUrl(); }
        public override double ImageWidth { get => 190; }
        public override double ImageHeight { get => 190; }
        public override string Tag { get => GetPrimaryTag(); }
        public override string Date { get => PublishingDateString; }
        public override string Title { get; }
        public override string Link { get; }
        public DateTime? PublishingDate { get; private set; }
        public string PublishingDateString { get; private set; }
        public BaseFeedItem SpecificItem { get; }
        public string Author { get; private set; }
        public ICollection<string> Categories { get; private set; }
        public string Content { get; private set; }
        public string Description { get; private set; }
        public string Id { get; private set; }
        public RSSType Type { get; set; }
    }
}
