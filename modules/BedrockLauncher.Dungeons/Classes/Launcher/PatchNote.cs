using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Dungeons.Classes.Launcher
{
    public class PatchNote
    {
        public string title { get; set; }
        public string version { get; set; }
        public string date { get; set; }
        public Image image { get; set; }
        public string ImageUrl
        {
            get
            {
                if (image == null) return string.Empty;
                else return "https://launchercontent.mojang.com/" + image.url;
            }
        }
        public string body { get; set; }
        public string id { get; set; }
        public string contentPath { get; set; }

        public class Image
        {
            public string url { get; set; }
            public string title { get; set; }
        }
    }
}
