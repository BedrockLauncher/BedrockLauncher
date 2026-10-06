using System;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    public class UniqueVersionID
    {
        public Guid UUID;
        public string Version;
        public UniqueVersionID(Guid _uuid, string _version)
        {
            UUID = _uuid;
            Version = _version;
        }
    }
}
