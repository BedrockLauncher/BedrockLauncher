using BedrockLauncher.UpdateProcessor.Interfaces;
using System;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    public struct VersionInfoTxt : IVersionInfo
    {
        public Guid uuid;
        public string packageMoniker;
        public string serverId;

        public string version;
        public string architecture;
        public bool isBeta;

        public VersionInfoTxt(string _uuid, string _packageMoniker, string _serverId, string _architexture, bool _isBeta)
        {
            if (!Guid.TryParse(_uuid, out uuid)) uuid = Guid.Empty;
            packageMoniker = _packageMoniker;
            serverId = _serverId;

            version = MinecraftVersion.ConvertVersion(_packageMoniker).ToString();
            architecture = _architexture;
            isBeta = _isBeta;
        }

        public string GetArchitecture()
        {
            return architecture;
        }

        public bool GetIsBeta()
        {
            return isBeta;
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
