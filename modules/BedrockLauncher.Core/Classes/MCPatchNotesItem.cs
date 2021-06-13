using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Core.Classes
{
    public class MCPatchNotesItem
    {
        public const string FallbackImageURL = @"/BedrockLauncher.Core;component/resources/images/packs/pack_icon.png";
        public const string FallbackImageURL_Dev = @"/BedrockLauncher.Core;component/resources/images/packs/dev_pack_icon.png";

        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public string Tag { get => (isBeta ? "Beta" : "Release"); }
        public string Url { get; set; }
        public System.Version Version { get; set; }
        public bool isBeta { get; set; } = false;
        public string Description { get; set; } = string.Empty;
        public string PublishingDateString { get; set; } = string.Empty;

        public MCPatchNotesItem()
        {

        }

    }
}
