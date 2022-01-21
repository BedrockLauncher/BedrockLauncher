using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Classes.SkinPack
{
    public class MCSkinPackMainfest
    {
        public int format_version { get; set; }
        public Header header { get; set; } = new Header();
        public List<Module> modules { get; set; } = new List<Module>();

        public class Module
        {
            public string type { get; set; }
            public string description { get; set; }
            public string uuid { get; set; }
            public int[] version { get; set; }
        }
        public class Header
        {
            public string name { get; set; }
            public string description { get; set; }
            public string uuid { get; set; }
            public int[] version { get; set; }
        }
    }
}
