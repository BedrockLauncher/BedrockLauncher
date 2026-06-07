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
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace BedrockLauncher.Downloaders
{
    public class VersionDownloader
    {
        private static readonly string[] RequiredGdkRuntimeDlls =
        {
            "vcruntime140_1.dll",
            "concrt140_app.dll",
            "msvcp140_app.dll",
            "vcruntime140_app.dll"
        };

        private VersionManager VersionDB = new VersionManager();

        private string winstoreDBFile => MainDataModel.Default.FilePaths.GetWinStoreVersionsDBFile();
        private string communityDBFile => MainDataModel.Default.FilePaths.GetCommunityVersionsDBFile();

        private MCVersion latestReleaseRef { get; set; }
        private MCVersion? latestBetaRef { get; set; }
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
            ObservableCollection<MCVersion> updatedVersions = new ObservableCollection<MCVersion>();

            // Retrive Versions into a temporary list first. This keeps already
            // playable local installs visible while the online feed refreshes.
            int userIndex = Properties.LauncherSettings.Default.CurrentInsiderAccountIndex;
            VersionDB.Init(userIndex, winstoreDBFile, communityDBFile);
            await VersionDB.LoadVersions(true, Properties.LauncherSettings.Default.FetchVersionsFromMicrosoftStore);

            //Add Versions to ObservableCollection, then Sort them
            List<VersionInfoJson> versionList = VersionDB.GetVersions();
            foreach (VersionInfoJson entry in versionList)
            {
                // Trace.WriteLine($"Found version: {entry.GetVersion()}");
                updatedVersions.Add(new MCVersion(entry.GetUUID().ToString(), entry.GetUUID().ToString(), GetRealVersion(entry.GetVersion()), entry.GetVersionType(), entry.GetArchitecture()));
            }

            AddRegisteredMinecraftPackageVersions(updatedVersions);
            await SyncUpLocalVersions(updatedVersions, OnLoad);
            updatedVersions.Sort((x, y) => x.Compare(y));


            //Get Latest Release and Beta Versions an Insert them into the ObservableCollection
            MCVersion latestRelease = updatedVersions.FirstOrDefault(x => x.IsRelease == true && VersionDbExtensions.DoesVerionArchMatch(Constants.CurrentArchitecture, x.Architecture));
            MCVersion? latestBeta = updatedVersions.FirstOrDefault(x => x.IsBeta == true && VersionDbExtensions.DoesVerionArchMatch(Constants.CurrentArchitecture, x.Architecture), null);
            MCVersion latestPreview = updatedVersions.FirstOrDefault(x => x.IsPreview == true && VersionDbExtensions.DoesVerionArchMatch(Constants.CurrentArchitecture, x.Architecture));

            this.latestReleaseRef = latestRelease;
            this.latestBetaRef = latestBeta;
            this.latestPreviewRef = latestPreview;

            if (latestPreview != null)
            {
                MCVersion latest_preview = new MCVersion(Constants.LATEST_PREVIEW_UUID, Constants.LATEST_PREVIEW_UUID, Application.Current.Resources["EditInstallationScreen_LatestPreview"].ToString(), latestPreview.Type, Constants.CurrentArchitecture);
                updatedVersions.Insert(0, latest_preview);
            }

            if (latestRelease != null)
            {
                MCVersion latest_release = new MCVersion(Constants.LATEST_RELEASE_UUID, Constants.LATEST_RELEASE_UUID, Application.Current.Resources["EditInstallationScreen_LatestRelease"].ToString(), latestRelease.Type, Constants.CurrentArchitecture);
                updatedVersions.Insert(0, latest_release);
            }
 
            if (latestBeta != null)
            {
                MCVersion latest_beta = new MCVersion(Constants.LATEST_BETA_UUID, Constants.LATEST_BETA_UUID, Application.Current.Resources["EditInstallationScreen_LatestBeta"].ToString(), latestBeta.Type, Constants.CurrentArchitecture);
                updatedVersions.Insert(0, latest_beta);
            }

            versions.Clear();
            foreach (MCVersion version in updatedVersions)
                versions.Add(version);

            string GetRealVersion(string versionS)
            {
                if (MinecraftVersion.TryParse(versionS, out MinecraftVersion version)) return version.ToRealString();
                else return new Version(0, 0, 0, 0).ToString();
            }
        }

        private void AddRegisteredMinecraftPackageVersions(ObservableCollection<MCVersion> versions)
        {
            try
            {
                var packageManager = new PackageManager();
                AddRegisteredMinecraftPackageVersions(versions, packageManager, VersionType.Release);
                AddRegisteredMinecraftPackageVersions(versions, packageManager, VersionType.Preview);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Unable to scan registered Minecraft packages: {ex}");
            }
        }

        private void AddRegisteredMinecraftPackageVersions(ObservableCollection<MCVersion> versions, PackageManager packageManager, VersionType type)
        {
            foreach (Package package in packageManager.FindPackagesForUser(string.Empty, Constants.GetPackageFamily(type)))
            {
                string displayVersion = GetDisplayVersion(package.Id.Version);
                string architecture = GetArchitecture(package);
                if (versions.Any(x => IsSameVersionEntry(x, displayVersion, architecture, type)))
                    continue;

                string packageIdentity = package.Id.FullName;
                versions.Add(new MCVersion(packageIdentity, packageIdentity, displayVersion, type, architecture));
                Trace.WriteLine($"Added registered Minecraft package version: {displayVersion} {architecture} {packageIdentity}");
            }
        }

        private static bool IsSameVersionEntry(MCVersion version, string displayVersion, string architecture, VersionType type)
        {
            if (version == null || version.Type != type) return false;
            if (!VersionDbExtensions.DoesVerionArchMatch(version.Architecture, architecture)) return false;
            if (System.Version.TryParse(version.Name, out System.Version existing) &&
                System.Version.TryParse(displayVersion, out System.Version detected))
            {
                return existing.Equals(detected);
            }

            return string.Equals(version.Name, displayVersion, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetArchitecture(Package package)
        {
            string architecture = package.Id.Architecture.ToString().ToLowerInvariant();
            return architecture switch
            {
                "x64" => "x64",
                "x86" => "x86",
                "arm64" => "arm64",
                "arm" => "arm",
                _ => Constants.CurrentArchitecture
            };
        }

        private static string GetDisplayVersion(PackageVersion version)
        {
            if (version.Major == 1 && version.Minor >= 22 && version.Build >= 100)
            {
                int feature = version.Build / 100;
                int patch = version.Build % 100;
                return patch > 0 ? $"{version.Minor}.{feature}.{patch}" : $"{version.Minor}.{feature}";
            }

            if (version.Major == 1 && version.Build >= 100)
            {
                int build = version.Build / 100;
                int revision = version.Build % 100;
                return revision > 0 ? $"1.{version.Minor}.{build}.{revision}" : $"1.{version.Minor}.{build}";
            }

            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private async Task SyncUpLocalVersions(ObservableCollection<MCVersion> versions, bool OnLoad = false)
        {
            DirectoryInfo directoryInfo = Directory.CreateDirectory(MainDataModel.Default.FilePaths.VersionsFolder);
            var webVersions = VersionDB.GetVersions();

            foreach (var directory in directoryInfo.EnumerateDirectories())
            {
                if (directory.Name.Equals("AppxBackups", StringComparison.OrdinalIgnoreCase) ||
                    directory.Name.Contains(".broken_", StringComparison.OrdinalIgnoreCase) ||
                    directory.Name.Contains(".incomplete_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string mainifest_file = FindFileIgnoringCase(directory.FullName, MCVersionExtensions.MainifestFileName);
                string packageId_file = FindFileIgnoringCase(directory.FullName, MCVersionExtensions.IdentificationFilename)
                    ?? Path.Combine(directory.FullName, MCVersionExtensions.IdentificationFilename);
                string customName_file = Path.Combine(directory.FullName, "custom_name.txt");
                string uuid = directory.Name;

                try
                {
                    if (!string.IsNullOrWhiteSpace(mainifest_file) && File.Exists(mainifest_file))
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
                        var folderVersion = await GetAppxMaifestIdentity(packageID, uuid, mainifest_file);
                        if (folderVersion.PackageType == PackageType.GDK && !IsCompleteGdkVersionDirectory(directory.FullName))
                        {
                            Trace.WriteLine($"Skipping incomplete or encrypted local GDK version folder: {directory.FullName}");
                            continue;
                        }

                        bool knownCatalogVersion = versions.Exists(x =>
                            (!string.IsNullOrWhiteSpace(packageID) && string.Equals(x.PackageID, packageID, StringComparison.OrdinalIgnoreCase)) ||
                            string.Equals(Path.GetFullPath(x.GameDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                Path.GetFullPath(directory.FullName).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                StringComparison.OrdinalIgnoreCase));

                        if (!knownCatalogVersion && !versions.Exists(x => x.UUID == uuid && x.PackageID == packageID))
                        {
                            var customVersion = folderVersion;
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
        private static bool IsCompleteGdkVersionDirectory(string directory)
        {
            return File.Exists(Path.Combine(directory, "Minecraft.Windows.exe")) &&
                File.Exists(Path.Combine(directory, "MicrosoftGame.Config")) &&
                Directory.Exists(Path.Combine(directory, "data")) &&
                RequiredGdkRuntimeDlls.All(fileName => File.Exists(Path.Combine(directory, fileName)));
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
                if (versioningMode == VersioningMode.LatestPreview && latestPreviewRef != null)
                {
                    MCVersion? latest_preview = MainDataModel.Default.Versions
                        .ToList().FirstOrDefault(x => x.UUID == latestPreviewRef.UUID && x.Type == latestPreviewRef.Type, null);
                    return latest_preview;
                }
                if (versioningMode == VersioningMode.LatestBeta && latestBetaRef != null)
                {
                    MCVersion? latest_beta = MainDataModel.Default.Versions
                        .ToList().FirstOrDefault(x => x.UUID == latestBetaRef.UUID && x.Type == latestBetaRef.Type, null);
                    return latest_beta;
                }
                if (versioningMode == VersioningMode.LatestRelease && latestReleaseRef != null)
                {
                    MCVersion? latest_release = MainDataModel.Default.Versions
                        .ToList().FirstOrDefault(x => x.UUID == latestReleaseRef.UUID && x.Type == latestReleaseRef.Type, null);
                    return latest_release;
                }
                else return null;
            }
            else if (MainDataModel.Default.Versions.ToList().Exists(x => x.UUID == versionUUID))
            {
                return MainDataModel.Default.Versions.ToList().Where(x => x.UUID == versionUUID).FirstOrDefault();
            }

            return TryGetLocalVersionFromDisk(versionUUID);
        }

        private MCVersion TryGetLocalVersionFromDisk(string versionUUID)
        {
            if (string.IsNullOrWhiteSpace(versionUUID))
                return null;

            string directory = Path.Combine(MainDataModel.Default.FilePaths.VersionsFolder, versionUUID);
            string manifestFile = FindFileIgnoringCase(directory, MCVersionExtensions.MainifestFileName);
            if (!Directory.Exists(directory) || !File.Exists(manifestFile))
                return null;

            try
            {
                var (packageName, packageVersion, processorArchitecture) = GetManifestIdentity(manifestFile);
                VersionType type;
                if (string.Equals(packageName, "Microsoft.MinecraftUWP", StringComparison.OrdinalIgnoreCase))
                    type = VersionType.Release;
                else if (string.Equals(packageName, "Microsoft.MinecraftWindowsBeta", StringComparison.OrdinalIgnoreCase))
                    type = VersionType.Preview;
                else
                    return null;

                string packageIdFile = FindFileIgnoringCase(directory, MCVersionExtensions.IdentificationFilename)
                    ?? Path.Combine(directory, MCVersionExtensions.IdentificationFilename);
                string packageId = File.Exists(packageIdFile)
                    ? File.ReadAllText(packageIdFile).Trim()
                    : versionUUID;

                if (string.IsNullOrWhiteSpace(packageId))
                    packageId = versionUUID;

                string displayVersion = GetDisplayVersionFromPackageVersion(versionUUID, packageVersion, type);
                MCVersion localVersion = new MCVersion(versionUUID, packageId, displayVersion, type, processorArchitecture);

                if (localVersion.PackageType == PackageType.GDK && !IsCompleteGdkVersionDirectory(directory))
                    return null;

                return localVersion;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Unable to resolve local Minecraft version {versionUUID}: {ex}");
                return null;
            }
        }

        private static (string PackageName, string PackageVersion, string Architecture) GetManifestIdentity(string manifestPath)
        {
            XElement identity = XDocument.Load(manifestPath)
                .Descendants()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Identity", StringComparison.OrdinalIgnoreCase));

            if (identity == null)
                throw new InvalidDataException("Minecraft package manifest does not contain an Identity element.");

            return (
                identity.Attribute("Name")?.Value,
                identity.Attribute("Version")?.Value,
                identity.Attribute("ProcessorArchitecture")?.Value
            );
        }

        private static string FindFileIgnoringCase(string directory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return null;

            return Directory.EnumerateFiles(directory)
                .FirstOrDefault(path => string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetDisplayVersionFromPackageVersion(string folderName, string packageVersion, VersionType type)
        {
            string normalizedFolderName = folderName;
            if (type == VersionType.Preview &&
                normalizedFolderName.StartsWith("preview-", StringComparison.OrdinalIgnoreCase))
            {
                normalizedFolderName = normalizedFolderName.Substring("preview-".Length);
            }
            else if (type == VersionType.Beta &&
                normalizedFolderName.StartsWith("beta-", StringComparison.OrdinalIgnoreCase))
            {
                normalizedFolderName = normalizedFolderName.Substring("beta-".Length);
            }

            if (System.Version.TryParse(normalizedFolderName, out _))
                return normalizedFolderName;

            if (!System.Version.TryParse(packageVersion, out System.Version version))
                return packageVersion;

            if (version.Major == 1 && version.Minor >= 22 && version.Build >= 100)
            {
                int feature = version.Build / 100;
                int patch = version.Build % 100;
                return patch > 0 ? $"{version.Minor}.{feature}.{patch}" : $"{version.Minor}.{feature}";
            }

            if (version.Major == 1 && version.Build >= 100)
            {
                int build = version.Build / 100;
                int revision = version.Build % 100;
                return revision > 0 ? $"1.{version.Minor}.{build}.{revision}" : $"1.{version.Minor}.{build}";
            }

            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}
