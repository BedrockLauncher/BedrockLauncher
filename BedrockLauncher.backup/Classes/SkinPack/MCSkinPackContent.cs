using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Classes.SkinPack
{
    public class MCSkinPackContent
    {
        public string geometry { get; set; }
        public ObservableCollection<MCSkin> skins { get; set; } = new ObservableCollection<MCSkin>();
        public string serialize_name { get; set; }
        public string localization_name { get; set; }
    }
}
