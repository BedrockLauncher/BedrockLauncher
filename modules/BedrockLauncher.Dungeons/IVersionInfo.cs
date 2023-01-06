using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    public interface IVersionInfo
    {
        public string GetVersion();
        public Guid GetUUID();
        public string GetArchitecture();
    }
}
