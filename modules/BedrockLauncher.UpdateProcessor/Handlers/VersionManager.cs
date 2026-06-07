using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BedrockLauncher.UpdateProcessor.Authentication;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Databases;
using BedrockLauncher.UpdateProcessor.Enums;
using Newtonsoft.Json.Linq;

namespace BedrockLauncher.UpdateProcessor.Handlers
{
    public class VersionManager
    {
        #region Singleton management
        private static VersionManager _singleton = null;

        public static VersionManager Singleton
        {
            get
            {
                if (_singleton == null)
                {
                    Trace.TraceWarning("Trying to access uninitialized VersionManager singleton.");
                    return null;
                }
                else
                    return _singleton;
            }
            private set
            {
                if (_singleton != null)
                {
                    Trace.TraceWarning("Attempt to override VersionManager singleton denied.");
                }
                else
                    _singleton = value;
            }
        }

        public VersionManager()
        {
            Singleton = this;
            HttpClient.Timeout = Timeout.InfiniteTimeSpan;
        }

        #endregion

        public delegate void DownloadProgress(long current, long total);

        private int UserTokenIndex = 0;

        private const string communityDBUrl = "https://www.raythnetwork.co.uk/versions.php?type=json";
        private const string gdkPackageLinksUrl = "https://cdn.jsdelivr.net/gh/MinecraftBedrockArchiver/GdkLinks@latest/urls.json";

        private string winstoreDBFile;
        private string communityDBFile;

        private HttpClient HttpClient = new HttpClient();
        private StoreNetwork StoreNetwork = new StoreNetwork();
        private List<VersionInfoJson> Versions = new List<VersionInfoJson>();
        private readonly Dictionary<string, List<string>> DirectPackageLinksById = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly string[] DirectPackageMirrorHosts =
        {
            "assets1.xboxlive.com",
            "assets2.xboxlive.com",
            "d1.xboxlive.com",
            "d2.xboxlive.com",
            "xvcf1.xboxlive.com",
            "xvcf2.xboxlive.com",
            "assets1.xboxlive.cn",
            "assets2.xboxlive.cn",
            "d1.xboxlive.cn",
            "d2.xboxlive.cn"
        };

        public List<VersionInfoJson> GetVersions() => Versions.ToList();
        public void Init(int _userTokenIndex, string _winstoreDBFile, string _communityDBFile)
        {
            UserTokenIndex = _userTokenIndex;
            winstoreDBFile = _winstoreDBFile;
            communityDBFile = _communityDBFile;
        }

        public async Task DownloadVersion(string versionName, string updateIdentity, int revisionNumber, string destination, DownloadProgress progress, CancellationToken cancellationToken, VersionType type)
        {
            Exception directDownloadException = null;

            if (DirectPackageLinksById.TryGetValue(updateIdentity, out List<string> directLinks) && directLinks.Count > 0)
            {
                try
                {
                    await DownloadDirectVersion(directLinks, destination, progress, cancellationToken);
                    return;
                }
                catch (Exception ex)
                {
                    directDownloadException = ex;
                    TryDeleteFile(destination);
                    Trace.WriteLine("Direct package links failed. Trying Microsoft Store fallback when possible.");
                    Trace.WriteLine(ex);
                }
            }

            string link = null;
            try
            {
                link = await StoreNetwork.getDownloadLink(updateIdentity, revisionNumber, type);
            }
            catch (Exception ex)
            {
                if (directDownloadException != null)
                    throw new ArgumentException("Direct package mirrors failed and the Microsoft Store fallback is unavailable for this version.", directDownloadException);

                throw new ArgumentException(string.Format("Bad updateIdentity for {0}", versionName), ex);
            }

            if (link == null)
            {
                if (directDownloadException != null)
                    throw new ArgumentException("Direct package mirrors failed and the Microsoft Store fallback is unavailable for this version.", directDownloadException);

                throw new ArgumentException(string.Format("Bad updateIdentity for {0}", versionName));
            }

            Trace.WriteLine("Resolved download link: " + link);

            await DownloadFromUrl(link, destination, progress, cancellationToken);
        }

        private async Task DownloadDirectVersion(List<string> directLinks, string destination, DownloadProgress progress, CancellationToken cancellationToken)
        {
            Exception lastException = null;

            foreach (string link in GetDirectDownloadCandidates(directLinks))
            {
                try
                {
                    Trace.WriteLine("Resolved direct GDK download link: " + link);
                    await DownloadFromUrl(link, destination, progress, cancellationToken);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    TryDeleteFile(destination);
                    TryDeleteFile(GetPartialDownloadPath(destination));
                    Trace.WriteLine("Direct GDK package download failed: " + link);
                    Trace.WriteLine(ex);
                }
            }

            throw new ArgumentException("Bad direct GDK package link.", lastException);
        }

        private async Task DownloadFromUrl(string link, string destination, DownloadProgress progress, CancellationToken cancellationToken)
        {
            string partialDestination = GetPartialDownloadPath(destination);
            TryDeleteFile(destination);

            long existingBytes = 0;
            if (File.Exists(partialDestination))
                existingBytes = new FileInfo(partialDestination).Length;

            using (var request = new HttpRequestMessage(HttpMethod.Get, link))
            {
                request.Headers.UserAgent.ParseAdd("BedrockLauncher");
                request.Headers.Accept.ParseAdd("*/*");

                if (existingBytes > 0)
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingBytes, null);

                using var resp = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (existingBytes > 0 && resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    TryDeleteFile(partialDestination);
                    existingBytes = 0;
                }

                if (resp.StatusCode != System.Net.HttpStatusCode.OK &&
                    resp.StatusCode != System.Net.HttpStatusCode.PartialContent)
                    resp.EnsureSuccessStatusCode();

                if (existingBytes > 0 && resp.StatusCode != System.Net.HttpStatusCode.PartialContent)
                    throw new IOException($"The server did not resume the partial download correctly. Status: {resp.StatusCode}");

                string contentType = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
                long responseSize = resp.Content.Headers.ContentLength ?? 0;
                long totalSize = responseSize;

                if (resp.Content.Headers.ContentRange?.Length.HasValue == true)
                    totalSize = resp.Content.Headers.ContentRange.Length.Value;
                else if (existingBytes > 0 && responseSize > 0)
                    totalSize = existingBytes + responseSize;

                if (LooksLikeBadPackageResponse(contentType, totalSize > 0 ? totalSize : responseSize))
                    throw new InvalidDataException($"The server returned {contentType} instead of a Minecraft package.");

                using (var inStream = await resp.Content.ReadAsStreamAsync(cancellationToken))
                using (var outStream = new FileStream(
                    partialDestination,
                    existingBytes > 0 ? FileMode.Append : FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    128 * 1024))
                {
                    long transferred = existingBytes;
                    progress(transferred, totalSize);
                    byte[] buf = new byte[128 * 1024];
                    DateTime lastProgress = DateTime.UtcNow;

                    while (true)
                    {
                        int n = await inStream.ReadAsync(buf, 0, buf.Length, cancellationToken);
                        if (n == 0)
                            break;

                        await outStream.WriteAsync(buf, 0, n, cancellationToken);
                        transferred += n;

                        if (DateTime.UtcNow - lastProgress >= TimeSpan.FromMilliseconds(250))
                        {
                            progress(transferred, totalSize);
                            lastProgress = DateTime.UtcNow;
                        }
                    }

                    progress(transferred, totalSize);

                    if (totalSize > 0 && transferred != totalSize)
                        throw new IOException($"Incomplete package download. Expected {totalSize} bytes, got {transferred} bytes.");

                    if (transferred < 64 * 1024)
                        throw new InvalidDataException($"Downloaded package is too small ({transferred} bytes).");
                }
            }

            File.Move(partialDestination, destination, true);
        }

        private static string GetPartialDownloadPath(string destination)
        {
            return destination + ".download";
        }

        private static IEnumerable<string> GetDirectDownloadCandidates(IEnumerable<string> directLinks)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string rawLink in directLinks
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .OrderBy(GetDirectLinkPriority))
            {
                foreach (string candidate in ExpandDirectDownloadLink(rawLink.Trim()))
                {
                    if (seen.Add(candidate))
                        yield return candidate;
                }
            }
        }

        private static IEnumerable<string> ExpandDirectDownloadLink(string link)
        {
            if (!Uri.TryCreate(link, UriKind.Absolute, out Uri uri))
            {
                yield return link;
                yield break;
            }

            string originalScheme = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && IsXboxLiveHost(uri.Host)
                ? "http"
                : uri.Scheme;

            yield return ReplaceUriHostAndScheme(uri, uri.Host, originalScheme);

            if (!IsXboxLiveHost(uri.Host))
                yield break;

            foreach (string mirrorHost in DirectPackageMirrorHosts)
            {
                if (mirrorHost.Equals(uri.Host, StringComparison.OrdinalIgnoreCase))
                    continue;

                yield return ReplaceUriHostAndScheme(uri, mirrorHost, "http");
            }
        }

        private static bool IsXboxLiveHost(string host)
        {
            return host.EndsWith(".xboxlive.com", StringComparison.OrdinalIgnoreCase) ||
                   host.EndsWith(".xboxlive.cn", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReplaceUriHostAndScheme(Uri uri, string host, string scheme)
        {
            var builder = new UriBuilder(uri)
            {
                Host = host,
                Scheme = scheme,
                Port = scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ? 80 : 443
            };

            return builder.Uri.ToString();
        }

        private static int GetDirectLinkPriority(string link)
        {
            if (!Uri.TryCreate(link, UriKind.Absolute, out Uri uri))
                return 99;

            int mirrorIndex = Array.FindIndex(DirectPackageMirrorHosts, host => host.Equals(uri.Host, StringComparison.OrdinalIgnoreCase));
            if (mirrorIndex >= 0)
                return mirrorIndex;

            return 99;
        }

        private static bool LooksLikeBadPackageResponse(string contentType, long totalSize)
        {
            if (contentType.IndexOf("text/", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (contentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return totalSize > 0 && totalSize < 64 * 1024;
        }

        public async Task LoadVersions(bool getNewVersions, bool checkMicrosoftStore)
        {
            Versions.Clear();
            DirectPackageLinksById.Clear();

            await EnableUserAuthorization();
            VersionJsonDb communityDB = LoadJsonDBVersions(communityDBFile);

            if (getNewVersions)
            {
                await UpdateDBFromURL(communityDB, communityDBFile, communityDBUrl);
            }

            VersionJsonDb winStoreDB = LoadJsonDBVersions(winstoreDBFile);

            if (getNewVersions && checkMicrosoftStore)
            {
                await UpdateDBFromStore(winStoreDB, winstoreDBFile);
            }

            await UpdateDBFromGdkPackageLinks(winStoreDB, winstoreDBFile);
        }

        private async Task UpdateDBFromURL(VersionJsonDb db, string filePath, string url)
        {
            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);
                var resp = await HttpClient.GetAsync(url);
                resp.EnsureSuccessStatusCode();
                var data = await resp.Content.ReadAsStringAsync();
                db.PraseRaw(data, GetVersionArches());
                db.Save(filePath);
                InsertVersionsFromDB(db);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("UpdateDBFromURL Failed!");
                Trace.WriteLine("File: " + filePath);
                Trace.WriteLine("Url: " + url);
                Trace.WriteLine(ex);
            }
        }
        /// <summary>
        /// Updates the databases by fetching the latest version
        /// </summary>
        /// <param name="JsonDb">JSON database</param>
        /// <param name="JsonFilePath">Path to the file storing the JSON database</param>
        /// <returns></returns>
        private async Task UpdateDBFromStore(VersionJsonDb JsonDb, string JsonFilePath)
        {
            try
            {
                if (File.Exists(JsonFilePath)) File.Delete(JsonFilePath);
                await UpdateDB(VersionType.Release, JsonDb);
                await UpdateDB(VersionType.Preview, JsonDb);
                JsonDb.Save(JsonFilePath);
                InsertVersionsFromDB(JsonDb);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("UpdateDBFromStore Failed!");
                Trace.WriteLine(ex);
            }
        }
        private async Task UpdateDB(VersionType type, VersionJsonDb JsonDb)
        {
            try
            {
                var config = await StoreNetwork.fetchConfigLastChanged();
                var cookie = await StoreNetwork.fetchCookie(config, type);

                List<string> knownVersions = JsonDb.GetVersions().ConvertAll(x => x.GetUUID().ToString());
                List<UpdateInfo> result = await StoreManager.CheckForGDKVersions(StoreNetwork, type, cookie, knownVersions);
                JsonDb.AddVersion(result, type);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("UpdateDBFromStore.UpdateDB Failed!");
                Trace.WriteLine("isBeta: " + type);
                Trace.WriteLine(ex);
            }
        }

        private async Task UpdateDBFromGdkPackageLinks(VersionJsonDb JsonDb, string JsonFilePath)
        {
            try
            {
                Dictionary<string, List<string>> packageLinks = await GetGdkPackageLinks();
                RemoveBadGeneratedModernGdkVersions(JsonDb);

                var groupedPackages = packageLinks
                    .Select(x => new
                    {
                        PackageVersion = x.Key,
                        DisplayVersion = GetDisplayVersion(x.Key),
                        Links = x.Value
                    })
                    .Where(x => IsSupportedModernGdkVersion(x.DisplayVersion))
                    .GroupBy(x => x.DisplayVersion, StringComparer.OrdinalIgnoreCase);

                foreach (var versionGroup in groupedPackages)
                {
                    var matchingPackage = versionGroup
                        .OrderByDescending(x => ParseLooseVersion(x.PackageVersion))
                        .FirstOrDefault();

                    if (matchingPackage == null || matchingPackage.Links.Count == 0) continue;

                    string displayVersion = versionGroup.Key;
                    string architecture = matchingPackage.Links
                        .Select(GetArchitectureFromUrl)
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "x64";
                    string id = GetStableDirectPackageId(displayVersion, architecture);

                    DirectPackageLinksById[id] = matchingPackage.Links;

                    if (JsonDb.list.Exists(x => x.GetUUID().ToString().Equals(id, StringComparison.OrdinalIgnoreCase))) continue;
                    if (JsonDb.list.Exists(x => x.GetVersion() == displayVersion && x.GetArchitecture() == architecture && x.GetVersionType() == VersionType.Release)) continue;

                    JsonDb.list.Add(new VersionInfoJson(displayVersion, id, VersionType.Release, architecture));
                }

                JsonDb.Save(JsonFilePath);
                InsertVersionsFromDB(JsonDb);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("UpdateDBFromGdkPackageLinks Failed!");
                Trace.WriteLine(ex);
            }
        }

        private async Task<Dictionary<string, List<string>>> GetGdkPackageLinks()
        {
            using (var resp = await HttpClient.GetAsync(gdkPackageLinksUrl))
            {
                resp.EnsureSuccessStatusCode();
                string data = await resp.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(data);
                var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                JObject releaseJson = json["release"] as JObject ?? json;

                foreach (JProperty property in releaseJson.Properties())
                {
                    IEnumerable<string> rawUrls = property.Value.Type == JTokenType.Array
                        ? property.Value.Values<string>()
                        : property.Value.ToString().Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    List<string> urls = rawUrls
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Where(IsWindowsMinecraftPackageUrl)
                        .ToList();

                    if (urls.Count > 0) result[property.Name] = urls;
                }

                return result;
            }
        }

        private static bool IsWindowsMinecraftPackageUrl(string url)
        {
            string lower = url.ToLowerInvariant();
            return lower.Contains("microsoft.minecraftuwp") &&
                   (lower.EndsWith(".appx") || lower.EndsWith(".msix") || lower.EndsWith(".msixvc") || lower.Contains(".appx?") || lower.Contains(".msix?") || lower.Contains(".msixvc?"));
        }

        private static string GetSupportedVersionKey(string packageVersion)
        {
            Version version = ParseLooseVersion(packageVersion);

            if (version.Major == 1 && version.Minor >= 26)
                return GetDisplayVersion(packageVersion);

            if (version.Major == 1 && version.Build >= 100)
                return $"1.{version.Minor}.{version.Build}";

            return FormatLooseVersion(version);
        }

        private static string GetDisplayVersion(string packageVersion)
        {
            Version version = ParseLooseVersion(packageVersion);

            if (version.Major == 1 && version.Minor >= 26)
            {
                if (version.Build >= 100)
                {
                    int feature = version.Build / 100;
                    int patch = version.Build % 100;
                    return patch > 0 ? $"{version.Minor}.{feature}.{patch}" : $"{version.Minor}.{feature}";
                }

                return version.Revision > 0
                    ? $"{version.Minor}.{version.Build}.{version.Revision}"
                    : $"{version.Minor}.{version.Build}";
            }

            return FormatLooseVersion(version);
        }

        private static void RemoveBadGeneratedModernGdkVersions(VersionJsonDb db)
        {
            db.list.RemoveAll(version =>
            {
                if (version.GetVersionType() != VersionType.Release)
                    return false;

                Version parsed = ParseLooseVersion(version.GetVersion());
                return parsed.Major == 26 && parsed.Minor == 0 && parsed.Build >= 10;
            });
        }

        private static bool IsSupportedModernGdkVersion(string displayVersion)
        {
            Version version = ParseLooseVersion(displayVersion);

            if (version.Major >= 26)
                return true;

            return version.Major == 1 &&
                   (version.Minor > 21 || (version.Minor == 21 && version.Build >= 130));
        }

        private static string GetArchitectureFromUrl(string url)
        {
            string lower = url.ToLowerInvariant();
            if (lower.Contains("_arm64_")) return "arm64";
            if (lower.Contains("_arm_")) return "arm";
            if (lower.Contains("_x86_")) return "x86";
            if (lower.Contains("_x64_")) return "x64";
            return "x64";
        }

        private static string GetStableDirectPackageId(string version, string architecture)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes("gdk|" + version + "|" + architecture));
                hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
                hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
                return new Guid(hash).ToString();
            }
        }

        private static Version ParseLooseVersion(string version)
        {
            string[] parts = version.Split('.');
            int[] values = new int[] { 0, 0, 0, 0 };

            for (int i = 0; i < parts.Length && i < values.Length; i++)
            {
                int.TryParse(parts[i], out values[i]);
            }

            return new Version(values[0], values[1], values[2], values[3]);
        }

        private static string FormatLooseVersion(Version version)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Best effort cleanup after a failed package mirror.
            }
        }

        private VersionJsonDb LoadJsonDBVersions(string filePath)
        {
            try
            {
                VersionJsonDb db = new VersionJsonDb();
                db.ReadJson(filePath, GetVersionArches());
                RemoveBadGeneratedModernGdkVersions(db);
                db.WriteJson(filePath);
                InsertVersionsFromDB(db);
                return db;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("LoadJsonDBVersions Failed! Generating Blank VersionJsonDb");
                Trace.WriteLine("File: " + filePath);
                Trace.WriteLine(ex);
                var db = new VersionJsonDb();
                db.Save(filePath);
                return db;
            }

        }
        private void InsertVersionsFromDB(VersionJsonDb db)
        {
            foreach (VersionInfoJson version in db.list)
            {
                if (!MinecraftVersion.TryParse(version.GetVersion(), out MinecraftVersion ver)) continue;
                if (Versions.Exists(x => x.GetUUID() == version.GetUUID())) continue;
                if (Versions.Exists(x => x.GetVersion() == version.GetVersion() && x.GetArchitecture() == version.GetArchitecture())) continue;
                Versions.Add(version);
            }
        }
        private async Task EnableUserAuthorization()
        {
            try
            {
                var token = await Task.Run(() => AuthenticationManager.Default.GetWUToken(UserTokenIndex));
                StoreNetwork.setMSAUserToken(token);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }

        }
        private Dictionary<Guid, string> GetVersionArches()
        {
            return Versions.ToDictionary(x => x.GetUUID(), x => x.GetArchitecture());
        }
    }
}
