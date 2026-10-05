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
        private string MicrosoftAccountId;

        private static readonly string[] communityDBUrls =
        {
            "https://mrarm.io/r/w10-vdb",
            "https://www.raythnetwork.co.uk/versions.php?type=json"
        };

        private static readonly string[] gdkLinksUrls =
        {
            "https://raw.githubusercontent.com/MinecraftBedrockArchiver/GdkLinks/refs/heads/master/urls.min.json",
            "https://raw.githubusercontent.com/MinecraftBedrockArchiver/GdkLinks/master/urls.json"
        };

        private string winstoreDBFile;
        private string communityDBFile;
        private string gdkLinksDBFile;

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
            string gdkLinksDBFile = null,
            string microsoftAccountId = null)
        {
            UserTokenIndex = userTokenIndex;
            MicrosoftAccountId = microsoftAccountId;
            this.winstoreDBFile = winstoreDBFile;
            this.communityDBFile = communityDBFile;

            this.gdkLinksDBFile =
                gdkLinksDBFile ??
                Path.Combine(
                    Path.GetDirectoryName(
                        communityDBFile) ?? ".",
                    "gdk_links_versions.json");
        }

        /// <summary>
        /// Downloads a Minecraft package.
        ///
        /// GDK packages use GdkLinks CDN URLs.
        /// UWP packages use the Microsoft Store download service.
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
            if (TryGetGdkDownloadUrls(
                    updateIdentity,
                    out var gdkUrls))
            {
                Exception lastError = null;

                foreach (var url in gdkUrls)
                {
                    try
                    {
                        Trace.WriteLine(
                            $"Downloading GDK package from CDN: {url}");

                        await DownloadFromDirectUrl(
                            url,
                            destination,
                            progress,
                            cancellationToken);

                        return;
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;

                        Trace.WriteLine(
                            $"CDN mirror failed: {url} — {ex.Message}");
                    }
                }

                throw new IOException(
                    $"All GdkLinks CDN mirrors failed for '{versionName}'",
                    lastError);
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

            // 1. Community database — UWP versions.
            VersionJsonDb communityDB =
                LoadJsonDBVersions(
                    communityDBFile);

            if (getNewVersions)
            {
                await UpdateDBFromURL(
                    communityDB,
                    communityDBFile,
                    communityDBUrls);
            }

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

            // 3. GdkLinks — GDK versions.
            await LoadGdkLinksVersions(
                getNewVersions);
        }

        private async Task LoadGdkLinksVersions(
            bool getNewVersions)
        {
            try
            {
                string cachePath =
                    gdkLinksDBFile;

                string rawJson = null;

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

                            rawJson =
                                await resp.Content.ReadAsStringAsync();

                            Directory.CreateDirectory(
                                Path.GetDirectoryName(
                                    cachePath) ?? ".");

                            File.WriteAllText(
                                cachePath,
                                rawJson);

                            Trace.WriteLine(
                                $"GdkLinks cache saved: {cachePath}");

                            break;
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(
                                $"GdkLinks fetch failed [{url}]: " +
                                ex.Message);
                        }
                    }
                }

                if (rawJson == null &&
                    File.Exists(cachePath))
                {
                    rawJson =
                        File.ReadAllText(
                            cachePath);
                }

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    Trace.WriteLine(
                        "No GdkLinks data available " +
                        "(no network and no cache).");

                    return;
                }

                var gdkDb =
                    new GdkLinksDb();

                gdkDb.Parse(
                    rawJson);

                foreach (
                    var pair
                    in gdkDb.DownloadUrlsByUuid)
                {
                    GdkDownloadUrls[pair.Key] =
                        pair.Value;
                }

                int added = 0;

                foreach (
                    var version
                    in gdkDb.Versions)
                {
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

                    added++;
                }

                Trace.WriteLine(
                    $"GdkLinks: {added} new version(s) added " +
                    $"(total CDN entries: {GdkDownloadUrls.Count}).");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    "LoadGdkLinksVersions failed:");

                Trace.WriteLine(ex);
            }
        }

        private async Task UpdateDBFromURL(
            VersionJsonDb db,
            string filePath,
            string[] urls)
        {
            foreach (var url in urls)
            {
                try
                {
                    var resp =
                        await HttpClient.GetAsync(
                            url);

                    resp.EnsureSuccessStatusCode();

                    string data =
                        await resp.Content.ReadAsStringAsync();

                    /*
                     * Do not delete the existing database before
                     * the new response has been downloaded and parsed.
                     */
                    var remoteDb =
                        new VersionJsonDb();

                    remoteDb.ParseRaw(
                        data,
                        GetVersionArches());

                    foreach (
                        var remoteVersion
                        in remoteDb.list)
                    {
                        if (!db.list.Any(x =>
                            x.uuid ==
                            remoteVersion.uuid &&
                            x.packageType ==
                            remoteVersion.packageType))
                        {
                            db.list.Add(
                                remoteVersion);
                        }
                    }

                    db.Save(
                        filePath);

                    InsertVersionsFromDB(
                        db);

                    Trace.WriteLine(
                        $"Community DB updated from: {url}");

                    return;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(
                        $"UpdateDBFromURL failed [{url}]: " +
                        ex.Message);
                }
            }

            Trace.TraceWarning(
                "All community DB URLs failed.");
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

                InsertVersionsFromDB(
                    jsonDb);
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

                InsertVersionsFromDB(
                    db);

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
                        () =>
                            string.IsNullOrWhiteSpace(
                                MicrosoftAccountId)
                                ? AuthenticationManager
                                    .Default
                                    .GetWUToken(
                                        UserTokenIndex)
                                : AuthenticationManager
                                    .Default
                                    .GetWUTokenForAccountId(
                                        MicrosoftAccountId));

                StoreNetwork.setMSAUserToken(
                    token);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"EnableUserAuthorization failed " +
                    $"({(string.IsNullOrWhiteSpace(MicrosoftAccountId) ? $"token index {UserTokenIndex}" : "linked Microsoft account")}): " +
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