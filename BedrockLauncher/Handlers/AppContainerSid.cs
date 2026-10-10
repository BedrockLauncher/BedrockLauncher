using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace BedrockLauncher.Handlers
{
    /// <summary>
    /// The security identifiers a packaged (AppContainer) app is granted access with. Windows grants them on the
    /// package's app data folder when it creates it; folders the launcher creates or links need them added, or the game
    /// cannot open them (Minecraft then shows "Storage is full" and no worlds).
    /// </summary>
    internal static class AppContainerSid
    {
        [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
        private static extern int DeriveAppContainerSidFromAppContainerName(string appContainerName, out IntPtr sid);

        [DllImport("advapi32.dll")]
        private static extern IntPtr FreeSid(IntPtr sid);

        /// <summary>
        /// The package's AppContainer SID (S-1-15-2-…) and the matching S-1-15-3-… SID Windows puts on the package's own
        /// app data folders. Empty when the SID cannot be derived.
        /// </summary>
        public static IReadOnlyList<SecurityIdentifier> ForPackageFamily(string packageFamilyName)
        {
            if (DeriveAppContainerSidFromAppContainerName(packageFamilyName, out IntPtr pointer) != 0 || pointer == IntPtr.Zero)
                return Array.Empty<SecurityIdentifier>();

            try
            {
                var appContainer = new SecurityIdentifier(pointer);
                var packageData = new SecurityIdentifier("S-1-15-3-" + appContainer.Value.Substring("S-1-15-2-".Length));
                return new[] { appContainer, packageData };
            }
            finally
            {
                FreeSid(pointer);
            }
        }
    }
}
