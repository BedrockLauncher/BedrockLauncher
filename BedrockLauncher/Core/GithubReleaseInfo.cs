using System;
using Newtonsoft.Json;

namespace BedrockLauncher.Core
{
    public class GithubReleaseInfo
    {
        public string name { get; set; }
        public string tag_name { get; set; }
        public string body { get; set; }
        public DateTime created_at { get; set; }
        public DateTime published_at { get; set; }
        public GithubAsset[] assets { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public bool prerelease { get; set; }

        [JsonIgnore]
        public bool isBeta { get; set; }

        public GithubReleaseInfo()
        { }

        public GithubReleaseInfo(GithubReleaseInfo toCopy)
        {
            this.name = toCopy.name;
            this.tag_name = toCopy.tag_name;
            this.body = toCopy.body;
            this.created_at = toCopy.created_at;
            this.published_at = toCopy.published_at;
            this.assets = toCopy.assets;
            this.url = toCopy.url;
            this.html_url = toCopy.html_url;
            this.isBeta = toCopy.isBeta;
            this.prerelease = toCopy.prerelease;
        }
    }
}
