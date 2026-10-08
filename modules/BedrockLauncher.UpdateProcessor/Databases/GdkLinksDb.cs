using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Enums;
using Newtonsoft.Json.Linq;

namespace BedrockLauncher.UpdateProcessor.Databases
{
    /// <summary>
    /// Parses https://github.com/MinecraftBedrockArchiver/GdkLinks urls.json
    /// and expands every entry with the full set of Xbox Live CDN mirrors.
    ///
    /// Primary CDN roots (CdnRootPaths):
    ///   http://assets1.xboxlive.com
    ///   http://assets2.xboxlive.com
    ///
    /// Background CDN roots (BackgroundCdnRootPaths):
    ///   http://d1.xboxlive.com
    ///   http://d2.xboxlive.com
    ///
    /// Chinese Akamai mirrors:
    ///   http://assets1.xboxlive.cn
    ///   http://assets2.xboxlive.cn
    ///   http://d1.xboxlive.cn
    ///   http://d2.xboxlive.cn
    /// </summary>
    public class GdkLinksDb
    {
        // -----------------------------------------------------------------
        // CDN mirror roots — ordered by priority (fastest first).
        // -----------------------------------------------------------------
        private static readonly string[] CdnRoots = new[]
        {
            // Primary (CdnRootPaths)
            "http://assets1.xboxlive.com",
            "http://assets2.xboxlive.com",
            // Background (BackgroundCdnRootPaths)
            "http://d1.xboxlive.com",
            "http://d2.xboxlive.com",
            // Chinese Akamai mirrors
            "http://assets1.xboxlive.cn",
            "http://assets2.xboxlive.cn",
            "http://d1.xboxlive.cn",
            "http://d2.xboxlive.cn",
        };

        public List<VersionInfoJson> Versions { get; } = new List<VersionInfoJson>();

        /// <summary>Entries that could not be resolved to exactly one package identity (with the reason).</summary>
        public List<string> RejectedEntries { get; } = new List<string>();

        public void Parse(string json)
        {
            Versions.Clear();
            RejectedEntries.Clear();

            var root = JObject.Parse(json);
            ParseChannel(root["release"] as JObject, VersionType.Release);
            ParseChannel(root["preview"] as JObject, VersionType.Preview);
            ParseChannel(root["beta"] as JObject, VersionType.Beta);
        }

        private void ParseChannel(JObject channel, VersionType type)
        {
            if (channel == null) return;

            foreach (var property in channel.Properties())
            {
                string versionName = property.Name?.Trim();
                if (string.IsNullOrWhiteSpace(versionName)) continue;
                if (!MinecraftVersion.TryParse(versionName, out _)) continue;

                if (property.Value is JArray urlsArray)
                {
                    ProcessUrlList(versionName, type, urlsArray, null);
                }
                else if (property.Value is JObject archMap)
                {
                    foreach (var archProp in archMap.Properties())
                    {
                        if (archProp.Value is JArray archUrlsArray)
                        {
                            ProcessUrlList(versionName, type, archUrlsArray, archProp.Name);
                        }
                    }
                }
            }
        }

        private void ProcessUrlList(string versionName, VersionType type, JArray urls, string architectureHint)
        {
            if (urls == null || urls.Count == 0) return;

            var urlList = urls
                .Select(x => x?.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (urlList.Count == 0) return;

            // Expand with all CDN mirrors so the downloader can fall back automatically.
            // urlList = ExpandWithMirrors(urlList);

            // The resource file name is the exact package identity. Every resource listed for a version must name
            // the same identity; anything else is ambiguous and the entry is not catalogued (no guessing).
            var identities = new List<GdkPackageIdentity>();
            foreach (string url in urlList)
            {
                if (!GdkPackageIdentity.TryParseFromUrl(url, out GdkPackageIdentity urlIdentity))
                {
                    Reject(versionName, type, $"'{url}' does not name a package identity.");
                    return;
                }

                identities.Add(urlIdentity);
            }

            GdkPackageIdentity identity = identities[0];
            if (identities.Any(x => !x.Equals(identity)))
            {
                Reject(versionName, type, "its resources name different package identities.");
                return;
            }

            if (!MinecraftPackageFamilies.BelongsTo(identity, type))
            {
                Reject(versionName, type, $"{identity.FamilyName} is not the {MinecraftPackageFamilies.GetFamilyName(type)} family.");
                return;
            }

            if (!TryGetPackageVersion(versionName, out Version expectedPackageVersion) ||
                expectedPackageVersion != identity.Version)
            {
                Reject(versionName, type, $"package version {identity.Version} does not encode version {versionName}.");
                return;
            }

            if (!string.IsNullOrEmpty(architectureHint) &&
                !string.Equals(architectureHint, identity.Architecture, StringComparison.OrdinalIgnoreCase))
            {
                Reject(versionName, type, $"it is listed as {architectureHint} but the package is {identity.Architecture}.");
                return;
            }

            // Keep the historical UUID key (type, version, architecture) so existing installations still resolve.
            string architecture = !string.IsNullOrEmpty(architectureHint) ? architectureHint : identity.Architecture;
            string uuid = CreateStableUuid($"gdk:{type}:{versionName}:{architecture}").ToString();

            Versions.Add(new VersionInfoJson(versionName, uuid, type, architecture, PackageType.GDK, identity.FullName, urlList.ToArray()));
        }

        /// <summary>
        /// Minecraft GDK builds encode their version in the package version as Major.Minor.(Build*100+Revision).0,
        /// e.g. 1.21.120.4 -> 1.21.12004.0 and 1.26.40.5 -> 1.26.4005.0.
        /// </summary>
        public static bool TryGetPackageVersion(string versionName, out Version packageVersion)
        {
            packageVersion = null;
            if (!Version.TryParse(versionName, out Version version) || version.Build < 0 || version.Revision < 0)
                return false;
            if (version.Revision > 99)
                return false;

            packageVersion = new Version(version.Major, version.Minor, version.Build * 100 + version.Revision, 0);
            return true;
        }

        private void Reject(string versionName, VersionType type, string reason)
        {
            RejectedEntries.Add($"GdkLinks {type} {versionName} skipped: {reason}");
        }

        /// <summary>
        /// For each URL in the original list, derives mirror URLs by swapping the host
        /// component with each known CDN root, then deduplicates.
        /// Original URLs are kept first so they are tried before mirrors.
        /// </summary>
        private static List<string> ExpandWithMirrors(List<string> original)
        {
            var result = new List<string>(original);

            foreach (var baseUrl in original)
            {
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)) continue;

                // Derive the path+query portion of the original URL.
                string pathAndQuery = uri.PathAndQuery;

                foreach (var root in CdnRoots)
                {
                    string mirror = root.TrimEnd('/') + pathAndQuery;

                    // Skip if identical to one already in the list.
                    if (result.Any(u => string.Equals(u, mirror, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    result.Add(mirror);
                }
            }

            return result;
        }

        public static Guid CreateStableUuid(string key)
        {
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
            return new Guid(hash);
        }
    }
}
