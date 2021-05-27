using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;
using BedrockLauncher.UpdateProcessor;
using ExtensionsDotNET;

namespace BedrockLauncher.Classes
{
    public class MCVersionList : ObservableCollection<MCVersion>
    {

        private readonly string _cacheFile;
        private readonly string _userCacheFile;
        private readonly Interfaces.ICommonVersionCommands _commands;
        private readonly HttpClient _client = new HttpClient();
        private static Win10StoreNetwork _store_manager = new Win10StoreNetwork();


        public MCVersionList(string cacheFile, string userCacheFile, Interfaces.ICommonVersionCommands commands)
        {
            _cacheFile = cacheFile;
            _userCacheFile = userCacheFile;
            _commands = commands;
        }

        private void PraseDB(Win10VersionDBManager.Win10VersionJsonDb db)
        {
            foreach (var v in db.list)
            {
                if (!this.ToList().Exists(x => x.UUID == v.uuid || x.Name == v.version)) this.Add(new MCVersion(v.uuid, v.version, v.isBeta, _commands));
            }
            this.Sort((x, y) => Compare(x, y));

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

        public async Task UpdateVersions()
        {
            _store_manager.setMSAUserToken(Win10AuthenticationManager.GetWUToken(Properties.LauncherSettings.Default.CurrentInsiderAccount));
            Clear();

            try 
            { 
                LoadFromLocalCache(); 
            }
            catch (Exception e) 
            { 
                System.Diagnostics.Debug.WriteLine("Version list local cache load failed:\n" + e.ToString()); 
            }
            try 
            { 
                await DownloadGlobalList();
            }
            catch (Exception e) 
            { 
                System.Diagnostics.Debug.WriteLine("Version list download failed:\n" + e.ToString()); 
            }
            try 
            { 
                LoadFromUserCache();
            }
            catch (Exception e) 
            { 
                System.Diagnostics.Debug.WriteLine("Version list user cache load failed:\n" + e.ToString());
            }
            try 
            { 
                await DownloadLatestList();
            }
            catch (Exception e)
            { 
                System.Diagnostics.Debug.WriteLine("Version list update check failed:\n" + e.ToString());
            }
            Win10VersionDBManager.Win10VersionJsonDb LoadFromUserCache()
            {
                try
                {
                    Win10VersionDBManager.Win10VersionJsonDb db = new Win10VersionDBManager.Win10VersionJsonDb();
                    db.ReadJson(_userCacheFile);
                    PraseDB(db);
                    return db;
                }
                catch (FileNotFoundException)
                {
                    // ignore
                    return new Win10VersionDBManager.Win10VersionJsonDb();
                }
                catch
                {
                    return new Win10VersionDBManager.Win10VersionJsonDb();
                }
            }

            Win10VersionDBManager.Win10VersionJsonDb LoadFromLocalCache()
            {
                try
                {
                    Win10VersionDBManager.Win10VersionJsonDb db = new Win10VersionDBManager.Win10VersionJsonDb();
                    db.ReadJson(_cacheFile);
                    PraseDB(db);
                    return db;
                }
                catch (FileNotFoundException)
                {
                    // ignore
                    return new Win10VersionDBManager.Win10VersionJsonDb();
                }
                catch
                {
                    return new Win10VersionDBManager.Win10VersionJsonDb();
                }
            }

            async Task DownloadLatestList()
            {
                var db = LoadFromUserCache();
                var config = await _store_manager.fetchConfigLastChanged();
                var cookie = await _store_manager.fetchCookie(config, false);
                var knownVersions = db.list.ToList().ConvertAll(x => x.uuid);
                db.AddVersion(await Win10StoreManager.CheckForVersions(_store_manager, cookie, knownVersions, false), false);
                db.WriteJson(_userCacheFile);
                PraseDB(db);
                config = await _store_manager.fetchConfigLastChanged();
                cookie = await _store_manager.fetchCookie(config, true);
                knownVersions = db.list.ToList().ConvertAll(x => x.uuid);
                db.AddVersion(await Win10StoreManager.CheckForVersions(_store_manager, cookie, knownVersions, true), true);
                db.WriteJson(_userCacheFile);
                PraseDB(db);
            }

            async Task DownloadGlobalList()
            {
                Win10VersionDBManager.Win10VersionJsonDb db = new Win10VersionDBManager.Win10VersionJsonDb();
                var resp = await _client.GetAsync("https://mrarm.io/r/w10-vdb");
                resp.EnsureSuccessStatusCode();
                var data = await resp.Content.ReadAsStringAsync();
                db.PraseJson(data);
                db.WriteJson(_cacheFile);
                PraseDB(db);
            }
        }



    }
}
