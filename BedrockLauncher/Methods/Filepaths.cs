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
        public static readonly string IconCacheFolderName = "icon_cache";

        #endregion

        #region Common Paths

        public static string CurrentLocation { get => (Properties.Settings.Default.PortableMode ? PortableLocation : GetFixedPath()); }
        public static string PortableLocation { get => System.Reflection.Assembly.GetExecutingAssembly().Location; }
        public static string DefaultLocation { get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolderName); }

        #endregion

        #region Dynamic Paths

        private static string GetFixedPath()
        {
            if (Properties.Settings.Default.FixedDirectory == string.Empty)
            {
                return DefaultLocation;
            }
            else return Properties.Settings.Default.FixedDirectory;
        }
        public static string GetVersionsFilePath()
        {
            return VersionCacheFileName;

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
            var profile = ConfigManager.ProfileList.profiles[profileName];
            string InstallationsPath = Path.Combine(profile.ProfilePath, installationDirectory);
            return Path.Combine(CurrentLocation, InstallationsFolderName, InstallationsPath, PackageDataFolderName);
        }
        public static string GetSkinPacksFolderPath(string InstallationsPath, bool DevFolder = false)
        {
            string[] Route = new string[] { "LocalState", "games", "com.mojang", (DevFolder ? "development_skin_packs" : "skin_packs") };
            string PackageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", GameManager.MINECRAFT_PACKAGE_FAMILY);


            if (Properties.Settings.Default.SaveRedirection) return Path.Combine(Route.Prepend(InstallationsPath).ToArray());
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
                Console.WriteLine(ex);
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
                Console.WriteLine(ex);
                return string.Empty;
            }
        }

        #endregion
    }
}
