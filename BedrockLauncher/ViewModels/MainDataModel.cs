using System;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using PostSharp.Patterns.Model;
using BedrockLauncher.Handlers;
using System.Windows.Threading;
using BedrockLauncher.Backend.Backporting;

namespace BedrockLauncher.ViewModels
{

    public class MainDataModel
    {
        public static MainDataModel Default { get; set; } = new MainDataModel();

        public static IBackwardsCommunication BackwardsCommunicationHost { get; private set; }
        public static void SetBackwardsCommunicationHost(IBackwardsCommunication host)
        {
            BackwardsCommunicationHost = host;
        }

        #region Properties

        public static UpdateHandler Updater { get; set; } = new UpdateHandler();
        public ProgressBarModel ProgressBarState { get; set; } = new ProgressBarModel();
        public PathHandler FilePaths { get; private set; } = new PathHandler();
        public PackageHandler PackageManager { get; set; } = new PackageHandler();
        public BLProfileList Config { get; private set; } = new BLProfileList();
        public ObservableCollection<MCVersion> Versions { get; private set; } = new ObservableCollection<MCVersion>();


        public bool AllowedToCloseWithGameOpen { get; set; } = false;
        public bool IsVersionsUpdating { get; private set; }


        #endregion

        #region Methods

        public async Task LoadVersions(bool onLoad = false)
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                if (IsVersionsUpdating) return;
                IsVersionsUpdating = true;

                try
                {
                    await PackageManager.VersionDownloader.UpdateVersionList(Versions, onLoad);
                    Config.SyncSystemMinecraftInstallations();
                }
                finally
                {
                    IsVersionsUpdating = false;
                }
            });

        }
        public void LoadConfig()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Config = BLProfileList.Load(FilePaths.GetProfilesFilePath(), Properties.LauncherSettings.Default.CurrentProfileUUID, Properties.LauncherSettings.Default.CurrentInstallationUUID);
                Config.SyncSystemMinecraftInstallations();
            });
        }
        public async void KillGame() => await PackageManager.ClosePackage();
        public async void RepairVersion(MCVersion v) => await PackageManager.DownloadPackage(v);
        public async void RemoveVersion(MCVersion v) => await PackageManager.RemovePackage(v);
        public bool IsVersionSelectedForPlay(MCVersion version)
        {
            if (version == null) return false;

            MCVersion selectedVersion = Config?.CurrentInstallation?.Version;
            if (selectedVersion == null) return false;

            return IsSameVersionEntry(selectedVersion, version);
        }

        public async Task InstallSelectAndPlayVersion(MCVersion version)
        {
            if (version == null) return;
            if (Config?.CurrentProfile == null) return;

            BLInstallation installation = Config.SelectOrCreateVersionInstallation(version);
            if (installation == null) return;

            string profileUUID = Config.CurrentProfile.UUID;
            string installPath = FilePaths.GetInstallationPackageDataPath(profileUUID, installation.DirectoryName_Full);

            if (!version.IsInstalled || !IsVersionSelectedForPlay(version))
                await PackageManager.InstallPackage(version, installPath);

            MCVersion.ClearInstallProbeCache();
            version.UpdateFolderSize();
            await LoadVersions();

            MCVersion refreshedVersion = FindMatchingVersion(version) ?? version;
            installation = Config.SelectOrCreateVersionInstallation(refreshedVersion);
            installPath = FilePaths.GetInstallationPackageDataPath(profileUUID, installation.DirectoryName_Full);

            foreach (var ver in Versions) ver.UpdateFolderSize();
            ProgressBarState.PlayButtonLanguageChanged = !ProgressBarState.PlayButtonLanguageChanged;
        }

        public async void Play(BLProfile p, BLInstallation i, bool KeepLauncherOpen, bool LaunchEditor, bool Save = true)
        {
            if (i == null || i.Version == null) return;

            i.LastPlayed = DateTime.Now;
            MainDataModel.Default.Config.Installation_UpdateLP(i);

            if (Save)
            {
                Properties.LauncherSettings.Default.CurrentInstallationUUID = i.InstallationUUID;
                Properties.LauncherSettings.Default.Save();
            }

            var Version = i.Version;
            var Path = MainDataModel.Default.FilePaths.GetInstallationPackageDataPath(p.UUID, i.DirectoryName_Full);

            bool wasInstalledBeforeClick = i.IsPlayableInstalled;
            if (!wasInstalledBeforeClick)
                await PackageManager.InstallPackage(Version, Path);

            if (!wasInstalledBeforeClick)
            {
                MCVersion.ClearInstallProbeCache();
                Version.UpdateFolderSize();
                await LoadVersions();
                ProgressBarState.PlayButtonLanguageChanged = !ProgressBarState.PlayButtonLanguageChanged;
                return;
            }

            if (i.IsPlayableInstalled) await PackageManager.LaunchPackage(Version, Path, KeepLauncherOpen, LaunchEditor);
        }

        public async void Install(BLProfile p, BLInstallation i)
        {
            if (i == null) return;

            var Version = i.Version;
            var Path = MainDataModel.Default.FilePaths.GetInstallationPackageDataPath(p.UUID, i.DirectoryName_Full);

            await PackageManager.InstallPackage(Version, Path);
        }

        private MCVersion FindMatchingVersion(MCVersion version)
        {
            foreach (MCVersion candidate in Versions)
            {
                if (IsSameVersionEntry(candidate, version))
                    return candidate;
            }

            return null;
        }

        private bool IsSameVersionEntry(MCVersion left, MCVersion right)
        {
            if (left == null || right == null) return false;
            if (!string.IsNullOrWhiteSpace(left.UUID) && left.UUID == right.UUID) return true;

            return left.Type == right.Type
                && left.Name == right.Name
                && left.Architecture == right.Architecture;
        }

        #endregion
    }
}
