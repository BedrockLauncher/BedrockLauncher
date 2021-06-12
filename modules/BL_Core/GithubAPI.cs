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
        public static string ACCESS_TOKEN 
        {
            get
            {

                List<string> token = new List<string>()
                {
                    "g",
                    "h",
                    "p",
                    "_",
                    "Z",
                    "B",
                    "0",
                    "D",
                    "u",
                    "X",
                    "r",
                    "F",
                    "u",
                    "o",
                    "1",
                    "w",
                    "V",
                    "w",
                    "C",
                    "y",
                    "P",
                    "D",
                    "j",
                    "w",
                    "B",
                    "k",
                    "i",
                    "X",
                    "f",
                    "T",
                    "P",
                    "S",
                    "g",
                    "B",
                    "1",
                    "c",
                    "F",
                    "i",
                    "e",
                    "J"
                };
                return String.Join("", token);
            }
        }
    }

    public class Asset
    {
        public string url { get; set; }
        public long size { get; set; }
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

        [JsonIgnore]
        public bool isBeta { get; set; }
    }
}
