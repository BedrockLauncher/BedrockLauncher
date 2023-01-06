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
        void AddVersion(List<UpdateInfo> u, VersionType type);
        void Save(string winstoreDBFile);
        List<IVersionInfo> GetVersions();
        void PraseRaw(string data, Dictionary<Guid, string> architectures);
    }
}
