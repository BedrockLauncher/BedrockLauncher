using System;
using System.Diagnostics;
using System.IO;
using BedrockLauncher.UpdateProcessor.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BedrockLauncher.Handlers
{
    /// <summary>
    /// The record (versions\&lt;version&gt;\metadata.json) the launcher writes after Windows has installed, and the
    /// launcher has validated, the exact package a GDK version requires. It is an audit of what was installed; the
    /// requirement itself lives in the GDK catalog and is never derived from this file.
    /// </summary>
    internal static class GdkInstallRecord
    {
        public const string FileName = "metadata.json";

        internal sealed class Data
        {
            public string MinecraftVersion { get; set; }
            public string VersionUuid { get; set; }
            public string VersionType { get; set; }
            public string PackageType { get; set; } = "GDK";
            public string RequiredPackageFullName { get; set; }
            public string InstalledPackageFullName { get; set; }
            public string PackageFamilyName { get; set; }
            public string Architecture { get; set; }
            public string InstallLocation { get; set; }
            public string LaunchHelperSha256 { get; set; }
            public string InstalledVia { get; set; } = "Windows/GamingServices";
            public string ValidatedUtc { get; set; }
        }

        public static void Write(string directory, Data data)
        {
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, FileName);
            string temporary = path + ".tmp";

            File.WriteAllText(temporary, JsonConvert.SerializeObject(data, Formatting.Indented));
            File.Move(temporary, path, overwrite: true);
        }

        /// <summary>Reads the installed package identity recorded for a version folder.</summary>
        public static bool TryRead(string directory, out GdkPackageIdentity installed)
        {
            installed = null;

            try
            {
                string path = Path.Combine(directory ?? string.Empty, FileName);
                if (!File.Exists(path))
                    return false;

                JObject record = JObject.Parse(File.ReadAllText(path));

                if (!string.Equals((string)record["PackageType"] ?? (string)record["packageType"], "GDK", StringComparison.OrdinalIgnoreCase))
                    return false;

                // Records written before the GDK catalog existed only carry "packageFullName".
                string installedFullName =
                    (string)record["InstalledPackageFullName"] ??
                    (string)record["packageFullName"];

                return GdkPackageIdentity.TryParseFullName(installedFullName, out installed);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                Trace.WriteLine($"Could not read the GDK install record in {directory}: {ex.Message}");
                return false;
            }
        }
    }
}
