using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.UpdateProcessor.Extensions;
using BedrockLauncher.UpdateProcessor.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BedrockLauncher.UpdateProcessor.Databases
{
    public enum GdkCatalogMergeResult
    {
        Added,
        Unchanged,
        ResourcesAdded,
        IdentityConflict,
        Rejected
    }

    public class VersionJsonDb : IVersionDb
    {
        public List<VersionInfoJson> list { get; private set; } =
            new List<VersionInfoJson>();

        private void SortVersions()
        {
            list.Sort();
            list.Reverse();
        }

        public void ReadJson(
            string filePath,
            Dictionary<Guid, string> architectures = null)
        {
            if (!File.Exists(filePath))
            {
                list.Clear();
                return;
            }

            string data = File.ReadAllText(filePath);
            ParseJson(data, architectures);
        }

        public void WriteJson(string filePath)
        {
            SortVersions();

            var valuesList = list
                .Select(ToJson)
                .ToList();

            string json = JsonConvert.SerializeObject(
                valuesList,
                Formatting.Indented);

            string directory =
                Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(filePath, json);
        }

        public void ParseJson(
            string json,
            Dictionary<Guid, string> architectures = null)
        {
            list.Clear();

            if (string.IsNullOrWhiteSpace(json))
                return;

            JArray data = JArray.Parse(json);

            foreach (JToken token in data)
            {
                if (!(token is JArray item))
                    continue;

                if (item.Count < 3)
                    continue;

                string version =
                    item[0]?.Value<string>();

                string uuid =
                    item[1]?.Value<string>();

                int typeValue =
                    item[2]?.Value<int>() ?? 0;

                if (string.IsNullOrWhiteSpace(version) ||
                    string.IsNullOrWhiteSpace(uuid))
                {
                    continue;
                }

                VersionType versionType =
                    (VersionType)typeValue;

                string architecture =
                    item.Count >= 4
                        ? item[3]?.Value<string>()
                        : VersionDbExtensions.FallbackArch;

                if (string.IsNullOrWhiteSpace(architecture))
                    architecture = VersionDbExtensions.FallbackArch;

                // Old database entries contain only four fields.
                // Old entries are treated as UWP.
                PackageType packageType =
                    item.Count >= 5
                        ? (PackageType)(item[4]?.Value<int>() ?? 0)
                        : PackageType.UWP;

                if (architecture == VersionDbExtensions.FallbackArch &&
                    architectures != null &&
                    Guid.TryParse(uuid, out Guid parsedUuid) &&
                    architectures.TryGetValue(
                        parsedUuid,
                        out string knownArchitecture))
                {
                    architecture = knownArchitecture;
                }

                // GDK entries carry the exact required package identity and its resources.
                string packageIdentity =
                    packageType == PackageType.GDK && item.Count >= 6
                        ? item[5]?.Value<string>()
                        : null;

                string[] downloadUrls =
                    packageType == PackageType.GDK && item.Count >= 7 && item[6] is JArray urls
                        ? urls.Select(x => x?.Value<string>())
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToArray()
                        : null;

                var parsedVersion = new VersionInfoJson(
                    version,
                    uuid,
                    versionType,
                    architecture,
                    packageType,
                    packageIdentity,
                    downloadUrls);

                if (!list.Any(x =>
                    x.uuid == parsedVersion.uuid &&
                    x.packageType == parsedVersion.packageType))
                {
                    list.Add(parsedVersion);
                }
            }

            SortVersions();
        }

        public void AddVersion(
            List<UpdateInfo> updates,
            VersionType type)
        {
            if (updates == null || updates.Count == 0)
                return;

            foreach (UpdateInfo update in updates)
            {
                if (update == null ||
                    string.IsNullOrWhiteSpace(update.packageMoniker) ||
                    string.IsNullOrWhiteSpace(update.updateId))
                {
                    continue;
                }

                string version =
                    MinecraftVersion
                        .ConvertVersion(
                            update.packageMoniker,
                            type)
                        .ToString();

                string architecture =
                    VersionDbExtensions.GetVersionArch(
                        update.packageMoniker,
                        type);

                var info = new VersionInfoJson(
                    version,
                    update.updateId,
                    type,
                    architecture,
                    PackageType.UWP);

                if (!list.Any(x =>
                    x.uuid == info.uuid &&
                    x.packageType == info.packageType))
                {
                    list.Add(info);
                }
            }

            SortVersions();
        }

        public void Save(string filePath)
        {
            WriteJson(filePath);
        }

        private static JArray ToJson(VersionInfoJson version)
        {
            var item = new JArray(
                version.version,
                version.uuid.ToString(),
                (int)version.type,
                version.architecture,
                (int)version.packageType);

            if (version.packageType == PackageType.GDK)
            {
                item.Add(version.packageIdentity);
                item.Add(new JArray(version.downloadUrls ?? Array.Empty<string>()));
            }

            return item;
        }

        /// <summary>
        /// Adds a discovered GDK version to the persisted catalog without ever rewriting an existing association.
        ///
        /// The persisted entry is the source of truth: once "Minecraft X -> package identity P" is stored, a later
        /// discovery (refresh, GdkLinks update, another download) can only add resources for exactly P. A discovery
        /// that claims a different identity for the same version is reported as a conflict and ignored.
        /// </summary>
        public GdkCatalogMergeResult MergeGdkEntry(VersionInfoJson discovered)
        {
            GdkPackageIdentity discoveredIdentity = discovered.GetRequiredGdkPackage();
            if (discoveredIdentity == null)
                return GdkCatalogMergeResult.Rejected;

            int index = list.FindIndex(x =>
                x.uuid == discovered.uuid &&
                x.packageType == PackageType.GDK);

            string[] discoveredUrls = FilterUrlsForIdentity(discovered.downloadUrls, discoveredIdentity);

            if (index < 0)
            {
                list.Add(new VersionInfoJson(
                    discovered.version,
                    discovered.uuid.ToString(),
                    discovered.type,
                    discovered.architecture,
                    PackageType.GDK,
                    discoveredIdentity.FullName,
                    discoveredUrls));

                SortVersions();
                return GdkCatalogMergeResult.Added;
            }

            VersionInfoJson persisted = list[index];
            GdkPackageIdentity persistedIdentity = persisted.GetRequiredGdkPackage();

            if (persistedIdentity == null || !persistedIdentity.Equals(discoveredIdentity))
                return GdkCatalogMergeResult.IdentityConflict;

            string[] mergedUrls = FilterUrlsForIdentity(persisted.downloadUrls, persistedIdentity)
                .Concat(discoveredUrls)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (mergedUrls.Length == (persisted.downloadUrls?.Length ?? 0))
                return GdkCatalogMergeResult.Unchanged;

            persisted.downloadUrls = mergedUrls;
            list[index] = persisted;
            return GdkCatalogMergeResult.ResourcesAdded;
        }

        /// <summary>Keeps only resources whose file name is exactly the given package identity.</summary>
        public static string[] FilterUrlsForIdentity(IEnumerable<string> urls, GdkPackageIdentity identity)
        {
            if (urls == null || identity == null)
                return Array.Empty<string>();

            return urls
                .Where(url => GdkPackageIdentity.TryParseFromUrl(url, out GdkPackageIdentity urlIdentity) &&
                              identity.Equals(urlIdentity))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public List<IVersionInfo> GetVersions()
        {
            return list
                .Cast<IVersionInfo>()
                .ToList();
        }

        public void ParseRaw(
            string data,
            Dictionary<Guid, string> architectures)
        {
            ParseJson(data, architectures);
        }

        // Compatibility alias for older code that used the misspelled name.
        public void PraseRaw(
            string data,
            Dictionary<Guid, string> architectures)
        {
            ParseRaw(data, architectures);
        }
    }
}