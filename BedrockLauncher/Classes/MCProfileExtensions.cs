using BedrockLauncher.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Classes
{
    public static class MCProfileExtensions
    {
        public static void GetVersionParams(MCVersion version, out VersioningMode versioningMode, out string version_uuid)
        {
            version_uuid = Constants.LATEST_RELEASE_UUID;
            versioningMode = VersioningMode.LatestRelease;

            if (version != null)
            {
                if (version.UUID == Constants.LATEST_BETA_UUID) versioningMode = VersioningMode.LatestBeta;
                else if (version.UUID == Constants.LATEST_RELEASE_UUID) versioningMode = VersioningMode.LatestRelease;
                else versioningMode = VersioningMode.None;

                version_uuid = version.UUID;
            }
        }
        public static void SetCurrentProfile(string profileUUID)
        {
            Properties.LauncherSettings.Default.CurrentProfile = profileUUID;
            Properties.LauncherSettings.Default.Save();
        }
        public static void SetCurrentInstallation(string installationUUID)
        {
            Properties.LauncherSettings.Default.CurrentInstallation = installationUUID;
            Properties.LauncherSettings.Default.Save();
        }
    }
}
