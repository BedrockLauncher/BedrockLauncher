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
using JemExtensions;
using BedrockLauncher.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using static BedrockLauncher.UpdateProcessor.Handlers.VersionManager;
using BedrockLauncher.UpdateProcessor.Handlers;
using System.Text.RegularExpressions;
using BedrockLauncher.UpdateProcessor.Extensions;
using BedrockLauncher.Enums;
using BedrockLauncher.UpdateProcessor.Enums;
using System.Xml.Linq;

namespace BedrockLauncher.Downloaders
{
    public class VersionDownloader
    {
        private VersionManager VersionDB = new VersionManager();

        private string winstoreDBFile => MainDataModel.Default.FilePaths.GetWinStoreVersionsDBFile();
        private string winstoreDBTechnicalFile => MainDataModel.Default.FilePaths.GetWinStoreVersionsTechnicalDBFile();
        private string communityDBFile => MainDataModel.Default.FilePaths.GetCommunityVersionsDBFile();
        private string communityDBTechnicalFile => MainDataModel.Default.FilePaths.GetCommunityVersionsTechnicalDBFile();

        private MCVersion latestReleaseRef { get; set; }
        private MCVersion latestBetaRef { get; set; }
        private MCVersion latestPreviewRef { get; set; }


        public async Task DownloadVersion(string versionName, string packageID, int revisionNumber, string destination, DownloadProgress progress, CancellationToken cancellationToken, VersionType versionType)
        {
            await VersionDB.DownloadVersion(versionName, GetUpdateIdentity(packageID), revisionNumber, destination, progress, cancellationToken, versionType);

            string GetUpdateIdentity(string packageID)
            {
                if (packageID == Constants.LATEST_BETA_UUID) return latestBetaRef.PackageID;
                else if (packageID == Constants.LATEST_RELEASE_UUID) return latestReleaseRef.PackageID;
                else if (packageID == Constants.LATEST_PREVIEW_UUID) return latestPreviewRef.PackageID;
                else return packageID;
            }
        }
        public async Task UpdateVersionList(ObservableCollection<MCVersion> versions, bool OnLoad = false)
        {
            bool AllowUpdating = OnLoad && Debugger.IsAttached ? Constants.Debugging.RetriveNewVersionsOnLoad : true;

            //Clear Existing Versions
            versions.Clear();

            //Retrive Versions
            int userIndex = Properties.LauncherSettings.Default.CurrentInsiderAccountIndex;
            VersionDB.Init(userIndex, winstoreDBFile, winstoreDBTechnicalFile, communityDBFile, communityDBTechnicalFile);
            await VersionDB.LoadVersions(true, Properties.LauncherSettings.Default.FetchVersionsFromMicrosoftStore);

            //Add Versions to ObservableCollection, then Sort them
            foreach (var entry in VersionDB.GetVersions())
            {
                versions.Add(new MCVersion(entry.GetUUID().ToString(), entry.GetUUID().ToString(), GetRealVersion(entry.GetVersion()), entry.GetVersionType(), entry.GetArchitecture()));
            }
                
            versions.Sort((x, y) => x.Compare(y));


            //Get Latest Release and Beta Versions an Insert them into the ObservableCollection
            var latestRelease = versions.First(x => x.IsRelease == true && VersionDbExtensions.DoesVerionArchMatch(Constants.CurrentArchitecture, x.Architecture));
            var latestBeta = versions.First(x => x.IsBeta == true && VersionDbExtensions.DoesVerionArchMatch(Constants.CurrentArchitecture, x.Architecture));
            var latestPreview = versions.First(x => x.IsPreview == true && VersionDbExtensions.DoesVerionArchMatch(Constants.CurrentArchitecture, x.Architecture));

            this.latestReleaseRef = latestRelease;
            this.latestBetaRef = latestBeta;
            this.latestPreviewRef = latestPreview;



            var latest_preview = new MCVersion(Constants.LATEST_PREVIEW_UUID, Constants.LATEST_PREVIEW_UUID, Application.Current.Resources["EditInstallationScreen_LatestPreview"].ToString(), latestPreview.Type, Constants.CurrentArchitecture);
            var latest_beta = new MCVersion(Constants.LATEST_BETA_UUID, Constants.LATEST_BETA_UUID, Application.Current.Resources["EditInstallationScreen_LatestBeta"].ToString(), latestBeta.Type, Constants.CurrentArchitecture);
            var latest_release = new MCVersion(Constants.LATEST_RELEASE_UUID, Constants.LATEST_RELEASE_UUID, Application.Current.Resources["EditInstallationScreen_LatestRelease"].ToString(), latestRelease.Type, Constants.CurrentArchitecture);

            versions.Insert(0, latest_preview);
            versions.Insert(0, latest_beta);
            versions.Insert(0, latest_release);

            await SyncUpLocalVersions(versions, OnLoad);

            string GetRealVersion(string versionS)
            {
                if (MinecraftVersion.TryParse(versionS, out MinecraftVersion version)) return version.ToRealString();
                else return new Version(0, 0, 0, 0).ToString();
            }
        }
        private async Task SyncUpLocalVersions(ObservableCollection<MCVersion> versions, bool OnLoad = false)
        {
            DirectoryInfo directoryInfo = Directory.CreateDirectory(MainDataModel.Default.FilePaths.VersionsFolder);
            var webVersions = VersionDB.GetVersions();

            foreach (var directory in directoryInfo.EnumerateDirectories())
            {
                string mainifest_file = Path.Combine(directory.FullName, MCVersionExtensions.MainifestFileName);
                string packageId_file = Path.Combine(directory.FullName, MCVersionExtensions.IdentificationFilename);
                string customName_file = Path.Combine(directory.FullName, "custom_name.txt");
                string uuid = directory.Name;

                try
                {
                    if (File.Exists(mainifest_file))
                    {
                        //Legacy Version Support
                        if (directory.Name.StartsWith("Minecraft-"))
                        {
                            string legacyPkgID = directory.Name.Replace("Minecraft-", "");
                            if (!File.Exists(packageId_file))
                            {
                                if (versions.Exists(x => x.PackageID == legacyPkgID))
                                    await File.WriteAllTextAsync(packageId_file, legacyPkgID);
                            }
                            directory.Rename(legacyPkgID);
                            uuid = legacyPkgID;
                        }

                        string packageID = await FileExtensions.TryReadAllTextAsync(packageId_file, null);

                        if (!versions.Exists(x => x.UUID == uuid && x.PackageID == packageID))
                        {
                            var customVersion = await GetAppxMaifestIdentity(packageID, uuid, mainifest_file);
                            string customNameFallback = string.Format("{0}.{1}.{2}", customVersion.Name, customVersion.Type.ToString().FirstOrDefault(), customVersion.Architecture);
                            customVersion.CustomName = await FileExtensions.TryReadAllTextAsync(customName_file, customNameFallback);
                            versions.Add(customVersion);
                        }

                    }
                }
                catch
                {
                    //TODO: Add Exception Handling
                }

            }
        }
        private async Task<MCVersion> GetAppxMaifestIdentity(string PackageID, string UUID, string file)
        {
            var (Name, Version, ProcessorArchitecture) = await MCVersionExtensions.GetCommonPackageValuesAsync(file);

            VersionType Type;
            if (Name == "Microsoft.MinecraftUWP") Type = VersionType.Release;
            else if (Name == "Microsoft.MinecraftWindowsBeta") Type = VersionType.Preview;
            else throw new Exception("That's not a Minecraft APPX file silly!"); //TODO: Localize String

            return new MCVersion(UUID, PackageID, Version, Type, ProcessorArchitecture);
        }
        public MCVersion GetVersion(VersioningMode versioningMode, string versionUUID)
        {
            if (versioningMode != VersioningMode.None)
            {
                var latest_preview = MainDataModel.Default.Versions.ToList().FirstOrDefault(x => x.UUID == latestPreviewRef.UUID && x.Type == latestPreviewRef.Type);
                var latest_beta = MainDataModel.Default.Versions.ToList().FirstOrDefault(x => x.UUID == latestBetaRef.UUID && x.Type == latestBetaRef.Type);
                var latest_release = MainDataModel.Default.Versions.ToList().FirstOrDefault(x => x.UUID == latestReleaseRef.UUID && x.Type == latestReleaseRef.Type);

                if (versioningMode == VersioningMode.LatestPreview && latest_preview != null) return latest_preview;
                else if (versioningMode == VersioningMode.LatestBeta && latest_beta != null) return latest_beta;
                else if (versioningMode == VersioningMode.LatestRelease && latest_release != null) return latest_release;
                else return null;
            }
            else if (MainDataModel.Default.Versions.ToList().Exists(x => x.UUID == versionUUID))
            {
                return MainDataModel.Default.Versions.ToList().Where(x => x.UUID == versionUUID).FirstOrDefault();
            }
            else return null;
        }
    }
}