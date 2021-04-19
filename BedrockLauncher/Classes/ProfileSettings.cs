using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Classes
{
    public class ProfileSettings
    {
        public string Name { get; set; }
        public string ProfilePath { get; set; }
        public List<Installation> Installations { get; set; }

    }
}
