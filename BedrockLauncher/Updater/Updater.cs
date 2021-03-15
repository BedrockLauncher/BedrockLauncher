using System;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Diagnostics;

namespace BedrockLauncher
{
    class Updater
    {
        private const string LATEST_BUILD_LINK = "https://github.com/XlynxX/BedrockLauncher/releases/latest";
        public Updater()
        {
            Debug.WriteLine("checking for updates");
            CheckUpdates();
        }

        private async void CheckUpdates()
        {
            var html = await Task.Run(getHtml);
            var nodes = html.DocumentNode.SelectNodes("//head/meta");
            foreach (var node in nodes)
            {
                try
                { 
                    if (node.Attributes["content"].Value.StartsWith("/XlynxX/BedrockLauncher/releases/tag/"))
                    {
                        string latestTag = node.Attributes["content"].Value.Replace("/XlynxX/BedrockLauncher/releases/tag/", "");
                        string latestTagDescription = node.NextSibling.Attributes["content"].Value;
                        Console.WriteLine("Current tag: " + Properties.Settings.Default.Version);
                        Console.WriteLine("Latest tag: " + latestTag);
                        Console.WriteLine("Latest tag description: " + latestTagDescription);
                        // if current tag < than latest tag
                        if (int.Parse(Properties.Settings.Default.Version.Replace(".", "")) < int.Parse(latestTag.Replace(".", "")))
                        { 
                            Console.WriteLine("update needed!");
                        }
                    }
                }
                catch { }
            }
            //Console.WriteLine("Node Name: " + node.Name + "\n" + node.OuterHtml);
        }

        private HtmlAgilityPack.HtmlDocument getHtml()
        {
            using (WebClient wc = new WebClient())
            {
                var web = new HtmlWeb();
                var doc = web.Load(LATEST_BUILD_LINK);
                return doc;
            }
        }
    }
}
