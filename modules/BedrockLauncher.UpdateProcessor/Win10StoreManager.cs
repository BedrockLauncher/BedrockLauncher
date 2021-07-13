using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.UpdateProcessor
{
    public static class Win10StoreManager
    {
        public async static Task<List<Win10StoreNetwork.UpdateInfo>> CheckForVersions(Win10StoreNetwork net, Win10StoreNetwork.CookieData cookie, List<string> knownVersions, bool isBeta, bool verbose = false)
        {
            Win10StoreNetwork.SyncResult res = new Win10StoreNetwork.SyncResult();
            try
            {
                res = await net.syncVersion(cookie, isBeta);
            }
            catch (Win10StoreNetwork.SOAPError e) 
            {
                System.Diagnostics.Debug.WriteLine(string.Format("SOAP ERROR: {0}", e.code));
                return new List<Win10StoreNetwork.UpdateInfo>();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Win10 version check failed: {0}", e.ToString()));
                return new List<Win10StoreNetwork.UpdateInfo>();
            }
            bool hasAnyNewVersions = false;
            List<Win10StoreNetwork.UpdateInfo> newUpdates = new List<Win10StoreNetwork.UpdateInfo>();
            foreach (var e in res.newUpdates) 
            {
                if (e.packageMoniker == null) continue;
                if (e.packageMoniker.StartsWith("Microsoft.MinecraftUWP_")) 
                {
                    bool verified = false;

                    try
                    {
                        var result = await net.getDownloadLink(e.updateId, 1, isBeta);
                        if (result != null) verified = true;
                    }
                    catch
                    {
                        continue;
                    }

                    if (!verified) continue;

                    string mergedString = e.serverId + " " + e.updateId + " " + e.packageMoniker;
                    if (knownVersions.Exists(x => x == e.updateId)) continue;
                    if (verbose) System.Diagnostics.Debug.WriteLine(string.Format("New UWP version: {0}", mergedString));
                    hasAnyNewVersions = true;
                    knownVersions.Add(mergedString);
                    newUpdates.Add(e);
                }
            }

            newUpdates = newUpdates.OrderBy(x => x.packageMoniker).ToList();

            if (!string.IsNullOrEmpty(res.newCookie.encryptedData)) cookie = res.newCookie;
            if (hasAnyNewVersions) return newUpdates;
            return new List<Win10StoreNetwork.UpdateInfo>();
        }

    }
}
