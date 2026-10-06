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

        public Dictionary<string, List<string>> DownloadUrlsByUuid { get; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public List<VersionInfoJson> Versions { get; } = new List<VersionInfoJson>();

        public void Parse(string json)
        {
            DownloadUrlsByUuid.Clear();
            Versions.Clear();

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

            string architecture = !string.IsNullOrEmpty(architectureHint) 
                ? architectureHint 
                : InferArchitecture(urlList[0]);
                
            string uuid = CreateStableUuid($"gdk:{type}:{versionName}:{architecture}").ToString();

            Versions.Add(new VersionInfoJson(versionName, uuid, type, architecture, PackageType.GDK));
            DownloadUrlsByUuid[uuid] = urlList;
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

        private static string InferArchitecture(string url)
        {
            string lower = url.ToLowerInvariant();
            if (lower.Contains("_arm64_") || lower.Contains("-arm64-")) return "arm64";
            if (lower.Contains("_x86_") || lower.Contains("-x86-")) return "x86";
            return "x64";
        }

        public static Guid CreateStableUuid(string key)
        {
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
            return new Guid(hash);
        }
    }
}
