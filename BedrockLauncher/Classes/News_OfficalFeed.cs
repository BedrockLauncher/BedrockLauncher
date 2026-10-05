using System.Collections.Generic;

namespace BedrockLauncher.Classes.Launcher
{
    public class News_OfficialFeed
    {
        public int version { get; set; }
        public List<News_OfficialItem> entries { get; set; }
    }
}
