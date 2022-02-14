using BedrockLauncher.UpdateProcessor.Interfaces;
using System;
using System.Collections.Generic;
using BedrockLauncher.UpdateProcessor.Enums;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    public struct VersionInfoJson : IVersionInfo, IComparable<VersionInfoJson>, IComparer<VersionInfoJson>
    {
    
        public string version;
        public Guid uuid;
        public VersionType type;
        public string architecture;

        public VersionInfoJson(string _version, string _uuid, VersionType _type, string _architexture)
        {
            if (!Guid.TryParse(_uuid, out uuid)) uuid = Guid.Empty;
            version = _version;
            type = _type;
            architecture = _architexture;
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

        public bool GetIsBeta()
        {
            return type == VersionType.Beta;
        }

        public int Compare(VersionInfoJson x, VersionInfoJson y)
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
