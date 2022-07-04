using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace BedrockLauncher.Core
{
    public class GithubAPI
    {
        public const string RELEASE_URL = "https://api.github.com/repos/BedrockLauncher/BedrockLauncher/releases";
        public const string BETA_URL = "https://api.github.com/repos/BedrockLauncher/BedrockLauncher-Beta/releases";
    }

    public class Asset
    {
        public string url { get; set; }
        public long size { get; set; }
        public string name { get; set; }
    }

    public class UpdateNote
    {
        public string name { get; set; }
        public string tag_name { get; set; }
        public string body { get; set; }
        public DateTime created_at { get; set; }
        public DateTime published_at { get; set; }
        public Asset[] assets { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }

        [JsonIgnore]
        public bool isBeta { get; set; }

        public UpdateNote()
        { }

        public UpdateNote(UpdateNote toCopy)
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
        }
    }
}
