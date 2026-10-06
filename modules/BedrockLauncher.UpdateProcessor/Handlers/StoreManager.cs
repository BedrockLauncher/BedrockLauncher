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
        /// Checks whether there are new UWP versions on the Windows Update Store network.
        /// </summary>
        /// <param name="net">Store network instance.</param>
        /// <param name="versionType">Type of release to check (Release / Beta / Preview).</param>
        /// <param name="cookie">
        /// Session cookie for the WU request. The cookie may be refreshed server-side;
        /// the updated value is stored back via <paramref name="cookie"/> (ref parameter).
        /// </param>
        /// <param name="knownVersions">
        /// List of already-known update IDs (GUIDs as strings). New ones are appended in-place.
        /// </param>
        /// <returns>The list of newly-discovered <see cref="UpdateInfo"/> entries.</returns>
        public static async Task<List<UpdateInfo>> CheckForUWPVersions(
            StoreNetwork net,
            VersionType versionType,
            CookieData cookie,
            List<string> knownVersions)
        {
            SyncResult syncResult;
            try
            {
                syncResult = await net.syncVersion(cookie, versionType);
            }
            catch (SOAPError e)
            {
                System.Diagnostics.Trace.WriteLine($"SOAP ERROR: {e.code}");
                return new List<UpdateInfo>();
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine($"WU version check failed: {e}");
                return new List<UpdateInfo>();
            }

            var newUpdates = new List<UpdateInfo>();

            foreach (UpdateInfo updateInfo in syncResult.newUpdates)
            {
                if (updateInfo.packageMoniker == null) continue;

                bool isUwpPackage =
                    updateInfo.packageMoniker.StartsWith("Microsoft.MinecraftUWP_",           StringComparison.Ordinal) ||
                    updateInfo.packageMoniker.StartsWith("Microsoft.MinecraftWindowsBeta_",   StringComparison.Ordinal);

                if (!isUwpPackage) continue;

                // Skip already-known versions.
                if (knownVersions.Contains(updateInfo.updateId)) continue;

                // Verify that a download link is actually available before adding.
                bool verified = false;
                try
                {
                    var result = await net.getDownloadLink(updateInfo.updateId, 1, versionType);
                    verified = result != null;
                }
                catch
                {
                    continue;
                }

                if (!verified) continue;

                string logEntry = $"{updateInfo.serverId} {updateInfo.updateId} {updateInfo.packageMoniker}";
                System.Diagnostics.Trace.WriteLine($"New UWP version discovered: {logEntry}");

                knownVersions.Add(updateInfo.updateId);
                newUpdates.Add(updateInfo);
            }

            newUpdates = newUpdates.OrderBy(x => x.packageMoniker, StringComparer.Ordinal).ToList();

            // Propagate the refreshed cookie if the server returned one.
            if (!string.IsNullOrEmpty(syncResult.newCookie.encryptedData))
                cookie = syncResult.newCookie;

            return newUpdates;
        }

        /// <summary>
        /// Backward-compatible alias for <see cref="CheckForUWPVersions"/>.
        /// The original name was misleading (it checked UWP packages, not GDK ones).
        /// </summary>
        [Obsolete("Use CheckForUWPVersions — this overload had a misleading name.")]
        public static Task<List<UpdateInfo>> CheckForGDKVersions(
            StoreNetwork net, VersionType versionType, CookieData cookie, List<string> knownVersions)
            => CheckForUWPVersions(net, versionType, cookie, knownVersions);
    }
}
