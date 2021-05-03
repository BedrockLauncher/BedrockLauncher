using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL_Core
{
    public class GithubAPI
    {
        public const string RELEASE_URL = "https://api.github.com/repos/CarJem/BedrockLauncher/releases/latest";

        public const string BETA_URL = "https://api.github.com/repos/CarJem/BedrockLauncher-Beta/releases/latest";

        public const string ACCESS_TOKEN = "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";
    }

    public class Asset
    {
        public string url { get; set; }
        public int size { get; set; }
    }

    public class UpdateNote
    {
        public string tag_name { get; set; }
        public string body { get; set; }
        public Asset[] assets { get; set; }
    }
}
