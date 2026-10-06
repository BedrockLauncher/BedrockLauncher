using BedrockLauncher.Classes;
using System;
using System.IO;
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
using System.Collections.Generic;
using static BedrockLauncher.UpdateProcessor.Handlers.VersionManager;
using BedrockLauncher.UpdateProcessor.Handlers;
using BedrockLauncher.UpdateProcessor.Extensions;
using BedrockLauncher.Enums;
using BedrockLauncher.UpdateProcessor.Enums;

namespace BedrockLauncher.Downloaders
{
    public class VersionDownloader
    {
        private VersionManager VersionDB = new VersionManager();

        private string winstoreDBFile => MainDataModel.Default.FilePaths.GetWinStoreVersionsDBFile();
        private string communityDBFile => MainDataModel.Default.FilePaths.GetCommunityVersionsDBFile();
        private string gdkLinksDBFile => MainDataModel.Default.FilePaths.GetGdkLinksVersionsDBFile();

        private MCVersion latestReleaseRef { get; set; }
        private MCVersion? latestBetaRef { get; set; }
        private MCVersion latestPreviewRef { get; set; }

        public async Task DownloadVersion(
            string versionName,
            string packageID,
            int revisionNumber,
            string destination,
            DownloadProgress progress,
            CancellationToken cancellationToken,
            VersionType versionType)
        {
            await VersionDB.DownloadVersion(
                versionName,
                GetUpdateIdentity(packageID),
                revisionNumber,
                destination,
                progress,
                cancellationToken,
                versionType);

            string GetUpdateIdentity(string packageID)
            {
                if (packageID == Constants.LATEST_BETA_UUID)
                    return latestBetaRef.PackageID;

                if (packageID == Constants.LATEST_RELEASE_UUID)
                    return latestReleaseRef.PackageID;

                if (packageID == Constants.LATEST_PREVIEW_UUID)
                    return latestPreviewRef.PackageID;

                return packageID;
            }
        }

        /// <summary>The CDN resources GdkLinks lists for a GDK version (keyed by the version's package id).</summary>
        public bool TryGetGdkDownloadUrls(string packageID, out List<string> urls)
        {
            return VersionDB.TryGetGdkDownloadUrls(packageID, out urls);
        }

        public void SetMSAUserToken(string token)
        {
            VersionDB.SetMSAUserToken(token);
        }

        public async Task UpdateVersionList(
            ObservableCollection<MCVersion> versions,
            bool OnLoad = false)
        {
            bool AllowUpdating =
                OnLoad && Debugger.IsAttached
                    ? Constants.Debugging.RetriveNewVersionsOnLoad
                    : true;

            versions.Clear();

            int userIndex =
                Properties.LauncherSettings.Default.CurrentInsiderAccountIndex;
            string microsoftAccountId =
                MainDataModel.Default.Config.CurrentProfile
                    ?.MicrosoftAccountId;

            VersionDB.Init(
                userIndex,
                winstoreDBFile,
                communityDBFile,
                gdkLinksDBFile,
                microsoftAccountId);

            await VersionDB.LoadVersions(
                true,
                Properties.LauncherSettings.Default.FetchVersionsFromMicrosoftStore);

            List<VersionInfoJson> versionList = VersionDB.GetVersions();

            foreach (VersionInfoJson entry in versionList)
            {
                versions.Add(new MCVersion(
                    entry.GetUUID().ToString(),
                    entry.GetUUID().ToString(),
                    GetRealVersion(entry.GetVersion()),
                    entry.GetVersionType(),
                    entry.GetArchitecture(),
                    entry.GetPackageType()));
            }

            versions.Sort((x, y) => x.Compare(y));

            MCVersion latestRelease = versions.First(
                x => x.IsRelease &&
                VersionDbExtensions.DoesVersionArchMatch(
                    Constants.CurrentArchitecture,
                    x.Architecture));

            MCVersion? latestBeta = versions.FirstOrDefault(
                x => x.IsBeta &&
                VersionDbExtensions.DoesVersionArchMatch(
                    Constants.CurrentArchitecture,
                    x.Architecture),
                null);

            MCVersion latestPreview = versions.First(
                x => x.IsPreview &&
                VersionDbExtensions.DoesVersionArchMatch(
                    Constants.CurrentArchitecture,
                    x.Architecture));

            latestReleaseRef = latestRelease;
            latestBetaRef = latestBeta;
            latestPreviewRef = latestPreview;

            MCVersion latest_preview = new MCVersion(
                Constants.LATEST_PREVIEW_UUID,
                Constants.LATEST_PREVIEW_UUID,
                Application.Current.Resources["EditInstallationScreen_LatestPreview"].ToString(),
                latestPreview.Type,
                Constants.CurrentArchitecture,
                latestPreview.PackageType);

            MCVersion latest_release = new MCVersion(
                Constants.LATEST_RELEASE_UUID,
                Constants.LATEST_RELEASE_UUID,
                Application.Current.Resources["EditInstallationScreen_LatestRelease"].ToString(),
                latestRelease.Type,
                Constants.CurrentArchitecture,
                latestRelease.PackageType);

            versions.Insert(0, latest_preview);
            versions.Insert(0, latest_release);

            if (latestBeta != null)
            {
                MCVersion latest_beta = new MCVersion(
                    Constants.LATEST_BETA_UUID,
                    Constants.LATEST_BETA_UUID,
                    Application.Current.Resources["EditInstallationScreen_LatestBeta"].ToString(),
                    latestBeta.Type,
                    Constants.CurrentArchitecture,
                    latestBeta.PackageType);

                versions.Insert(0, latest_beta);
            }

            await SyncUpLocalVersions(versions, OnLoad);

            string GetRealVersion(string versionS)
            {
                if (MinecraftVersion.TryParse(
                    versionS,
                    out MinecraftVersion version))
                {
                    return version.ToRealString();
                }

                return new Version(0, 0, 0, 0).ToString();
            }
        }

        private async Task SyncUpLocalVersions(
            ObservableCollection<MCVersion> versions,
            bool OnLoad = false)
        {
            DirectoryInfo directoryInfo =
                Directory.CreateDirectory(
                    MainDataModel.Default.FilePaths.VersionsFolder);

            foreach (var directory in directoryInfo.EnumerateDirectories())
            {
                if (directory.Name.Equals(
                    "AppxBackups",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string manifestFile = Path.Combine(
                    directory.FullName,
                    MCVersionExtensions.MainifestFileName);

                string exeFile = Path.Combine(
                    directory.FullName,
                    "Minecraft.Windows.exe");

                string gdkConfig = Path.Combine(
                    directory.FullName,
                    "MicrosoftGame.Config");

                string cdnPackageFile = Path.Combine(
                    directory.FullName,
                    "cdn_package.txt");

                string packageIdFile = Path.Combine(
                    directory.FullName,
                    MCVersionExtensions.IdentificationFilename);

                string customNameFile = Path.Combine(
                    directory.FullName,
                    "custom_name.txt");

                string uuid = directory.Name;

                try
                {
                    bool hasManifest = File.Exists(manifestFile);
                    bool hasExe = File.Exists(exeFile);
                    bool hasGdkConfig = File.Exists(gdkConfig);
                    bool hasCdnPackage = File.Exists(cdnPackageFile);

                    if (!hasManifest &&
                        !hasExe &&
                        !hasGdkConfig &&
                        !hasCdnPackage)
                    {
                        continue;
                    }

                    // Legacy Version Support
                    if (directory.Name.StartsWith("Minecraft-"))
                    {
                        string legacyPkgID =
                            directory.Name.Replace("Minecraft-", "");

                        if (!File.Exists(packageIdFile))
                        {
                            if (versions.Exists(
                                x => x.PackageID == legacyPkgID))
                            {
                                await File.WriteAllTextAsync(
                                    packageIdFile,
                                    legacyPkgID);
                            }
                        }

                        directory.Rename(legacyPkgID);
                        uuid = legacyPkgID;
                    }

                    string packageID =
                        await FileExtensions.TryReadAllTextAsync(
                            packageIdFile,
                            uuid);

                    MCVersion existing =
                        versions.FirstOrDefault(
                            x => x.UUID == uuid ||
                                 x.PackageID == packageID ||
                                 x.Name == uuid);

                    if (existing != null)
                    {
                        continue;
                    }

                    MCVersion customVersion = null;

                    if (hasGdkConfig || hasCdnPackage)
                    {
                        string versionName = uuid;

                        if (hasExe)
                        {
                            FileVersionInfo fileVersion =
                                FileVersionInfo.GetVersionInfo(exeFile);

                            versionName =
                                fileVersion.ProductVersion ??
                                fileVersion.FileVersion ??
                                uuid;
                        }

                        customVersion = new MCVersion(
                            uuid,
                            packageID,
                            versionName,
                            VersionType.Release,
                            Constants.CurrentArchitecture,
                            PackageType.GDK);
                    }
                    else if (hasManifest)
                    {
                        customVersion =
                            await GetAppxMaifestIdentity(
                                packageID,
                                uuid,
                                manifestFile);
                    }
                    else if (hasExe)
                    {
                        FileVersionInfo fileVersion =
                            FileVersionInfo.GetVersionInfo(exeFile);

                        string versionName =
                            fileVersion.ProductVersion ??
                            fileVersion.FileVersion ??
                            uuid;

                        customVersion = new MCVersion(
                            uuid,
                            packageID,
                            versionName,
                            VersionType.Release,
                            Constants.CurrentArchitecture,
                            PackageType.UWP);
                    }

                    if (customVersion != null)
                    {
                        string customNameFallback =
                            string.Format(
                                "{0}.{1}.{2}",
                                customVersion.Name,
                                customVersion.Type
                                    .ToString()
                                    .FirstOrDefault(),
                                customVersion.Architecture);

                        customVersion.CustomName =
                            await FileExtensions.TryReadAllTextAsync(
                                customNameFile,
                                customNameFallback);

                        versions.Add(customVersion);
                    }
                }
                catch
                {
                    // Ignore corrupted folder
                }
            }
        }

        private async Task<MCVersion> GetAppxMaifestIdentity(
            string PackageID,
            string UUID,
            string file)
        {
            var (
                Name,
                Version,
                ProcessorArchitecture) =
                await MCVersionExtensions.GetCommonPackageValuesAsync(file);

            VersionType Type;

            if (Name == "Microsoft.MinecraftUWP")
            {
                Type = VersionType.Release;
            }
            else if (Name == "Microsoft.MinecraftWindowsBeta")
            {
                Type = VersionType.Preview;
            }
            else
            {
                throw new Exception(
                    "That's not a Minecraft APPX file silly!");
            }

            return new MCVersion(
                UUID,
                PackageID,
                Version,
                Type,
                ProcessorArchitecture);
        }

        public MCVersion GetVersion(
            VersioningMode versioningMode,
            string versionUUID)
        {
            if (versioningMode != VersioningMode.None)
            {
                if (versioningMode == VersioningMode.LatestPreview &&
                    latestPreviewRef != null)
                {
                    MCVersion? latest_preview =
                        MainDataModel.Default.Versions
                            .ToList()
                            .FirstOrDefault(
                                x => x.UUID == latestPreviewRef.UUID &&
                                     x.Type == latestPreviewRef.Type,
                                null);

                    return latest_preview;
                }

                if (versioningMode == VersioningMode.LatestBeta &&
                    latestBetaRef != null)
                {
                    MCVersion? latest_beta =
                        MainDataModel.Default.Versions
                            .ToList()
                            .FirstOrDefault(
                                x => x.UUID == latestBetaRef.UUID &&
                                     x.Type == latestBetaRef.Type,
                                null);

                    return latest_beta;
                }

                if (versioningMode == VersioningMode.LatestRelease &&
                    latestReleaseRef != null)
                {
                    MCVersion? latest_release =
                        MainDataModel.Default.Versions
                            .ToList()
                            .FirstOrDefault(
                                x => x.UUID == latestReleaseRef.UUID &&
                                     x.Type == latestReleaseRef.Type,
                                null);

                    return latest_release;
                }

                return null;
            }

            if (MainDataModel.Default.Versions
                .ToList()
                .Exists(x => x.UUID == versionUUID))
            {
                return MainDataModel.Default.Versions
                    .ToList()
                    .FirstOrDefault(x => x.UUID == versionUUID);
            }

            return null;
        }
    }
}