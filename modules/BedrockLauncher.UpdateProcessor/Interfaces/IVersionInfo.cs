using BedrockLauncher.UpdateProcessor.Enums;
using System;

namespace BedrockLauncher.UpdateProcessor.Interfaces
{
    public interface IVersionInfo
    {
        public string GetVersion();
        public Guid GetUUID();
        public string GetArchitecture();
        public VersionType GetVersionType();
        public bool GetIsBeta();
        public PackageType GetPackageType();
    }
}
