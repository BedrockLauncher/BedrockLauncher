using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL_Core.Interfaces
{
    public interface IFilepathManager
    {

        #region Strings

        string UserDataFileName { get; }
        string SettingsFileName { get; }
        string VersionCacheFileName { get; }
        string UserVersionCacheFileName { get; }
        string AppDataFolderName { get; }
        string InstallationsFolderName { get; }
        string PackageDataFolderName { get; }
        string IconCacheFolderName { get; }
        string PrefabedIconRootPath { get; }

        #endregion

        #region Common Paths

        string CurrentLocation { get; }
        string ExecutableLocation { get; }
        string ExecutableDirectory { get; }
        string ExecutableDataDirectory { get; }
        string DefaultLocation { get; }

        #endregion

        #region Dynamic Paths

        string GetSettingsFilePath();
        string GetUserVersionsFilePath();
        string GetVersionsFilePath();
        string GetProfilesFilePath();
        string GetCacheFolderPath();
        string GetInstallationsFolderPath(string profileName, string installationDirectory);
        string GetSkinPacksFolderPath(string InstallationsPath, bool DevFolder = false, bool HasSaveRedirection = true);

        #endregion

        #region Image Cache

        string GenerateIconCacheFileName(string extension);
        bool RemoveImageFromIconCache(string filePath);
        string AddImageToIconCache(string sourceFilePath);

        #endregion
    }
}
