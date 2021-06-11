using System.Collections.Generic;

namespace BedrockLauncher.Core.Classes.MediaWiki
{
    public class RootObject
    {
        public string batchcomplete { get; set; }
        public Continue @continue { get; set; }
        public Query query { get; set; }
        public Limits limits { get; set; }

    }
}
