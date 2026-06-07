using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BedrockLauncher.Classes
{
    internal static class LauncherVersionPathHelper
    {
        public static IEnumerable<DirectoryInfo> GetDirectoriesToScan(string configuredVersionsFolder)
        {
            string roamingVersionsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraft_bedrock",
                "versions");

            string[] candidates =
            {
                configuredVersionsFolder,
                roamingVersionsFolder
            };

            foreach (string candidate in candidates
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Path.GetFullPath(path))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                yield return Directory.CreateDirectory(candidate);
            }
        }
    }
}
