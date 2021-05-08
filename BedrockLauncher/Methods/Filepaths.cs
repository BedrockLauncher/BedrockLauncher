using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using BedrockLauncher.Methods;

namespace BedrockLauncher.Methods
{
    public static class Filepaths
    {

        #region Strings

        public static readonly string UserDataFileName = "user_profile.json";
        public static readonly string SettingsFileName = "settings.json";
        public static readonly string VersionCacheFileName = "versions.json";
        public static readonly string AppDataFolderName = ".minecraft_bedrock";
        public static readonly string InstallationsFolderName = "installations";
        public static readonly string PackageDataFolderName = "packageData";
        public static readonly string IconCacheFolderName = "icon_cache";
        public static readonly string PrefabedIconRootPath = @"/BedrockLauncher;component/resources/images/installation_icons/";

        #endregion

        #region Common Paths

        public static string CurrentLocation { get => (Properties.LauncherSettings.Default.PortableMode ? ExecutableDataDirectory : GetFixedPath()); }
        public static string ExecutableLocation { get => System.Reflection.Assembly.GetExecutingAssembly().Location; }
        public static string ExecutableDirectory { get => Path.GetDirectoryName(ExecutableLocation); }
        public static string ExecutableDataDirectory 
        { 
            get 
            {
                string path = Path.Combine(Path.GetDirectoryName(ExecutableLocation), "data");
                Directory.CreateDirectory(path);
                return path;
            }
        }
        public static string DefaultLocation { get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolderName); }

        #endregion

        #region Dynamic Paths

        private static string GetFixedPath()
        {
            string FixedDirectory = string.Empty;
            if (Properties.LauncherSettings.Default.FixedDirectory == string.Empty)
            {
                FixedDirectory = DefaultLocation;
            }
            else FixedDirectory = Properties.LauncherSettings.Default.FixedDirectory;

            if (!Directory.Exists(FixedDirectory)) Directory.CreateDirectory(FixedDirectory);
            return FixedDirectory;
        }

        public static string GetSettingsFilePath()
        {
            return Path.Combine(ExecutableDataDirectory, SettingsFileName);
        }

        public static string GetVersionsFilePath()
        {
            return Path.Combine(CurrentLocation, VersionCacheFileName);
        }
        public static string GetProfilesFilePath()
        {
            return Path.Combine(CurrentLocation, UserDataFileName);
        }
        public static string GetCacheFolderPath()
        {
            string cache_dir = Path.Combine(CurrentLocation, IconCacheFolderName);
            if (!Directory.Exists(cache_dir)) Directory.CreateDirectory(cache_dir);
            return cache_dir;
        }
        public static string GetInstallationsFolderPath(string profileName, string installationDirectory)
        {
            if (!ConfigManager.ProfileList.profiles.ContainsKey(profileName)) return string.Empty;
            var profile = ConfigManager.ProfileList.profiles[profileName];
            string InstallationsPath = Path.Combine(profile.ProfilePath, installationDirectory);
            return Path.Combine(CurrentLocation, InstallationsFolderName, InstallationsPath, PackageDataFolderName);
        }
        public static string GetSkinPacksFolderPath(string InstallationsPath, bool DevFolder = false)
        {
            if (InstallationsPath == string.Empty) return string.Empty;
            string[] Route = new string[] { "LocalState", "games", "com.mojang", (DevFolder ? "development_skin_packs" : "skin_packs") };
            string PackageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", GameManager.MINECRAFT_PACKAGE_FAMILY);


            if (Properties.LauncherSettings.Default.SaveRedirection) return Path.Combine(Route.Prepend(InstallationsPath).ToArray());
            else return Path.Combine(Route.Prepend(PackageFolder).ToArray());
        }



        #endregion

        #region Image Cache


        public static string GenerateIconCacheFileName(string extension)
        {
            string cache_dir = GetCacheFolderPath();
            string destFileName = string.Empty;

            while (destFileName == string.Empty || File.Exists(destFileName))
            {
                string cache_filename = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + extension;
                destFileName = Path.Combine(cache_dir, cache_filename);
            }

            return destFileName;
        }

        public static bool RemoveImageFromIconCache(string filePath)
        {
            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Program.LogConsoleLine(ex);
                return false;
            }
        }

        public static string AddImageToIconCache(string sourceFilePath)
        {
            string destFileName = GenerateIconCacheFileName(Path.GetExtension(sourceFilePath));

            try
            {
                File.Copy(sourceFilePath, destFileName);
                return destFileName;
            }
            catch (Exception ex)
            {
                Program.LogConsoleLine(ex);
                return string.Empty;
            }
        }

        #endregion
    }
}
