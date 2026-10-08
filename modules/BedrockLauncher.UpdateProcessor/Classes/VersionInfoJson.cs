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

        /// <summary>
        /// GDK only: full name of the exact package this version requires
        /// (e.g. Microsoft.MinecraftUWP_1.26.4005.0_x64__8wekyb3d8bbwe). Null for UWP.
        /// </summary>
        public string packageIdentity;

        /// <summary>GDK only: CDN resources whose file name is exactly <see cref="packageIdentity"/>.</summary>
        public string[] downloadUrls;

        public VersionInfoJson(
            string version,
            string uuid,
            VersionType type,
            string architecture,
            PackageType packageType = PackageType.UWP,
            string packageIdentity = null,
            string[] downloadUrls = null)
        {
            if (!Guid.TryParse(uuid, out this.uuid))
                this.uuid = Guid.Empty;

            this.version = version;
            this.type = type;
            this.architecture = architecture;
            this.packageType = packageType;
            this.packageIdentity = packageType == PackageType.GDK ? packageIdentity : null;
            this.downloadUrls = packageType == PackageType.GDK ? downloadUrls ?? Array.Empty<string>() : null;
        }

        /// <summary>The persisted required GDK package, or null when this is not a GDK version or it is unresolved.</summary>
        public GdkPackageIdentity GetRequiredGdkPackage()
        {
            if (packageType != PackageType.GDK)
                return null;

            return GdkPackageIdentity.TryParseFullName(packageIdentity, out GdkPackageIdentity identity)
                ? identity
                : null;
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