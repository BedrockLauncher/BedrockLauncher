using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BedrockLauncher.UpdateProcessor.Authentication;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Databases;
using BedrockLauncher.UpdateProcessor.Interfaces;
using Extensions;

namespace BedrockLauncher.UpdateProcessor.Handlers
{
    public class VersionManager
    {
        public delegate void DownloadProgress(long current, long? total);

        private int UserTokenIndex = 0;

        private const string communityDBUrl = "https://mrarm.io/r/w10-vdb";
        private const string communityDBTechnicalUrl = "https://raw.githubusercontent.com/MCMrARM/mc-w10-versiondb/master/versions.txt";

        private string winstoreDBFile;
        private string winstoreDBTechnicalFile;
        private string communityDBFile;
        private string communityDBTechnicalFile;

        private HttpClient HttpClient = new HttpClient();
        private StoreNetwork StoreNetwork = new StoreNetwork();
        private Dictionary<Guid, IVersionInfo> Versions = new Dictionary<Guid, IVersionInfo>();

        public List<IVersionInfo> GetVersions() => Versions.Values.ToList();

        public void Init(int _userTokenIndex, string _winstoreDBFile, string _winstoreDBTechnicalFile, string _communityDBFile, string _communityDBTechnicalFile)
        {
            UserTokenIndex = _userTokenIndex;
            winstoreDBFile = _winstoreDBFile;
            winstoreDBTechnicalFile = _winstoreDBTechnicalFile;
            communityDBFile = _communityDBFile;
            communityDBTechnicalFile = _communityDBTechnicalFile;
        }

        public async Task DownloadVersion(string versionName, string updateIdentity, int revisionNumber, string destination, DownloadProgress progress, CancellationToken cancellationToken)
        {
            string link = await StoreNetwork.getDownloadLink(updateIdentity, revisionNumber, true);
            if (link == null)
                throw new ArgumentException(string.Format("Bad updateIdentity for {0}", versionName));
            System.Diagnostics.Debug.WriteLine("Resolved download link: " + link);

            using (var resp = await HttpClient.GetAsync(link, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                using (var inStream = await resp.Content.ReadAsStreamAsync())
                using (var outStream = new FileStream(destination, FileMode.Create))
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
            var resp = await HttpClient.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var data = await resp.Content.ReadAsStringAsync();
            db.PraseRaw(data, GetVersionArches());
            db.Save(filePath);
            InsertVersionsFromDB(db);
        }
        private async Task UpdateDBFromStore(IVersionDb db, string filePath)
        {
            await UpdateDB(true);
            await UpdateDB(false);
            db.Save(filePath);
            InsertVersionsFromDB(db);

            async Task UpdateDB(bool isBeta)
            {
                var config = await StoreNetwork.fetchConfigLastChanged();
                var cookie = await StoreNetwork.fetchCookie(config, isBeta);
                var knownVersions = db.GetVersions().ConvertAll(x => x.GetUUID().ToString());
                var results = await StoreManager.CheckForVersions(StoreNetwork, cookie, knownVersions, isBeta);
                db.AddVersion(results, isBeta);
            }
        }
        private VersionJsonDb LoadJsonDBVersions(string filePath)
        {
            VersionJsonDb db = new VersionJsonDb();
            db.ReadJson(filePath, GetVersionArches());
            db.WriteJson(filePath);
            InsertVersionsFromDB(db);
            return db;
        }
        private VersionTextDb LoadTextDBVersions(string filePath)
        {
            VersionTextDb db = new VersionTextDb();
            db.ReadFile(filePath);
            InsertVersionsFromDB(db);
            return db;
        }

        private void InsertVersionsFromDB(IVersionDb db)
        {
            foreach (var version in db.GetVersions())
            {
                if (!Versions.ContainsKey(version.GetUUID()))
                    Versions.Add(version.GetUUID(), version);
            }
        }
        private async Task EnableUserAuthorization()
        {
            var token = await Task.Run(() => AuthenticationManager.Default.GetWUToken(UserTokenIndex));
            StoreNetwork.setMSAUserToken(token);
        }
        private Dictionary<Guid, string> GetVersionArches()
        {
            return Versions.ToDictionary(x => x.Key, x => x.Value.GetArchitecture());
        }
    }
}
