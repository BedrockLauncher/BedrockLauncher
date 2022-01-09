using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Classes.SkinPack
{
    public class MCSkinPackContent
    {
        public string geometry { get; set; }
        public List<MCSkin> skins { get; set; } = new List<MCSkin>();
        public string serialize_name { get; set; }
        public string localization_name { get; set; }
    }
}
