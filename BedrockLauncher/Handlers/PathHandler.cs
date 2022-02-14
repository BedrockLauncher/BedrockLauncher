using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using BedrockLauncher.Classes;
using BedrockLauncher.ViewModels;
using BedrockLauncher.UpdateProcessor.Enums;

namespace BedrockLauncher.Handlers
{
    public class PathHandler
    {
        #region Strings

        public string UserDataFileName { get => "user_profile.json"; }
        public string SettingsFileName { get => "settings.json"; }
        public string WinStoreVersionsDBFileName { get => "winstore_versions.json"; }
        public string WinStoreVersionsTechnicalDBFileName { get => "winstore_technical_versions.txt"; }
        public string CommunityVersionsDBFileName { get => "community_versions.json"; }
        public string CommunityVersionsTechnicalDBFileName { get => "community_technical_versions.txt"; }
        public string AppDataFolderName { get => ".minecraft_bedrock"; }

        public string InstallationsFolderName { get => "installations"; }
        public string PackageDataFolderName { get => "packageData"; }
        public string IconCacheFolderName { get => "icon_cache"; }
        public string PrefabedIconRootPath { get => @"/BedrockLauncher;component/resources/images/installation_icons/"; }

        #endregion

        #region Common Paths

        public string CurrentLocation { get => (Properties.LauncherSettings.Default.PortableMode ? ExecutableDataDirectory : GetFixedPath()); }
        public string ExecutableLocation { get => System.Reflection.Assembly.GetExecutingAssembly().Location; }
        public string ExecutableDirectory { get => Path.GetDirectoryName(ExecutableLocation); }
        public string ExecutableDataDirectory 
        { 
            get 
            {
                string path = Path.Combine(ExecutableDirectory, "data");
                Directory.CreateDirectory(path);
                return path;
            }
        }
        public string DefaultLocation { get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolderName); }

        #endregion

        #region Dynamic Paths

        private string GetFixedPath()
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

        public string GetSettingsFilePath()
        {
            return Path.Combine(ExecutableDataDirectory, SettingsFileName);
        }
        public string GetCommunityVersionsDBFile()
        {
            return Path.Combine(CurrentLocation, CommunityVersionsDBFileName);
        }
        public string GetCommunityVersionsTechnicalDBFile()
        {
            return Path.Combine(CurrentLocation, CommunityVersionsTechnicalDBFileName);
        }
        public string GetWinStoreVersionsTechnicalDBFile()
        {
            return Path.Combine(CurrentLocation, WinStoreVersionsTechnicalDBFileName);
        }
        public string GetWinStoreVersionsDBFile()
        {
            return Path.Combine(CurrentLocation, WinStoreVersionsDBFileName);
        }
        public string GetProfilesFilePath()
        {
            return Path.Combine(CurrentLocation, UserDataFileName);
        }
        public string GetCacheFolderPath()
        {
            string cache_dir = Path.Combine(CurrentLocation, IconCacheFolderName);
            if (!Directory.Exists(cache_dir)) Directory.CreateDirectory(cache_dir);
            return cache_dir;
        }
        public string GetInstallationsFolderPath(string profileName, string installationDirectory)
        {
            if (!MainViewModel.Default.Config.profiles.ContainsKey(profileName)) return string.Empty;
            var profile = MainViewModel.Default.Config.profiles[profileName];
            string InstallationsPath = Path.Combine(profile.ProfilePath, installationDirectory);
            return Path.Combine(CurrentLocation, InstallationsFolderName, InstallationsPath, PackageDataFolderName);
        }
        public string GetSkinPacksFolderPath(string InstallationsPath, VersionType type, bool DevFolder = false, bool HasSaveRedirection = true)
        {
            if (InstallationsPath == string.Empty) return string.Empty;
            string[] Route = new string[] { (DevFolder ? "development_skin_packs" : "skin_packs") };
            string PackageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", Constants.GetPackageFamily(type), "LocalState", "games", "com.mojang");


            if (HasSaveRedirection) return Path.Combine(Route.Prepend(InstallationsPath).ToArray());
            else return Path.Combine(Route.Prepend(PackageFolder).ToArray());
        }



        #endregion

        #region Image Cache


        public string GenerateIconCacheFileName(string extension)
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

        public bool RemoveImageFromIconCache(string filePath)
        {
            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                return false;
            }
        }

        public string AddImageToIconCache(string sourceFilePath)
        {
            string destFileName = GenerateIconCacheFileName(Path.GetExtension(sourceFilePath));

            try
            {
                File.Copy(sourceFilePath, destFileName);
                return destFileName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                return string.Empty;
            }
        }

        #endregion
    }
}
