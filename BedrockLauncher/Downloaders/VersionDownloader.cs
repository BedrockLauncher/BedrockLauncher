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
using System.Xml.Linq;

namespace BedrockLauncher.Downloaders
{
    public class VersionDownloader
    {
        private VersionManager VersionDB = new VersionManager();

        private string winstoreDBFile => MainDataModel.Default.FilePaths.GetWinStoreVersionsDBFile();
        private string communityDBFile => MainDataModel.Default.FilePaths.GetCommunityVersionsDBFile();
        private string gdkLinksDBFile => MainDataModel.Default.FilePaths.GetGdkLinksVersionsDBFile();
        private string gdkVersionsDBFile => MainDataModel.Default.FilePaths.GetGdkVersionsDBFile();

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

        /// <summary>True when the catalog lists at least one resource for exactly the version's required GDK package.</summary>
        public bool HasGdkDownloadResource(MCVersion v)
        {
            return v.RequiredGdkPackage != null &&
                   VersionDB.TryGetGdkDownloadUrls(v.PackageID, out List<string> urls) &&
                   VersionJsonDb.FilterUrlsForIdentity(urls, v.RequiredGdkPackage).Length > 0;
        }

        /// <summary>Downloads exactly the required GDK package of a GDK version.</summary>
        public async Task DownloadGdkPackage(
            MCVersion v,
            string destination,
            DownloadProgress progress,
            CancellationToken cancellationToken)
        {
            if (v.PackageType != PackageType.GDK || v.RequiredGdkPackage == null)
                throw new InvalidOperationException($"Minecraft {v.Name} has no required GDK package to download.");

            await VersionDB.DownloadGdkPackage(
                v.PackageID,
                v.RequiredGdkPackage,
                destination,
                progress,
                cancellationToken);
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

            VersionDB.Init(
                userIndex,
                winstoreDBFile,
                communityDBFile,
                gdkLinksDBFile,
                gdkVersionsDBFile);

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
                    entry.GetPackageType(),
                    entry.GetRequiredGdkPackage()));
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
                latestPreview.PackageType,
                latestPreview.RequiredGdkPackage);

            MCVersion latest_release = new MCVersion(
                Constants.LATEST_RELEASE_UUID,
                Constants.LATEST_RELEASE_UUID,
                Application.Current.Resources["EditInstallationScreen_LatestRelease"].ToString(),
                latestRelease.Type,
                Constants.CurrentArchitecture,
                latestRelease.PackageType,
                latestRelease.RequiredGdkPackage);

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
                    latestBeta.PackageType,
                    latestBeta.RequiredGdkPackage);

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

                    // Only the package's own manifest / game config identify a version folder.
                    if (!hasManifest &&
                        !hasGdkConfig)
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

                    // Classify from the folder's own package files only (never from what Windows has installed).
                    var manifestIdentity = hasManifest
                        ? await MCVersionExtensions.GetCommonPackageValuesAsync(manifestFile)
                        : null;

                    string gameConfigIdentityName = hasGdkConfig
                        ? ReadGameConfigIdentityName(gdkConfig)
                        : null;

                    MinecraftPackageClassification classification =
                        MinecraftPackageClassifier.Classify(
                            manifestIdentity?.Item1,
                            gameConfigIdentityName);

                    if (!classification.IsResolved)
                    {
                        Trace.WriteLine(
                            $"Skipping version folder {directory.FullName}: {classification.Reason}");

                        continue;
                    }

                    MCVersion customVersion;

                    if (classification.PackageType == PackageType.GDK)
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

                        // The required package is the identity the folder's manifest declares; without a complete
                        // identity the requirement stays unresolved and the version cannot be launched.
                        GdkPackageIdentity requiredPackage = null;
                        if (manifestIdentity != null &&
                            Version.TryParse(manifestIdentity.Item2, out Version packageVersion) &&
                            packageVersion.Revision >= 0 &&
                            !string.IsNullOrWhiteSpace(manifestIdentity.Item3) &&
                            manifestIdentity.Item3 != "???")
                        {
                            requiredPackage = new GdkPackageIdentity(
                                MinecraftPackageFamilies.GetIdentityName(classification.VersionType),
                                packageVersion,
                                manifestIdentity.Item3,
                                MinecraftPackageFamilies.PublisherId);
                        }

                        customVersion = new MCVersion(
                            uuid,
                            packageID,
                            versionName,
                            classification.VersionType,
                            requiredPackage?.Architecture ?? Constants.CurrentArchitecture,
                            PackageType.GDK,
                            requiredPackage);
                    }
                    else
                    {
                        customVersion = new MCVersion(
                            uuid,
                            packageID,
                            manifestIdentity.Item2,
                            classification.VersionType,
                            manifestIdentity.Item3);
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
                catch (Exception ex)
                {
                    Trace.WriteLine(
                        $"Skipping unreadable version folder {directory.FullName}: {ex.Message}");
                }
            }
        }

        private static string ReadGameConfigIdentityName(string gameConfigPath)
        {
            XDocument gameConfig = XDocument.Load(gameConfigPath);

            return gameConfig.Root?
                .Elements()
                .FirstOrDefault(x => x.Name.LocalName == "Identity")?
                .Attribute("Name")?
                .Value;
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