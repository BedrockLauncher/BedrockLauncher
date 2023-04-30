using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace BedrockLauncher.Core
{
    public class GithubAPI
    {
        public const string RELEASE_URL = "https://api.github.com/repos/BedrockLauncher/BedrockLauncher/releases";
        public const string BETA_URL = "https://api.github.com/repos/BedrockLauncher/BedrockLauncher-Beta/releases";
    }
}
