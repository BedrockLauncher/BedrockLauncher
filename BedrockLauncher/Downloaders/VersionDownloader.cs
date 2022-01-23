using BedrockLauncher.Classes;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BedrockLauncher.UpdateProcessor;
using BedrockLauncher.UpdateProcessor.Databases;
using BedrockLauncher.UpdateProcessor.Classes;
using System.Linq;
using Extensions;
using BedrockLauncher.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using static BedrockLauncher.UpdateProcessor.Handlers.VersionManager;
using BedrockLauncher.UpdateProcessor.Handlers;
using System.Text.RegularExpressions;

namespace BedrockLauncher.Downloaders
{
    public class VersionDownloader
    {
        private VersionManager VersionDB = new VersionManager();

        private string winstoreDBFile => MainViewModel.Default.FilePaths.GetWinStoreVersionsDBFile();
        private string winstoreDBTechnicalFile => MainViewModel.Default.FilePaths.GetWinStoreVersionsTechnicalDBFile();
        private string communityDBFile => MainViewModel.Default.FilePaths.GetCommunityVersionsDBFile();
        private string communityDBTechnicalFile => MainViewModel.Default.FilePaths.GetCommunityVersionsTechnicalDBFile();

        private string latestReleaseUUID { get; set; }
        private string latestBetaUUID { get; set; }


        public async Task DownloadVersion(string versionName, string uuid, int revisionNumber, string destination, DownloadProgress progress, CancellationToken cancellationToken)
        {
            await VersionDB.DownloadVersion(versionName, GetUpdateIdentity(uuid), revisionNumber, destination, progress, cancellationToken);

            string GetUpdateIdentity(string uuid)
            {
                if (uuid == Constants.LATEST_BETA_UUID) return latestReleaseUUID;
                else if (uuid == Constants.LATEST_RELEASE_UUID) return latestBetaUUID;
                else return uuid;
            }
        }
        public async Task UpdateVersions(ObservableCollection<BLVersion> versions, bool OnLoad = false)
        {
            bool AllowUpdating = OnLoad && Debugger.IsAttached ? Constants.Debugging.RetriveNewVersionsOnLoad : true;

            //Clear Existing Versions
            versions.Clear();

            //Retrive Versions
            int userIndex = Properties.LauncherSettings.Default.CurrentInsiderAccountIndex;
            VersionDB.Init(userIndex, winstoreDBFile, winstoreDBTechnicalFile, communityDBFile, communityDBTechnicalFile);
            await VersionDB.LoadVersions(AllowUpdating);

            //Add Versions to ObservableCollection, then Sort them
            foreach (var entry in VersionDB.GetVersions())
            {
                versions.Add(new BLVersion(entry.GetUUID().ToString(), GetRealVersion(entry.GetVersion()), entry.GetIsBeta(), entry.GetArchitecture()));
            }
                
            versions.Sort((x, y) => x.Compare(y));


            //Get Latest Release and Beta Versions an Insert them into the ObservableCollection
            this.latestReleaseUUID = versions.First(x => x.IsBeta == false && Constants.CurrentArchitecture == x.Architecture).UUID;
            this.latestBetaUUID = versions.First(x => x.IsBeta == true && Constants.CurrentArchitecture == x.Architecture).UUID;

            var latest_beta = new BLVersion(Constants.LATEST_BETA_UUID, Application.Current.Resources["EditInstallationScreen_LatestSnapshot"].ToString(), true, Constants.CurrentArchitecture);
            var latest_release = new BLVersion(Constants.LATEST_RELEASE_UUID, Application.Current.Resources["EditInstallationScreen_LatestRelease"].ToString(), false, Constants.CurrentArchitecture);

            versions.Insert(0, latest_beta);
            versions.Insert(0, latest_release);

            string GetRealVersion(string versionS)
            {
                if (MinecraftVersion.TryParse(versionS, out MinecraftVersion version)) return version.ToRealString();
                else return new Version(0, 0, 0, 0).ToString();
            }
        }


    }
}