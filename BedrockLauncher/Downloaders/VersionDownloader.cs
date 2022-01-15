using BedrockLauncher.Classes;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BedrockLauncher.UpdateProcessor;
using System.Linq;
using ExtensionsDotNET;
using BedrockLauncher.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;

namespace BedrockLauncher.Downloaders
{
    public class VersionDownloader
    {
        private HttpClient _client = new HttpClient();
        private Win10StoreNetwork _store_manager = new Win10StoreNetwork();

        private string cacheFile => MainViewModel.Default.FilePaths.GetVersionsFilePath();
        private string userCacheFile => MainViewModel.Default.FilePaths.GetUserVersionsFilePath();
        private string technicalUserCacheFile => MainViewModel.Default.FilePaths.GetUserVersionsTechnicalFilePath();


        private string latestReleaseUUID = string.Empty;
        private string latestBetaUUID = string.Empty;
        private string GetUpdateIdentity(string uuid)
        {
            if (uuid == Constants.LATEST_BETA_UUID) return latestBetaUUID;
            else if (uuid == Constants.LATEST_RELEASE_UUID) return latestReleaseUUID;
            else return uuid;
        }

        public void EnableUserAuthorization()
        {
            try
            {
                _store_manager.setMSAUserToken(Win10AuthenticationManager.Default.GetWUToken(Properties.LauncherSettings.Default.CurrentInsiderAccountIndex));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error while Authenticating UserToken for Version Fetching:\n" + ex);
            }
        }
        public void PraseDB(ObservableCollection<BLVersion> list, Win10VersionDBManager.Win10VersionJsonDb db)
        {
            foreach (var v in db.list)
            {
                if (!list.ToList().Exists(x => x.UUID == v.uuid || x.Name == v.version)) list.Add(new BLVersion(v.uuid, v.version, v.isBeta));
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
        public async Task Download(string versionName, string uuid, int revisionNumber, string destination, DownloadProgress progress, CancellationToken cancellationToken)
        {
            var updateIdentity = GetUpdateIdentity(uuid);
            string link = await _store_manager.getDownloadLink(updateIdentity, revisionNumber, true);
            if (link == null)
                throw new ArgumentException(string.Format("Bad updateIdentity for {0}", versionName));
            System.Diagnostics.Debug.WriteLine("Resolved download link: " + link);
            await DownloadFile(link, destination, progress, cancellationToken);
        }

        public async Task UpdateVersions(ObservableCollection<BLVersion> versions, VersionUpdateOptions options = null)
        {
            if (options == null) options = VersionUpdateOptions.Default;

            await ThreadingExtensions.StartSTATask(EnableUserAuthorization);
            versions.Clear();

            LoadFromLocalCache(versions);
            if (!options.CacheOnly) await LoadFromURL(versions);
            LoadFromUserCache(versions);
            if (!options.CacheOnly) await LoadFromAPI(versions);
            if (!options.CacheOnly) await LoadFromAPI_Technical();
            LoadDefaults(versions);
        }

        public Win10VersionDBManager.Win10VersionJsonDb LoadFromUserCache(ObservableCollection<BLVersion> versions)
        {
            try
            {
                Win10VersionDBManager.Win10VersionJsonDb db = new Win10VersionDBManager.Win10VersionJsonDb();
                db.ReadJson(userCacheFile);
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
        public Win10VersionDBManager.Win10VersionJsonDb LoadFromLocalCache(ObservableCollection<BLVersion> versions)
        {
            try
            {
                Win10VersionDBManager.Win10VersionJsonDb db = new Win10VersionDBManager.Win10VersionJsonDb();
                db.ReadJson(cacheFile);
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
        public Win10VersionDBManager.Win10VersionTextDb LoadFromTechCache()
        {
            try
            {
                Win10VersionDBManager.Win10VersionTextDb db = new Win10VersionDBManager.Win10VersionTextDb();
                db.Read(technicalUserCacheFile);
                return db;
            }
            catch (FileNotFoundException e)
            {
                // ignore
                System.Diagnostics.Debug.WriteLine("Version list technical cache load failed:\n" + e.ToString());
                return new Win10VersionDBManager.Win10VersionTextDb();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Version list technical cache load failed:\n" + e.ToString());
                return new Win10VersionDBManager.Win10VersionTextDb();
            }
        }

        private void LoadDefaults(ObservableCollection<BLVersion> versions)
        {
            this.latestReleaseUUID = versions.First(x => x.IsBeta == false)?.UUID ?? string.Empty;
            this.latestBetaUUID = versions.First(x => x.IsBeta == true)?.UUID ?? string.Empty;

            var latest_beta = new BLVersion(Constants.LATEST_BETA_UUID, Application.Current.Resources["EditInstallationScreen_LatestSnapshot"].ToString(), true);
            var latest_release = new BLVersion(Constants.LATEST_RELEASE_UUID, Application.Current.Resources["EditInstallationScreen_LatestRelease"].ToString(), false);

            versions.Insert(0, latest_beta);
            versions.Insert(0, latest_release);
        }

        public async Task LoadFromAPI_Technical()
        {
            try
            {
                var db = LoadFromTechCache();
                var config = await _store_manager.fetchConfigLastChanged();
                var cookie = await _store_manager.fetchCookie(config, false);
                var knownVersions = db.releaseList.ToList().ConvertAll(x => x.uuid);
                db.AddVersion(await Win10StoreManager.CheckForVersions(_store_manager, cookie, knownVersions, false), false);
                db.Write(technicalUserCacheFile);
                config = await _store_manager.fetchConfigLastChanged();
                cookie = await _store_manager.fetchCookie(config, true);
                knownVersions = db.betaList.ToList().ConvertAll(x => x.uuid);
                db.AddVersion(await Win10StoreManager.CheckForVersions(_store_manager, cookie, knownVersions, true), true);
                db.Write(technicalUserCacheFile);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Version list update check failed:\n" + e.ToString());
            }
        }
        public async Task LoadFromAPI(ObservableCollection<BLVersion> versions)
        {
            try
            {
                var db = LoadFromUserCache(versions);
                var config = await _store_manager.fetchConfigLastChanged();
                var cookie = await _store_manager.fetchCookie(config, false);
                var knownVersions = db.list.ToList().ConvertAll(x => x.uuid);
                db.AddVersion(await Win10StoreManager.CheckForVersions(_store_manager, cookie, knownVersions, false), false);
                db.WriteJson(userCacheFile);
                PraseDB(versions, db);
                config = await _store_manager.fetchConfigLastChanged();
                cookie = await _store_manager.fetchCookie(config, true);
                knownVersions = db.list.ToList().ConvertAll(x => x.uuid);
                db.AddVersion(await Win10StoreManager.CheckForVersions(_store_manager, cookie, knownVersions, true), true);
                db.WriteJson(userCacheFile);
                PraseDB(versions, db);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Version list update check failed:\n" + e.ToString());
            }
        }
        public async Task LoadFromURL(ObservableCollection<BLVersion> versions)
        {
            try
            {
                Win10VersionDBManager.Win10VersionJsonDb db = new Win10VersionDBManager.Win10VersionJsonDb();
                var resp = await _client.GetAsync("https://mrarm.io/r/w10-vdb");
                resp.EnsureSuccessStatusCode();
                var data = await resp.Content.ReadAsStringAsync();
                db.PraseJson(data);
                db.WriteJson(cacheFile);
                PraseDB(versions, db);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Version list download failed:\n" + e.ToString());
            }

        }

        public delegate void DownloadProgress(long current, long? total);

        public class VersionUpdateOptions
        {
            public static readonly VersionUpdateOptions Default = new VersionUpdateOptions();

            public bool CacheOnly { get; set; } = false;
        }
    }
}