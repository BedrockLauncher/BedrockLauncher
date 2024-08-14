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

    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]    //119 Lines
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

                await PackageManager.VersionDownloader.UpdateVersionList(Versions, onLoad);

                IsVersionsUpdating = false;
            });

        }
        public void LoadConfig()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Config = BLProfileList.Load(FilePaths.GetProfilesFilePath(), Properties.LauncherSettings.Default.CurrentProfileUUID, Properties.LauncherSettings.Default.CurrentProfileUUID);
            });
        }
        public async void KillGame() => await PackageManager.ClosePackage();
        public async void RepairVersion(MCVersion v) => await PackageManager.DownloadPackage(v);
        public async void RemoveVersion(MCVersion v) => await PackageManager.RemovePackage(v);
        public async void Play(BLProfile p, BLInstallation i, bool KeepLauncherOpen, bool Save = true)
        {
            if (i == null) return;

            i.LastPlayed = DateTime.Now;
            MainDataModel.Default.Config.Installation_UpdateLP(i);

            if (Save)
            {
                Properties.LauncherSettings.Default.CurrentInstallationUUID = i.InstallationUUID;
                Properties.LauncherSettings.Default.Save();
            }

            var Version = i.Version;
            var Path = MainDataModel.Default.FilePaths.GetInstallationPackageDataPath(p.UUID, i.DirectoryName_Full);

            await PackageManager.InstallPackage(Version, Path);
            await PackageManager.LaunchPackage(Version, Path, KeepLauncherOpen);
        }

        public async void Install(BLProfile p, BLInstallation i)
        {
            if (i == null) return;

            var Version = i.Version;
            var Path = MainDataModel.Default.FilePaths.GetInstallationPackageDataPath(p.UUID, i.DirectoryName_Full);

            await PackageManager.InstallPackage(Version, Path);
        }

        #endregion
    }
}
