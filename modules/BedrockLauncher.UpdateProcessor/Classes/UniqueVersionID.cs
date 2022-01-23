using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    public class UniqueVersionID
    {
        public Guid UUID;
        public string Version;
        public UniqueVersionID(Guid _uuid, string _version)
        {
            UUID = _uuid;
            Version = _version;
        }
    }
}
