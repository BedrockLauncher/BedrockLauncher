using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Enums;
using System;
using System.Collections.Generic;

namespace BedrockLauncher.UpdateProcessor.Interfaces
{
    public interface IVersionDb
    {
        void AddVersion(
            List<UpdateInfo> updates,
            VersionType type);

        void Save(string filePath);

        List<IVersionInfo> GetVersions();

        void ParseRaw(
            string data,
            Dictionary<Guid, string> architectures);
    }
}