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
        }

        #endregion

        public delegate void DownloadProgress(long current, long total);

        private int UserTokenIndex = 0;

        private const string communityDBUrl = "https://www.raythnetwork.co.uk/versions.php?type=json";

        private string winstoreDBFile;
        private string communityDBFile;

        private HttpClient HttpClient = new HttpClient();
        private StoreNetwork StoreNetwork = new StoreNetwork();
        private List<VersionInfoJson> Versions = new List<VersionInfoJson>();

        public List<VersionInfoJson> GetVersions() => Versions.ToList();
        public void Init(int _userTokenIndex, string _winstoreDBFile, string _communityDBFile)
        {
            UserTokenIndex = _userTokenIndex;
            winstoreDBFile = _winstoreDBFile;
            communityDBFile = _communityDBFile;
        }

        public async Task DownloadVersion(string versionName, string updateIdentity, int revisionNumber, string destination, DownloadProgress progress, CancellationToken cancellationToken, VersionType type)
        {
            string link = await StoreNetwork.getDownloadLink(updateIdentity, revisionNumber, type);
            if (link == null)
                throw new ArgumentException(string.Format("Bad updateIdentity for {0}", versionName));
            Trace.WriteLine("Resolved download link: " + link);

            using (var resp = await HttpClient.GetAsync(link, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                using (var inStream = await resp.Content.ReadAsStreamAsync())
                {
                    using (var outStream = new FileStream(destination, FileMode.Create))
                    {
                        long totalSize = resp.Content.Headers.ContentLength.Value;
                        progress(0, totalSize);
                        long transferred = 0;
                        byte[] buf = new byte[1024 * 1024];

                        Task task = null;
                        CancellationTokenSource ts = new CancellationTokenSource();

                        while (true)
                        {
                            int n = await inStream.ReadAsync(buf, 0, buf.Length, cancellationToken);
                            if (n == 0)
                                break;
                            await outStream.WriteAsync(buf, 0, n, cancellationToken);
                            transferred += n;
                            UpdateProgress(ref task, ref ts, transferred, totalSize);

                        }
                    }
                }
            }

            void UpdateProgress(ref Task task, ref CancellationTokenSource ts, long transferred, long totalSize)
            {
                if (task != null)
                {
                    if (!task.IsCompleted) ts.Cancel();
                    task = null;
                    ts = new CancellationTokenSource();
                }
                if (task == null)
                {
                    task = new Task(() => progress(transferred, totalSize), ts.Token);
                }


                task.Start();
            }
        }
        public async Task LoadVersions(bool getNewVersions, bool checkMicrosoftStore)
        {
            Versions.Clear();

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

        private VersionJsonDb LoadJsonDBVersions(string filePath)
        {
            try
            {
                VersionJsonDb db = new VersionJsonDb();
                db.ReadJson(filePath, GetVersionArches());
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
