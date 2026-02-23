using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.UpdateProcessor.Enums;

namespace BedrockLauncher.UpdateProcessor.Interfaces
{
    public interface IVersionDb
    {
        void Save(string winstoreDBFile);
        List<IVersionInfo> GetVersions();
        void ParseRaw(string data, Dictionary<string, string> architectures);
    }
}
