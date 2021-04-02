using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BedrockLauncher.Core
{
    public static class Constants
    {


        public static readonly string UserDataFileName = "user_profile.json";
        public static readonly string VersionCacheFileName = "versions.json";

        private static string AppData { get => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); }
        public static string AppDataFolder { get => Path.Combine(AppData, ".minecraft_bedrock"); }
        public static string InstallationsPath { get => Path.Combine(AppData, ".minecraft_bedrock", "installations"); }


        public static string GetVersionsFilePath()
        {
            /*if (Properties.Settings.Default.PortableInstalls)*/ return VersionCacheFileName;
            //else return Path.Combine(AppDataFolder, VersionCacheFileName);

        }
        public static string GetProfilesFilePath()
        {
            if (Properties.Settings.Default.PortableMode) return UserDataFileName;
            else return Path.Combine(AppDataFolder, UserDataFileName);
        }
        public static string GetInstallationsFolderPath(string profileName, string installationName)
        {
            var profile = ConfigManager.ProfileList.profiles[profileName];
            string installationPath = Path.Combine(profile.ProfilePath, installationName);

            if (Properties.Settings.Default.PortableMode) return Path.Combine(installationPath, "packageData");
            else return Path.Combine(InstallationsPath, installationPath, "packageData");



        }
    }
}
