using BedrockLauncher.UpdateProcessor.Interfaces;
using System;
using BedrockLauncher.UpdateProcessor.Enums;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    public struct VersionInfoTxt : IVersionInfo
    {
        public Guid uuid;
        public string packageMoniker;
        public string serverId;

        public string version;
        public string architecture;
        public VersionType type;

        public VersionInfoTxt(string _uuid, string _packageMoniker, string _serverId, string _architexture, VersionType _type)
        {
            if (!Guid.TryParse(_uuid, out uuid)) uuid = Guid.Empty;
            packageMoniker = _packageMoniker;
            serverId = _serverId;

            version = MinecraftVersion.ConvertVersion(_packageMoniker, _type).ToString();
            architecture = _architexture;
            type = _type;
        }

        public string GetArchitecture()
        {
            return architecture;
        }

        public VersionType GetVersionType()
        {
            return type;
        }

        public bool GetIsBeta()
        {
            return type == VersionType.Beta;
        }

        public Guid GetUUID()
        {
            return uuid;
        }

        public string GetVersion()
        {
            return version;
        }
    }
}
