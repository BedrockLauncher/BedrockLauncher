using BedrockLauncher.Classes;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BedrockLauncher.UpdateProcessor;
using System.Linq;
using ExtensionsDotNET;
using BL_Core.Classes;

namespace BedrockLauncher.Downloaders
{
    public class VersionDownloader
    {
        private HttpClient _client = new HttpClient();
        private Win10StoreNetwork _store_manager = new Win10StoreNetwork();

        private async Task DownloadFile(string url, string to, DownloadProgress progress, CancellationToken cancellationToken)
        {
            using (var resp = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                using (var inStream = await resp.Content.ReadAsStreamAsync())
                using (var outStream = new FileStream(to, FileMode.Create))
                {
                    long? totalSize = resp.Content.Headers.ContentLength;
                    progress(0, totalSize);
                    long transferred = 0;
                    byte[] buf = new byte[1024 * 1024];
                    while (true)
                    {
                        int n = await inStream.ReadAsync(buf, 0, buf.Length, cancellationToken);
                        if (n == 0)
                            break;
                        await outStream.WriteAsync(buf, 0, n, cancellationToken);
                        transferred += n;
                        progress(transferred, totalSize);
                    }
                }
            }
        }
        public void EnableUserAuthorization()
        {
            _store_manager.setMSAUserToken(Win10AuthenticationManager.GetWUToken(Properties.LauncherSettings.Default.CurrentInsiderAccount));
        }
        public async Task Download(string versionName, string updateIdentity, int revisionNumber, string destination, DownloadProgress progress, CancellationToken cancellationToken)
        {
            string link = await _store_manager.getDownloadLink(updateIdentity, revisionNumber, true);
            if (link == null)
                throw new ArgumentException(string.Format("Bad updateIdentity for {0}", versionName));
            System.Diagnostics.Debug.WriteLine("Resolved download link: " + link);
            await DownloadFile(link, destination, progress, cancellationToken);
        }

        public delegate void DownloadProgress(long current, long? total);

        public void PraseDB(MCVersionList list, Win10VersionDBManager.Win10VersionJsonDb db)
        {
            foreach (var v in db.list)
            {
                if (!list.ToList().Exists(x => x.UUID == v.uuid || x.Name == v.version)) list.Add(new MCVersion(v.uuid, v.version, v.isBeta));
            }
            list.Sort((x, y) => Compare(x, y));

            int Compare(MCVersion x, MCVersion y)
            {
                try
                {
                    var a = new Version(x.Name);
                    var b = new Version(y.Name);
                    return b.CompareTo(a);
                }
                catch
                {
                    return y.Name.CompareTo(x.Name);
                }

            }
        }

        public async Task UpdateVersions(MCVersionList versions)
        {
            _store_manager.setMSAUserToken(Win10AuthenticationManager.GetWUToken(Properties.LauncherSettings.Default.CurrentInsiderAccount));
            versions.Clear();

            LoadFromLocalCache();
            await DownloadGlobalList();
            LoadFromUserCache();
            await DownloadLatestList();

            Win10VersionDBManager.Win10VersionJsonDb LoadFromUserCache()
            {
                try
                {
                    Win10VersionDBManager.Win10VersionJsonDb db = new Win10VersionDBManager.Win10VersionJsonDb();
                    db.ReadJson(versions.userCacheFile);
                    PraseDB(versions, db);
                    return db;
                }
                catch (FileNotFoundException e)
                {
                    // ignore
                    System.Diagnostics.Debug.WriteLine("Version list user cache load failed:\n" + e.ToString());
                    return new Win10VersionDBManager.Win10VersionJsonDb();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Version list user cache load failed:\n" + e.ToString());
                    return new Win10VersionDBManager.Win10VersionJsonDb();
                }
            }

            Win10VersionDBManager.Win10VersionJsonDb LoadFromLocalCache()
            {
                try
                {
                    Win10VersionDBManager.Win10VersionJsonDb db = new Win10VersionDBManager.Win10VersionJsonDb();
                    db.ReadJson(versions.cacheFile);
                    PraseDB(versions, db);
                    return db;
                }
                catch (FileNotFoundException e)
                {
                    // ignore
                    System.Diagnostics.Debug.WriteLine("Version list local cache load failed:\n" + e.ToString());
                    return new Win10VersionDBManager.Win10VersionJsonDb();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Version list local cache load failed:\n" + e.ToString());
                    return new Win10VersionDBManager.Win10VersionJsonDb();
                }
            }

            async Task DownloadLatestList()
            {
                try
                {
                    var db = LoadFromUserCache();
                    var config = await _store_manager.fetchConfigLastChanged();
                    var cookie = await _store_manager.fetchCookie(config, false);
                    var knownVersions = db.list.ToList().ConvertAll(x => x.uuid);
                    db.AddVersion(await Win10StoreManager.CheckForVersions(_store_manager, cookie, knownVersions, false), false);
                    db.WriteJson(versions.userCacheFile);
                    PraseDB(versions, db);
                    config = await _store_manager.fetchConfigLastChanged();
                    cookie = await _store_manager.fetchCookie(config, true);
                    knownVersions = db.list.ToList().ConvertAll(x => x.uuid);
                    db.AddVersion(await Win10StoreManager.CheckForVersions(_store_manager, cookie, knownVersions, true), true);
                    db.WriteJson(versions.userCacheFile);
                    PraseDB(versions, db);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Version list update check failed:\n" + e.ToString());
                }
            }

            async Task DownloadGlobalList()
            {
                try
                {
                    Win10VersionDBManager.Win10VersionJsonDb db = new Win10VersionDBManager.Win10VersionJsonDb();
                    var resp = await _client.GetAsync("https://mrarm.io/r/w10-vdb");
                    resp.EnsureSuccessStatusCode();
                    var data = await resp.Content.ReadAsStringAsync();
                    db.PraseJson(data);
                    db.WriteJson(versions.cacheFile);
                    PraseDB(versions, db);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Version list download failed:\n" + e.ToString());
                }

            }
        }
    }
}