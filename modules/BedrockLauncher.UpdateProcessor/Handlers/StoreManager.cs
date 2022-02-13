using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BedrockLauncher.UpdateProcessor.Classes;

namespace BedrockLauncher.UpdateProcessor.Handlers
{
    public static class StoreManager
    {
        public async static Task<List<UpdateInfo>> CheckForVersions(StoreNetwork net, CookieData cookie, List<string> knownVersions, bool isBeta, bool verbose = false)
        {
            SyncResult res = new SyncResult();
            try
            {
                res = await net.syncVersion(cookie, isBeta);
            }
            catch (SOAPError e) 
            {
                System.Diagnostics.Trace.WriteLine(string.Format("SOAP ERROR: {0}", e.code));
                return new List<UpdateInfo>();
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine(string.Format("Win10 version check failed: {0}", e.ToString()));
                return new List<UpdateInfo>();
            }
            bool hasAnyNewVersions = false;
            List<UpdateInfo> newUpdates = new List<UpdateInfo>();
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
                    if (verbose) System.Diagnostics.Trace.WriteLine(string.Format("New UWP version: {0}", mergedString));
                    hasAnyNewVersions = true;
                    knownVersions.Add(mergedString);
                    newUpdates.Add(e);
                }
            }

            newUpdates = newUpdates.OrderBy(x => x.packageMoniker).ToList();

            if (!string.IsNullOrEmpty(res.newCookie.encryptedData)) cookie = res.newCookie;
            if (hasAnyNewVersions) return newUpdates;
            return new List<UpdateInfo>();
        }

    }
}
