using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BedrockLauncher.UpdateProcessor.Handlers
{
    public static class StoreManager
    {   
        /// <summary>
        /// Checks whether there are new versions on the Store network
        /// </summary>
        /// <param name="net">Store network</param>
        /// <param name="versionType">Type of release to check</param>
        /// <param name="knownVersions">List of currently known text versions</param>
        /// <returns>The list of new versions</returns>
        public async static Task<List<UpdateInfo>> CheckForGDKVersions(StoreNetwork net, VersionType versionType,
            CookieData cookie, List<string> knownVersions)
        {
            SyncResult syncResult;
            try
            {
                syncResult = await net.syncVersion(cookie, versionType);
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
            foreach (UpdateInfo updateInfo in syncResult.newUpdates)
            {
                if (updateInfo.packageMoniker == null) continue;
                if (updateInfo.packageMoniker.StartsWith("Microsoft.MinecraftUWP_") || updateInfo.packageMoniker.StartsWith("Microsoft.MinecraftWindowsBeta_"))
                {
                    bool verified = false;

                    try
                    {
                        var result = await net.getDownloadLink(updateInfo.updateId, 1, versionType);
                        if (result != null) verified = true;
                    }
                    catch
                    {
                        continue;
                    }

                    if (!verified) continue;

                    string mergedString = updateInfo.serverId + " " + updateInfo.updateId + " " + updateInfo.packageMoniker;

                    if (knownVersions.Exists(x => x == updateInfo.updateId)) continue;

                    System.Diagnostics.Trace.WriteLine(string.Format("New UWP version: {0}", mergedString));
                    hasAnyNewVersions = true;
                    knownVersions.Add(mergedString);
                    newUpdates.Add(updateInfo);
                }
            }

            newUpdates = newUpdates.OrderBy(x => x.packageMoniker).ToList();

            if (!string.IsNullOrEmpty(syncResult.newCookie.encryptedData)) cookie = syncResult.newCookie;
            if (hasAnyNewVersions) return newUpdates;
            return new List<UpdateInfo>();
        }
    }
}
