using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BedrockLauncher.UpdateProcessor.Authentication;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Databases;
using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.UpdateProcessor.Interfaces;
using JemExtensions;

namespace BedrockLauncher.UpdateProcessor.Handlers
{
    public class VersionManager
    {
        public delegate void DownloadProgress(long current, long total);

        private int UserTokenIndex = 0;

        private const string communityDBUrl = "https://mrarm.io/r/w10-vdb";
        private const string communityDBTechnicalUrl = "https://raw.githubusercontent.com/MCMrARM/mc-w10-versiondb/master/versions.txt";

        private string winstoreDBFile;
        private string winstoreDBTechnicalFile;
        private string communityDBFile;
        private string communityDBTechnicalFile;

        private HttpClient HttpClient = new HttpClient();
        private StoreNetwork StoreNetwork = new StoreNetwork();
        private List<IVersionInfo> Versions = new List<IVersionInfo>();

        public List<IVersionInfo> GetVersions() => Versions.ToList();
        public void Init(int _userTokenIndex, string _winstoreDBFile, string _winstoreDBTechnicalFile, string _communityDBFile, string _communityDBTechnicalFile)
        {
            UserTokenIndex = _userTokenIndex;
            winstoreDBFile = _winstoreDBFile;
            winstoreDBTechnicalFile = _winstoreDBTechnicalFile;
            communityDBFile = _communityDBFile;
            communityDBTechnicalFile = _communityDBTechnicalFile;
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
        public async Task LoadVersions(bool getNewVersions)
        {
            Versions.Clear();

            await EnableUserAuthorization();

            var communityDBT = LoadTextDBVersions(communityDBTechnicalFile);
            var communityDB = LoadJsonDBVersions(communityDBFile);

            if (getNewVersions)
            {
                await UpdateDBFromURL(communityDBT, communityDBTechnicalFile, communityDBTechnicalUrl);
                await UpdateDBFromURL(communityDB, communityDBFile, communityDBUrl);
            }

            var winStoreDBT = LoadTextDBVersions(winstoreDBTechnicalFile);
            var winStoreDB = LoadJsonDBVersions(winstoreDBFile);

            if (getNewVersions)
            {
                await UpdateDBFromStore(winStoreDBT, winstoreDBTechnicalFile);
                await UpdateDBFromStore(winStoreDB, winstoreDBFile);
            }
        }

        private async Task UpdateDBFromURL(IVersionDb db, string filePath, string url)
        {
            try
            {
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
        private async Task UpdateDBFromStore(IVersionDb db, string filePath)
        {
            try
            {
                await UpdateDB(VersionType.Beta);
                await UpdateDB(VersionType.Release);
                await UpdateDB(VersionType.Preview);
                db.Save(filePath);
                InsertVersionsFromDB(db);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("UpdateDBFromStore Failed!");
                Trace.WriteLine("File: " + filePath);
                Trace.WriteLine(ex);
            }


            async Task UpdateDB(VersionType type)
            {
                try
                {
                    var config = await StoreNetwork.fetchConfigLastChanged();
                    var cookie = await StoreNetwork.fetchCookie(config, type);
                    var knownVersions = db.GetVersions().ConvertAll(x => x.GetUUID().ToString());
                    var results = await StoreManager.CheckForVersions(StoreNetwork, cookie, knownVersions, type);
                    db.AddVersion(results, type);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("UpdateDBFromStore.UpdateDB Failed!");
                    Trace.WriteLine("File: " + filePath);
                    Trace.WriteLine("isBeta: " + type);
                    Trace.WriteLine(ex);
                }

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
                return new VersionJsonDb();
            }

        }
        private VersionTextDb LoadTextDBVersions(string filePath)
        {
            try
            {
                VersionTextDb db = new VersionTextDb();
                db.ReadFile(filePath);
                InsertVersionsFromDB(db);
                return db;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("LoadTextDBVersions Failed! Generating Blank VersionTextDb");
                Trace.WriteLine("File: " + filePath);
                Trace.WriteLine(ex);
                return new VersionTextDb();
            }

        }

        private void InsertVersionsFromDB(IVersionDb db)
        {
            foreach (var version in db.GetVersions())
            {
                if (!MinecraftVersion.TryParse(version.GetVersion(), out MinecraftVersion ver)) continue;
                if (Versions.Exists(x => x.GetUUID() == version.GetUUID())) continue;
                if (Versions.Exists(x => x.GetVersion() == version.GetVersion() && x.GetArchitecture() == version.GetArchitecture())) continue;
                if (ver.Revision == 0) Versions.Add(version);
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
