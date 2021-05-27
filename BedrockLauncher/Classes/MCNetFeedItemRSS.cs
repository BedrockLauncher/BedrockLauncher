using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;

namespace BedrockLauncher.Classes
{
    public class MCNetFeedItemRSS : MCNetFeedItem
    {
        private const string FallbackImageURL = @"/BedrockLauncher;component/resources/images/ui/packs/invalid_pack.png";

        public string GetImageUrl()
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

        public string PrimaryTag
        {
            get
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
        }

        public MCNetFeedItemRSS(FeedItem item) : base()
        {
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
        public override double ImageWidth { get => 200; }
        public override double ImageHeight { get => 200; }
        public override string Tag { get => PrimaryTag; }
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
    }
}
