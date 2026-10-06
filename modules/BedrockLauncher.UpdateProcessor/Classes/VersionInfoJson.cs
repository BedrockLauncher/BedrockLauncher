using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.UpdateProcessor.Interfaces;
using System;
using System.Collections.Generic;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    public struct VersionInfoJson :
        IVersionInfo,
        IComparable<VersionInfoJson>,
        IComparer<VersionInfoJson>
    {
        public string version;
        public Guid uuid;
        public VersionType type;
        public string architecture;
        public PackageType packageType;

        public VersionInfoJson(
            string version,
            string uuid,
            VersionType type,
            string architecture,
            PackageType packageType = PackageType.UWP)
        {
            if (!Guid.TryParse(uuid, out this.uuid))
                this.uuid = Guid.Empty;

            this.version = version;
            this.type = type;
            this.architecture = architecture;
            this.packageType = packageType;
        }

        public string GetArchitecture()
        {
            return architecture;
        }

        public Guid GetUUID()
        {
            return uuid;
        }

        public string GetVersion()
        {
            return version;
        }

        public VersionType GetVersionType()
        {
            return type;
        }

        public PackageType GetPackageType()
        {
            return packageType;
        }

        public bool GetIsBeta()
        {
            return type == VersionType.Beta;
        }

        public int Compare(
            VersionInfoJson x,
            VersionInfoJson y)
        {
            var a = MinecraftVersion.Parse(x.version);
            var b = MinecraftVersion.Parse(y.version);

            return a.CompareTo(b);
        }

        public int CompareTo(VersionInfoJson other)
        {
            return Compare(this, other);
        }
    }
}