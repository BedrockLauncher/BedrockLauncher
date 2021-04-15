using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeHollow.FeedReader;

namespace BedrockLauncher.Classes
{
    public class MCNetFeedItem : FeedItem
    {
        private const string FallbackImageURL = "";

        public string ImageUrl
        {
            get
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

        public MCNetFeedItem(FeedItem item) : base()
        {
            this.Author = item.Author;
            this.Categories = item.Categories;
            this.Content = item.Content;
            this.Description = item.Description;
            this.Id = item.Id;
            this.Link = item.Link;
            this.PublishingDate = item.PublishingDate;
            this.PublishingDateString = item.PublishingDateString;
            this.SpecificItem = item.SpecificItem;
            this.Title = item.Title;
        }

    }
}
