using System.Collections.Generic;

namespace BL_Core.Classes.MediaWiki
{
    public class Query
    {
        public Dictionary<string, Val> pages { get; set; }

        public List<Val> categorymembers { get; set; }
    }
}
