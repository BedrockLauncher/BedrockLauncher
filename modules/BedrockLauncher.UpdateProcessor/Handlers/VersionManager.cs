using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BedrockLauncher.UpdateProcessor.Authentication;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Databases;
using BedrockLauncher.UpdateProcessor.Enums;

namespace BedrockLauncher.UpdateProcessor.Handlers
{
    public class VersionManager
    {
        #region Singleton management

        private static VersionManager _singleton;

        public static VersionManager Singleton
        {
            get
            {
                if (_singleton == null)
                    Trace.TraceWarning(
                        "Trying to access uninitialized VersionManager singleton.");

                return _singleton;
            }

            private set
            {
                if (_singleton != null)
                {
                    Trace.TraceWarning(
                        "Attempt to override VersionManager singleton denied.");
                }
                else
                {
                    _singleton = value;
                }
            }
        }

        public VersionManager()
        {
            Singleton = this;
        }

        #endregion

        #region Delegates

        public delegate void DownloadProgress(
            long current,
            long total);

        #endregion

        #region Configuration

        private int UserTokenIndex;
        // The project's curated UWP version list. It is the only community source: entries removed from it must
        // disappear from the launcher too, so the local copy is replaced by it, never merged with another list.
        private const string communityDBUrl =
            "https://www.raythnetwork.co.uk/versions.php?type=json";

        // GDK build list (GdkLinks format), served by the project.
        private static readonly string[] gdkLinksUrls =
        {
            "https://www.raythnetwork.co.uk/gdk.urls.min.json",
            "https://www.raythnetwork.co.uk/gdk.urls.json"
        };

        private string winstoreDBFile;
        private string communityDBFile;
        private string gdkLinksDBFile;
        private string gdkVersionsDBFile;

        private readonly HttpClient HttpClient =
            new HttpClient();

        private readonly StoreNetwork StoreNetwork =
            new StoreNetwork();

        private readonly List<VersionInfoJson> Versions =
            new List<VersionInfoJson>();

        private readonly Dictionary<string, List<string>> GdkDownloadUrls =
            new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Public API

        public List<VersionInfoJson> GetVersions()
        {
            return Versions.ToList();
        }

        public bool TryGetGdkDownloadUrls(
            string versionUuid,
            out List<string> urls)
        {
            if (GdkDownloadUrls.TryGetValue(
                    versionUuid ?? string.Empty,
                    out urls) &&
                urls != null &&
                urls.Count > 0)
            {
                return true;
            }

            urls = null;
            return false;
        }

        public void SetMSAUserToken(string token)
        {
            StoreNetwork.setMSAUserToken(token);
        }

        public void Init(
            int userTokenIndex,
            string winstoreDBFile,
            string communityDBFile,
            string gdkLinksDBFile,
            string gdkVersionsDBFile)
        {
            UserTokenIndex = userTokenIndex;
            this.winstoreDBFile = winstoreDBFile;
            this.communityDBFile = communityDBFile;
            this.gdkLinksDBFile = gdkLinksDBFile;
            this.gdkVersionsDBFile = gdkVersionsDBFile;
        }

        /// <summary>
        /// Downloads a UWP Minecraft package through the Microsoft Store download service.
        /// GDK versions never come through here (see <see cref="DownloadGdkPackage"/>).
        /// </summary>
        public async Task DownloadVersion(
            string versionName,
            string updateIdentity,
            int revisionNumber,
            string destination,
            DownloadProgress progress,
            CancellationToken cancellationToken,
            VersionType type)
        {
            if (GdkDownloadUrls.ContainsKey(updateIdentity ?? string.Empty))
            {
                throw new InvalidOperationException(
                    $"'{versionName}' is a GDK version and cannot be downloaded through the UWP (Store) pipeline.");
            }

            string link =
                await StoreNetwork.getDownloadLink(
                    updateIdentity,
                    revisionNumber,
                    type);

            if (link == null)
            {
                throw new ArgumentException(
                    $"Could not resolve download link for '{versionName}' " +
                    $"(updateId: {updateIdentity})");
            }

            Trace.WriteLine(
                $"Downloading UWP package: {link}");

            await DownloadFromDirectUrl(
                link,
                destination,
                progress,
                cancellationToken);
        }

        /// <summary>
        /// Downloads exactly the required GDK package. Only resources whose file name is the required package
        /// identity are used; there is no fallback to another version or to the Store pipeline.
        /// </summary>
        public async Task DownloadGdkPackage(
            string versionUuid,
            GdkPackageIdentity requiredPackage,
            string destination,
            DownloadProgress progress,
            CancellationToken cancellationToken)
        {
            if (requiredPackage == null)
                throw new ArgumentNullException(nameof(requiredPackage));

            TryGetGdkDownloadUrls(versionUuid, out List<string> listedUrls);
            string[] urls = VersionJsonDb.FilterUrlsForIdentity(listedUrls, requiredPackage);

            if (urls.Length == 0)
            {
                throw new IOException(
                    $"No download resource is catalogued for {requiredPackage.FullName}.");
            }

            Exception lastError = null;

            foreach (var url in urls)
            {
                try
                {
                    Trace.WriteLine(
                        $"Downloading GDK package {requiredPackage.FullName} from CDN: {url}");

                    await DownloadFromDirectUrl(
                        url,
                        destination,
                        progress,
                        cancellationToken);

                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;

                    Trace.WriteLine(
                        $"CDN mirror failed: {url} — {ex.Message}");
                }
            }

            throw new IOException(
                $"All CDN mirrors failed for {requiredPackage.FullName}",
                lastError);
        }

        #endregion

        #region Download

        private async Task DownloadFromDirectUrl(
            string url,
            string destination,
            DownloadProgress progress,
            CancellationToken cancellationToken)
        {
            string temporaryPath =
                destination + ".download";

            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);

            try
            {
                using var resp =
                    await HttpClient.GetAsync(
                        url,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                resp.EnsureSuccessStatusCode();

                long totalSize =
                    resp.Content.Headers.ContentLength ?? -1;

                long transferred = 0;

                byte[] buffer =
                    new byte[1024 * 1024];

                progress?.Invoke(
                    0,
                    totalSize > 0
                        ? totalSize
                        : 1);

                using (
                    var input =
                        await resp.Content.ReadAsStreamAsync())
                using (
                    var output =
                        new FileStream(
                            temporaryPath,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None,
                            65536,
                            useAsync: true))
                {
                    while (true)
                    {
                        int read =
                            await input.ReadAsync(
                                buffer,
                                0,
                                buffer.Length,
                                cancellationToken);

                        if (read == 0)
                            break;

                        await output.WriteAsync(
                            buffer,
                            0,
                            read,
                            cancellationToken);

                        transferred += read;

                        /*
                         * IMPORTANT:
                         *
                         * Do not create a new Task.Run here.
                         *
                         * The old implementation scheduled progress updates
                         * asynchronously and cancelled the previous update
                         * whenever another chunk arrived. With large GDK
                         * packages this caused most updates to disappear.
                         *
                         * Report directly to PackageHandler instead.
                         */
                        progress?.Invoke(
                            transferred,
                            totalSize > 0
                                ? totalSize
                                : transferred);
                    }
                }

                if (!File.Exists(temporaryPath))
                {
                    throw new IOException(
                        "Il pacchetto scaricato non esiste.");
                }

                if (new FileInfo(temporaryPath).Length == 0)
                {
                    throw new IOException(
                        "Il pacchetto scaricato è vuoto.");
                }

                if (File.Exists(destination))
                    File.Delete(destination);

                File.Move(
                    temporaryPath,
                    destination);

                progress?.Invoke(
                    transferred,
                    totalSize > 0
                        ? totalSize
                        : transferred);
            }
            catch
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                throw;
            }
        }

        #endregion

        #region Version loading pipeline

        public async Task LoadVersions(
            bool getNewVersions,
            bool checkMicrosoftStore)
        {
            Versions.Clear();
            GdkDownloadUrls.Clear();

            await EnableUserAuthorization();

            // 1. Community database — UWP versions. Listed only after the refresh, so entries removed from the
            //    curated list are not shown from the previous local copy.
            VersionJsonDb communityDB =
                LoadJsonDBVersions(
                    communityDBFile);

            if (getNewVersions)
            {
                await UpdateDBFromURL(
                    communityDB,
                    communityDBFile,
                    communityDBUrl);
            }

            InsertVersionsFromDB(
                communityDB);

            // 2. Microsoft Store database — UWP versions.
            VersionJsonDb winStoreDB =
                LoadJsonDBVersions(
                    winstoreDBFile);

            if (getNewVersions &&
                checkMicrosoftStore)
            {
                await UpdateDBFromStore(
                    winStoreDB,
                    winstoreDBFile);
            }

            InsertVersionsFromDB(
                winStoreDB);

            // 3. GdkLinks — GDK versions.
            await LoadGdkLinksVersions(
                getNewVersions);
        }

        /// <summary>
        /// Loads the GDK catalog.
        ///
        /// Discovery and selection are separate: GdkLinks only answers "which GDK builds exist", the persisted GDK
        /// catalog (gdkVersionsDBFile) answers "which exact package does Minecraft X require". A discovered entry is
        /// added when the version is new; an already catalogued version keeps its package identity, so a refresh,
        /// a GdkLinks change or a newer build can never re-point an existing version.
        /// </summary>
        private async Task LoadGdkLinksVersions(
            bool getNewVersions)
        {
            VersionJsonDb gdkCatalog =
                LoadGdkCatalog();

            try
            {
                string rawJson =
                    await ReadGdkLinksManifest(
                        getNewVersions);

                if (!string.IsNullOrWhiteSpace(rawJson))
                {
                    var gdkLinks =
                        new GdkLinksDb();

                    gdkLinks.Parse(
                        rawJson);

                    foreach (string rejected in gdkLinks.RejectedEntries)
                        Trace.WriteLine(rejected);

                    MergeGdkDiscoveries(
                        gdkCatalog,
                        gdkLinks.Versions);

                    gdkCatalog.Save(
                        gdkVersionsDBFile);
                }
                else
                {
                    Trace.WriteLine(
                        "No GdkLinks data available (no network and no cache); " +
                        "using the persisted GDK catalog only.");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    "GdkLinks discovery failed; using the persisted GDK catalog only:");

                Trace.WriteLine(ex);
            }

            InsertGdkCatalog(
                gdkCatalog);
        }

        /// <summary>Merges discovered GDK builds into the persisted catalog (add-only) and logs the outcome.</summary>
        public static List<GdkCatalogMergeResult> MergeGdkDiscoveries(
            VersionJsonDb gdkCatalog,
            IEnumerable<VersionInfoJson> discovered)
        {
            var results =
                new List<GdkCatalogMergeResult>();

            foreach (var version in discovered)
            {
                GdkCatalogMergeResult result =
                    gdkCatalog.MergeGdkEntry(
                        version);

                results.Add(result);

                if (result == GdkCatalogMergeResult.Added)
                {
                    Trace.WriteLine(
                        $"GDK catalog: Minecraft {version.version} ({version.type}) -> {version.packageIdentity}");
                }
                else if (result == GdkCatalogMergeResult.IdentityConflict)
                {
                    VersionInfoJson persisted =
                        gdkCatalog.list.First(x =>
                            x.uuid == version.uuid &&
                            x.packageType == PackageType.GDK);

                    Trace.TraceWarning(
                        $"GDK catalog: GdkLinks now lists {version.packageIdentity} for Minecraft {version.version} " +
                        $"({version.type}), but the version is associated with {persisted.packageIdentity}. " +
                        "The persisted association is kept.");
                }
                else if (result == GdkCatalogMergeResult.Rejected)
                {
                    Trace.TraceWarning(
                        $"GDK catalog: Minecraft {version.version} ({version.type}) has no valid package identity; not catalogued.");
                }
            }

            return results;
        }

        private VersionJsonDb LoadGdkCatalog()
        {
            var catalog =
                new VersionJsonDb();

            try
            {
                catalog.ReadJson(
                    gdkVersionsDBFile);
            }
            catch (Exception ex)
            {
                // Never overwrite an unreadable catalog: the associations it holds cannot be rebuilt reliably.
                string backup =
                    gdkVersionsDBFile + ".unreadable-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                Trace.TraceError(
                    $"The GDK catalog '{gdkVersionsDBFile}' could not be read; it is preserved as '{backup}'.");

                Trace.WriteLine(ex);

                if (File.Exists(gdkVersionsDBFile))
                    File.Move(gdkVersionsDBFile, backup);

                catalog =
                    new VersionJsonDb();
            }

            // The GDK catalog holds GDK entries only.
            catalog.list.RemoveAll(x =>
                x.packageType != PackageType.GDK);

            return catalog;
        }

        private async Task<string> ReadGdkLinksManifest(
            bool getNewVersions)
        {
            if (getNewVersions)
            {
                foreach (var url in gdkLinksUrls)
                {
                    try
                    {
                        Trace.WriteLine(
                            $"Fetching GdkLinks manifest from: {url}");

                        var resp =
                            await HttpClient.GetAsync(url);

                        resp.EnsureSuccessStatusCode();

                        string rawJson =
                            await resp.Content.ReadAsStringAsync();

                        Directory.CreateDirectory(
                            Path.GetDirectoryName(
                                gdkLinksDBFile) ?? ".");

                        File.WriteAllText(
                            gdkLinksDBFile,
                            rawJson);

                        Trace.WriteLine(
                            $"GdkLinks cache saved: {gdkLinksDBFile}");

                        return rawJson;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(
                            $"GdkLinks fetch failed [{url}]: " +
                            ex.Message);
                    }
                }
            }

            return File.Exists(gdkLinksDBFile)
                ? File.ReadAllText(gdkLinksDBFile)
                : null;
        }

        private void InsertGdkCatalog(
            VersionJsonDb gdkCatalog)
        {
            int added = 0;

            foreach (var version in gdkCatalog.list)
            {
                GdkPackageIdentity required =
                    version.GetRequiredGdkPackage();

                if (required == null)
                {
                    Trace.TraceWarning(
                        $"GDK catalog entry {version.version} ({version.uuid}) has no valid package identity; skipped.");

                    continue;
                }

                if (!MinecraftVersion.TryParse(
                        version.GetVersion(),
                        out _))
                {
                    continue;
                }

                /*
                 * Do NOT remove an UWP version just because
                 * a GDK version has the same version number.
                 *
                 * UWP and GDK are different package types and
                 * must both remain selectable.
                 */
                if (Versions.Exists(x =>
                    x.GetUUID() ==
                    version.GetUUID() &&
                    x.GetPackageType() ==
                    version.GetPackageType()))
                {
                    continue;
                }

                Versions.Add(
                    version);

                GdkDownloadUrls[version.uuid.ToString()] =
                    VersionJsonDb.FilterUrlsForIdentity(
                        version.downloadUrls,
                        required)
                    .ToList();

                added++;
            }

            Trace.WriteLine(
                $"GDK catalog: {added} version(s) available.");
        }

        /// <summary>
        /// Replaces the local database with the remote list. The local copy is kept untouched until the remote
        /// response has been downloaded and parsed, so a failed refresh never empties it.
        /// </summary>
        private async Task UpdateDBFromURL(
            VersionJsonDb db,
            string filePath,
            string url)
        {
            try
            {
                var resp =
                    await HttpClient.GetAsync(
                        url);

                resp.EnsureSuccessStatusCode();

                string data =
                    await resp.Content.ReadAsStringAsync();

                // Parse into a scratch database first: a malformed response must not clear the local list.
                new VersionJsonDb().ParseRaw(
                    data,
                    GetVersionArches());

                db.ParseRaw(
                    data,
                    GetVersionArches());

                db.Save(
                    filePath);

                Trace.WriteLine(
                    $"Community DB replaced from: {url} ({db.list.Count} versions)");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"UpdateDBFromURL failed [{url}]; keeping the local copy: " +
                    ex.Message);
            }
        }

        private async Task UpdateDBFromStore(
            VersionJsonDb jsonDb,
            string jsonFilePath)
        {
            try
            {
                if (File.Exists(jsonFilePath))
                    File.Delete(jsonFilePath);

                await FetchStoreVersions(
                    VersionType.Release,
                    jsonDb);

                await FetchStoreVersions(
                    VersionType.Preview,
                    jsonDb);

                jsonDb.Save(
                    jsonFilePath);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    "UpdateDBFromStore failed:");

                Trace.WriteLine(ex);
            }
        }

        private async Task FetchStoreVersions(
            VersionType type,
            VersionJsonDb jsonDb)
        {
            try
            {
                var config =
                    await StoreNetwork.fetchConfigLastChanged();

                var cookie =
                    await StoreNetwork.fetchCookie(
                        config,
                        type);

                var knownVersions =
                    jsonDb
                        .GetVersions()
                        .ConvertAll(
                            x => x.GetUUID().ToString());

                var result =
                    await StoreManager.CheckForUWPVersions(
                        StoreNetwork,
                        type,
                        cookie,
                        knownVersions);

                jsonDb.AddVersion(
                    result,
                    type);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"FetchStoreVersions failed " +
                    $"(type={type}):");

                Trace.WriteLine(ex);
            }
        }

        private VersionJsonDb LoadJsonDBVersions(
            string filePath)
        {
            try
            {
                var db =
                    new VersionJsonDb();

                db.ReadJson(
                    filePath,
                    GetVersionArches());

                db.WriteJson(
                    filePath);

                return db;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(
                    $"LoadJsonDBVersions failed for '{filePath}' " +
                    "— creating empty DB.");

                Trace.WriteLine(ex);

                var db =
                    new VersionJsonDb();

                db.Save(
                    filePath);

                return db;
            }
        }

        private void InsertVersionsFromDB(
            VersionJsonDb db)
        {
            foreach (
                var version
                in db.list)
            {
                // Community / Store databases describe UWP packages only. GDK versions come exclusively from the
                // GDK catalog, which carries their required package identity.
                if (version.GetPackageType() != PackageType.UWP)
                {
                    Trace.WriteLine(
                        $"Ignoring non-UWP entry {version.GetVersion()} in a UWP version database.");

                    continue;
                }

                if (!MinecraftVersion.TryParse(
                        version.GetVersion(),
                        out _))
                {
                    continue;
                }

                if (Versions.Exists(x =>
                    x.GetUUID() ==
                    version.GetUUID() &&
                    x.GetPackageType() ==
                    version.GetPackageType()))
                {
                    continue;
                }

                /*
                 * UWP and GDK must be allowed to coexist.
                 *
                 * PackageType is part of the identity here.
                 */
                if (Versions.Exists(x =>
                    x.GetVersion() ==
                    version.GetVersion() &&
                    x.GetArchitecture() ==
                    version.GetArchitecture() &&
                    x.GetVersionType() ==
                    version.GetVersionType() &&
                    x.GetPackageType() ==
                    version.GetPackageType()))
                {
                    continue;
                }

                Versions.Add(
                    version);
            }
        }

        #endregion

        #region Authentication

        private async Task EnableUserAuthorization()
        {
            try
            {
                var token =
                    await Task.Run(
                        () => AuthenticationManager
                            .Default
                            .GetWUToken(
                                UserTokenIndex));

                StoreNetwork.setMSAUserToken(
                    token);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"EnableUserAuthorization failed (token index {UserTokenIndex}): " +
                    ex.Message);
            }
        }

        #endregion

        #region Helpers

        private Dictionary<Guid, string> GetVersionArches()
        {
            return Versions.ToDictionary(
                x => x.GetUUID(),
                x => x.GetArchitecture());
        }

        #endregion
    }
}