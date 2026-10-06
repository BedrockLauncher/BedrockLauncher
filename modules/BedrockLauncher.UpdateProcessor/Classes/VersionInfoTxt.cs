using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.UpdateProcessor.Interfaces;
using System;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    /// <summary>
    /// Represents a version entry backed by a raw UWP package-moniker string
    /// (as used in the legacy .txt version database format).
    /// </summary>
    public struct VersionInfoTxt : IVersionInfo
    {
        public Guid   uuid;
        public string packageMoniker;
        public string serverId;

        public string     version;
        public string     architecture;
        public VersionType type;
        public PackageType packageType;

        /// <param name="architecture">Target architecture string (e.g. "x64", "x86", "arm64").</param>
        public VersionInfoTxt(string uuid, string packageMoniker, string serverId, string architecture, VersionType type, PackageType packageType = PackageType.UWP)
        {
            if (!Guid.TryParse(uuid, out this.uuid)) this.uuid = Guid.Empty;
            this.packageMoniker = packageMoniker;
            this.serverId       = serverId;

            this.version      = MinecraftVersion.ConvertVersion(packageMoniker, type).ToString();
            this.architecture = architecture;
            this.type         = type;
            this.packageType  = packageType;
        }

        public string     GetArchitecture() => architecture;
        public VersionType GetVersionType() => type;
        public bool       GetIsBeta()       => type == VersionType.Beta;
        public Guid       GetUUID()         => uuid;
        public string     GetVersion()      => version;
        public PackageType GetPackageType() => packageType;
    }
}
