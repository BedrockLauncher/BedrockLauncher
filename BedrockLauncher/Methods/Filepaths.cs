using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using BedrockLauncher.Core;

namespace BedrockLauncher.Methods
{
    public static class Filepaths
    {

        #region Strings

        public static readonly string UserDataFileName = "user_profile.json";
        public static readonly string VersionCacheFileName = "versions.json";
        public static readonly string AppDataFolderName = ".minecraft_bedrock";
        public static readonly string InstallationsFolderName = "installations";
        public static readonly string PackageDataFolderName = "packageData";

        #endregion

        #region Common Paths

        public static string PortableLocation { get => System.Reflection.Assembly.GetExecutingAssembly().Location; }
        public static string FixedDataFolder { get => Path.Combine(AppData, AppDataFolderName); }
        public static string InstallationsPath_Fixed { get => Path.Combine(AppData, AppDataFolderName, InstallationsFolderName); }
        private static string AppData { get => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); }

        #endregion

        #region Dynamic Paths

        public static string GetVersionsFilePath()
        {
            return VersionCacheFileName;

        }
        public static string GetProfilesFilePath()
        {
            if (Properties.Settings.Default.PortableMode) return Methods.Filepaths.PortableLocation + "\\" + UserDataFileName;
            else return Path.Combine(FixedDataFolder, UserDataFileName);
        }
        public static string GetInstallationsFolderPath(string profileName, string installationDirectory)
        {
            var profile = ConfigManager.ProfileList.profiles[profileName];
            string InstallationsPath_Portable = Path.Combine(Methods.Filepaths.PortableLocation, profile.ProfilePath, installationDirectory);

            if (Properties.Settings.Default.PortableMode) return Path.Combine(InstallationsPath_Portable, PackageDataFolderName);
            else return Path.Combine(InstallationsPath_Fixed, InstallationsPath_Portable, PackageDataFolderName);
        }
        public static string GetSkinPacksFolderPath(string InstallationsPath, bool DevFolder = false)
        {
            string[] Route = new string[] { "LocalState", "games", "com.mojang", (DevFolder ? "development_skin_packs" : "skin_packs") };
            string PackageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", GameManager.MINECRAFT_PACKAGE_FAMILY);


            if (Properties.Settings.Default.SaveRedirection) return Path.Combine(Route.Prepend(InstallationsPath).ToArray());
            else return Path.Combine(Route.Prepend(PackageFolder).ToArray());
        }



        #endregion
    }
}
