using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.UpdateProcessor.Databases
{
    public class VersionNxDb : IVersionDb
    {
        public void AddVersion(List<UpdateInfo> u, bool isBeta)
        {
            throw new NotImplementedException();
        }

        public List<IVersionInfo> GetVersions()
        {
            throw new NotImplementedException();
        }

        public void PraseRaw(string data, Dictionary<Guid, string> architectures)
        {
            throw new NotImplementedException();
        }

        public void Save(string winstoreDBFile)
        {
            throw new NotImplementedException();
        }
    }
}
