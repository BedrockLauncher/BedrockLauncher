using BedrockLauncher.Classes;
using BedrockLauncher.Enums;
using BedrockLauncher.Exceptions;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.UpdateProcessor.Extensions;
using BedrockLauncher.ViewModels;
using JemExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualBasic.FileIO;
using Windows.ApplicationModel;

namespace BedrockLauncher.Handlers
{
    public partial class PackageHandler
    {
        // ERROR_INSTALL_PACKAGE_DOWNGRADE
        // winerror.h / FACILITY_WIN32
        // HRESULT: 0x80073D06
        private const int ErrorInstallPackageDowngrade =
            unchecked((int)0x80073D06);

        #region GDK Public Methods

        public async Task PlayGdkPackage(
            BLProfile profile,
            MCVersion v,
            bool keepLauncherOpen,
            bool launchEditor)
        {
            try
            {
                StartTask();

                Package installed =
                    await EnsureGdkVersionInstalled(profile, v);

                if (installed == null)
                {
                    throw new AppLaunchFailedException(
                        new InvalidOperationException(
                            $"Minecraft GDK version {v.DisplayName} is not installed."));
                }

                string familyName =
                    installed.Id.FamilyName;

                string installedLocation =
                    GetPackageInstalledLocation(installed);

                if (string.IsNullOrWhiteSpace(installedLocation))
                {
                    throw new AppLaunchFailedException(
                        new InvalidOperationException(
                            $"Could not resolve the installation location for {installed.Id.FullName}."));
                }

                string applicationId =
                    GdkRegistration.GetApplicationId(installedLocation);

                MainDataModel.Default.ProgressBarState
                    .SetProgressBarState(LauncherState.isLaunching);

                bool launched =
                    await LaunchGdkPackage(
                        v,
                        familyName,
                        applicationId,
                        launchEditor);

                if (!launched)
                {
                    throw new AppLaunchFailedException(
                        $"Could not launch the Minecraft GDK version: {v.DisplayName}",
                        new InvalidOperationException(
                            "Windows did not start the installed Minecraft package."));
                }

                Trace.WriteLine(
                    $"GDK launch requested for {v.DisplayName}.");

                if (keepLauncherOpen)
                {
                    await GetGameHandle(
                        Constants.MINECRAFT_PROCESS_NAME);
                }
                else
                {
                    await Application.Current.Dispatcher.InvokeAsync(
                        () => Application.Current.MainWindow?.Close());
                }
            }
            catch (PackageManagerException e)
            {
                EndTask();
                SetException(e);
            }
            catch (OperationCanceledException e)
            {
                EndTask();
                SetException(
                    new PackageDownloadCanceledException(e));
            }
            catch (Exception e)
            {
                EndTask();
                SetException(
                    new AppLaunchFailedException(e));
            }
        }

        public async Task InstallGdkPackage(
            BLProfile profile,
            MCVersion v,
            bool force = false)
        {
            try
            {
                StartTask();

                await EnsureGdkVersionInstalled(
                    profile,
                    v,
                    force);
            }
            catch (PackageManagerException e)
            {
                SetException(e);
            }
            catch (OperationCanceledException e)
            {
                SetException(
                    new PackageDownloadCanceledException(e));
            }
            catch (Exception e)
            {
                SetException(
                    new PackageExtractionFailedException(e));
            }
            finally
            {
                EndTask();
            }
        }

        #endregion

        #region GDK Official Install

        private async Task<Package> EnsureGdkVersionInstalled(
            BLProfile profile,
            MCVersion v,
            bool force = false)
        {
            await VerifyGdkEntitlement(
                profile,
                v);

            Package exact =
                GetInstalledMinecraftPackage(
                    v,
                    requireExactVersion: true);

            if (exact != null && !force)
            {
                Trace.WriteLine(
                    $"GDK {v.Name} is already installed by Windows: " +
                    exact.Id.FullName);

                return exact;
            }

            SaveDataGuard guard =
                ProtectSaveData(v.Type);

            try
            {
                return await AcquireAndInstallGdkThroughWindows(v);
            }
            finally
            {
                RestoreSaveData(
                    v.Type,
                    guard);
            }
        }

        private async Task VerifyGdkEntitlement(
            BLProfile profile,
            MCVersion v)
        {
            MainDataModel.Default.ProgressBarState
                .SetProgressBarText(v.DisplayName);

            GdkEntitlementResult entitlement =
                await GdkEntitlementService.VerifyAsync(
                    profile,
                    v.Type);

            if (entitlement == null ||
                !entitlement.IsEntitled)
            {
                throw new GdkEntitlementException(
                    entitlement?.Message ??
                    "You are not entitled to use this Minecraft version.");
            }
        }

        private static GdkVersionUnavailableException
            CreateGdkNoSourceException(
                MCVersion v,
                string reason)
        {
            return new GdkVersionUnavailableException(
                $"Minecraft {v.Name} cannot be installed: {reason} " +
                "Windows does not have this version installed, so there is no " +
                "authorised Microsoft source for it right now.");
        }

        private async Task<Package>
            AcquireAndInstallGdkThroughWindows(
                MCVersion v)
        {
            if (!VersionDownloader.TryGetGdkDownloadUrls(
                    v.PackageID,
                    out List<string> urls) ||
                urls == null ||
                urls.Count == 0)
            {
                throw CreateGdkNoSourceException(
                    v,
                    $"GdkLinks lists no download resource for it " +
                    $"(id {v.PackageID}).");
            }

            string identityProblem =
                ValidateGdkResourceIdentity(
                    v,
                    urls);

            if (identityProblem != null)
            {
                throw CreateGdkNoSourceException(
                    v,
                    $"the GdkLinks resource does not match this package: " +
                    identityProblem);
            }

            string packagePath =
                await EnsureMsixvcDownloaded(v);

            try
            {
                Package installed =
                    await DeployGdkPackageThroughWindows(
                        v,
                        packagePath);

                WriteGdkVersionMetadata(
                    v,
                    installed);

                return installed;
            }
            finally
            {
                if (!Properties.LauncherSettings.Default.KeepAppx)
                {
                    SafeDeleteFile(packagePath);
                }
            }
        }

        private async Task<string>
            EnsureMsixvcDownloaded(
                MCVersion v)
        {
            string cacheDirectory =
                GetPackageCacheDirectory(v);

            Directory.CreateDirectory(
                cacheDirectory);

            string packagePath =
                Path.Combine(
                    cacheDirectory,
                    GetMinecraftPackageFileName(
                        v,
                        ".msixvc"));

            bool cachedUsable =
                File.Exists(packagePath) &&
                new FileInfo(packagePath).Length > 0 &&
                !LooksLikeTextResponse(
                    packagePath,
                    out _);

            if (!cachedUsable)
            {
                SafeDeleteFile(packagePath);

                SetCancelation(true);

                try
                {
                    await DownloadPackage(
                        v,
                        packagePath,
                        CancelSource);
                }
                finally
                {
                    SetCancelation(false);
                }
            }
            else
            {
                Trace.WriteLine(
                    $"Reusing cached MSIXVC for {v.Name}: {packagePath}");
            }

            if (!File.Exists(packagePath))
            {
                throw new PackageDownloadFailedException(
                    $"The package download for {v.Name} did not create a file.",
                    new FileNotFoundException(
                        "MSIXVC package was not created.",
                        packagePath));
            }

            if (LooksLikeTextResponse(
                    packagePath,
                    out string downloadProblem))
            {
                SafeDeleteFile(packagePath);

                throw new PackageDownloadFailedException(
                    $"The CDN did not return a package for {v.Name}: " +
                    downloadProblem,
                    new InvalidDataException(downloadProblem));
            }

            return packagePath;
        }

        private async Task<Package>
            DeployGdkPackageThroughWindows(
                MCVersion v,
                string packagePath)
        {
            try
            {
                await RunWindowsDeployment(
                    packagePath);
            }
            catch (GdkDeploymentRejectedException e)
                when (e.DeploymentHResult ==
                      ErrorInstallPackageDowngrade)
            {
                Trace.WriteLine(
                    $"Windows reported a higher installed version; " +
                    $"asking to switch: {e.Message}");

                await SwitchAwayFromHigherVersion(v);

                await RunWindowsDeployment(
                    packagePath);
            }

            Package installed =
                GetInstalledMinecraftPackage(
                    v,
                    requireExactVersion: true);

            if (installed == null)
            {
                throw new PackageRegistrationFailedException(
                    new InvalidOperationException(
                        $"Windows accepted {Path.GetFileName(packagePath)} " +
                        $"but no installed Minecraft package matches version {v.Name}."));
            }

            Trace.WriteLine(
                $"Windows installed {installed.Id.FullName}");

            return installed;
        }

        private async Task RunWindowsDeployment(
            string packagePath)
        {
            string fileName =
                Path.GetFileName(packagePath);

            try
            {
                Trace.WriteLine(
                    $"Requesting Windows to install {fileName}");

                MainDataModel.Default.ProgressBarState
                    .SetProgressBarText(fileName);

                MainDataModel.Default.ProgressBarState
                    .SetProgressBarState(
                        LauncherState.isRegisteringPackage);

                await DeploymentProgressWrapper(
                    PM.AddPackageAsync(
                        new Uri(Path.GetFullPath(packagePath)),
                        null,
                        Constants.StorePackageDeploymentOptions));
            }
            catch (Exception ex)
            {
                int hresult =
                    ex.InnerException?.HResult ??
                    ex.HResult;

                string code =
                    hresult != 0
                        ? $" (HRESULT 0x{hresult:X8})"
                        : string.Empty;

                throw new GdkDeploymentRejectedException(
                    $"Windows rejected the installation of {fileName}" +
                    $"{code}: {ex.Message}",
                    hresult,
                    ex);
            }
            finally
            {
                ResetTask();
            }
        }

        #endregion

        #region GDK Version Switching

        private async Task SwitchAwayFromHigherVersion(
            MCVersion v)
        {
            Package current =
                GetInstalledMinecraftPackage(
                    v,
                    requireExactVersion: false);

            string currentVersion =
                current != null
                    ? GetPackageVersionString(current)
                    : "the currently installed version";

            var answer =
                await DialogPrompt.ShowDialog_YesNo(
                    "Switch Minecraft version",
                    $"Windows keeps only one Minecraft GDK package installed at a time, " +
                    $"and a newer version ({currentVersion}) is installed. To install " +
                    $"and run {v.DisplayName}, the newer version has to be removed first. " +
                    "Your worlds and settings are kept, and you can reinstall the newer " +
                    "version from the Microsoft Store or the Xbox app whenever you want. " +
                    "Continue?");

            if (answer != System.Windows.Forms.DialogResult.Yes)
            {
                throw new OperationCanceledException(
                    "Version switch cancelled: the installed Minecraft was left untouched.");
            }

            await UnregisterLauncherGdkRegistrations(v);

            if (current == null)
                return;

            MainDataModel.Default.ProgressBarState
                .SetProgressBarText(
                    current.Id.FullName);

            MainDataModel.Default.ProgressBarState
                .SetProgressBarState(
                    LauncherState.isRemovingPackage);

            await RemoveWindowsGdkPackage(
                current.Id.FullName);

            Trace.WriteLine(
                $"Removed higher GDK version: {current.Id.FullName}");
        }

        private async Task RemoveWindowsGdkPackage(
            string packageFullName)
        {
            try
            {
                await DeploymentProgressWrapper(
                    PM.RemovePackageAsync(
                        packageFullName,
                        Constants.PackageRemovalOptions));
            }
            catch (Exception winrtError)
            {
                Trace.WriteLine(
                    $"WinRT removal failed, trying PowerShell: {winrtError}");

                string command =
                    $"Remove-AppxPackage -Package " +
                    $"'{PowerShellPackageCommand.Escape(packageFullName)}'";

                await PowerShellPackageCommand.RunAsync(
                    command,
                    "GDK package removal");
            }
        }

        private async Task UnregisterLauncherGdkRegistrations(
            MCVersion v)
        {
            IEnumerable<Package> packages;

            try
            {
                packages =
                    PM.FindPackagesForUser(
                        string.Empty,
                        Constants.GetPackageFamily(v.Type))
                    .ToList();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"Failed to enumerate GDK registrations: {ex}");

                return;
            }

            foreach (Package package in packages)
            {
                string location =
                    GetPackageInstalledLocation(package);

                if (!IsPathInside(
                        MainDataModel.Default.FilePaths.VersionsFolder,
                        location))
                {
                    continue;
                }

                Trace.WriteLine(
                    $"Removing launcher GDK registration: " +
                    package.Id.FullName);

                try
                {
                    await GdkRegistration.UnregisterAsync(
                        package.Id.FullName);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(
                        $"Failed to unregister {package.Id.FullName}: {ex}");
                }
            }
        }

        #endregion

        #region GDK Launch

        private async Task<bool> LaunchGdkPackage(
            MCVersion v,
            string packageFamilyName,
            string applicationId,
            bool launchEditor)
        {
            if (launchEditor)
            {
                return await TryLaunchSupportedUri(
                    new Uri(
                        $"{Constants.GetUri(v.Type)}:?Editor=True"));
            }

            return GdkRegistration.Activate(
                packageFamilyName,
                applicationId);
        }

        #endregion

        #region GDK Metadata

        private void WriteGdkVersionMetadata(
            MCVersion v,
            Package installed)
        {
            try
            {
                string directory =
                    v.GameDirectory;

                EnsureSafeLauncherVersionDirectory(
                    directory);

                Directory.CreateDirectory(
                    directory);

                var metadata = new
                {
                    version = v.Name,
                    uuid = v.UUID,
                    packageId = v.PackageID,
                    type = v.Type.ToString(),
                    architecture = v.Architecture,
                    packageType = v.PackageType.ToString(),
                    packageFamilyName = installed?.Id?.FamilyName,
                    packageFullName = installed?.Id?.FullName,
                    source = "GdkLinks",
                    installedVia = "Windows/GamingServices",
                    contentArchivedLocally = false,
                    installedUtc = DateTime.UtcNow.ToString("o")
                };

                File.WriteAllText(
                    Path.Combine(
                        directory,
                        "metadata.json"),
                    JsonConvert.SerializeObject(
                        metadata,
                        Formatting.Indented));
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"Could not write GDK version metadata for {v.Name}: {ex}");
            }
        }

        #endregion

        #region Save Data

        private sealed class SaveDataGuard
        {
            public string MojangPath { get; set; }

            public string LinkTarget { get; set; }

            public string MovedBackupPath { get; set; }
        }

        private SaveDataGuard ProtectSaveData(
            VersionType type)
        {
            var guard =
                new SaveDataGuard();

            try
            {
                string localAppData =
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData);

                string mojang =
                    Path.Combine(
                        localAppData,
                        "Packages",
                        Constants.GetPackageFamily(type),
                        "LocalState",
                        "games",
                        "com.mojang");

                guard.MojangPath =
                    mojang;

                if (!Directory.Exists(mojang))
                    return guard;

                var info =
                    new DirectoryInfo(mojang);

                bool isReparsePoint =
                    (info.Attributes &
                     FileAttributes.ReparsePoint) ==
                    FileAttributes.ReparsePoint;

                if (isReparsePoint)
                {
                    guard.LinkTarget =
                        info.LinkTarget;

                    Directory.Delete(
                        mojang,
                        false);

                    Trace.WriteLine(
                        $"Detached save-data link before removal " +
                        $"(real worlds kept at: {guard.LinkTarget}).");

                    return guard;
                }

                bool hasContent =
                    Directory.EnumerateFileSystemEntries(
                        mojang)
                    .Any();

                if (!hasContent)
                    return guard;

                string backup =
                    mojang +
                    ".launcher-backup-" +
                    Guid.NewGuid().ToString("N");

                Directory.Move(
                    mojang,
                    backup);

                guard.MovedBackupPath =
                    backup;

                Trace.WriteLine(
                    $"Moved save-data folder aside before removal: {backup}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"Could not protect save data before removal: {ex}");
            }

            return guard;
        }

        private void RestoreSaveData(
            VersionType type,
            SaveDataGuard guard)
        {
            if (guard == null ||
                string.IsNullOrEmpty(guard.MojangPath))
            {
                return;
            }

            if (string.IsNullOrEmpty(guard.LinkTarget) &&
                string.IsNullOrEmpty(guard.MovedBackupPath))
            {
                return;
            }

            try
            {
                string parent =
                    Path.GetDirectoryName(
                        guard.MojangPath);

                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                if (Directory.Exists(
                        guard.MojangPath))
                {
                    var info =
                        new DirectoryInfo(
                            guard.MojangPath);

                    bool isLink =
                        (info.Attributes &
                         FileAttributes.ReparsePoint) ==
                        FileAttributes.ReparsePoint;

                    bool isEmpty =
                        !isLink &&
                        !Directory.EnumerateFileSystemEntries(
                            guard.MojangPath)
                        .Any();

                    if (isLink || isEmpty)
                    {
                        Directory.Delete(
                            guard.MojangPath,
                            false);
                    }
                    else
                    {
                        Trace.WriteLine(
                            "Save-data folder was recreated with content; " +
                            "leaving it untouched.");

                        return;
                    }
                }

                if (!string.IsNullOrEmpty(
                        guard.LinkTarget))
                {
                    SymLinkHelper.CreateSymbolicLinkSafe(
                        guard.MojangPath,
                        guard.LinkTarget,
                        SymLinkHelper.SymbolicLinkType.Directory);

                    Trace.WriteLine(
                        $"Reattached save-data link: " +
                        $"{guard.MojangPath} -> {guard.LinkTarget}");
                }
                else if (!string.IsNullOrEmpty(
                             guard.MovedBackupPath) &&
                         Directory.Exists(
                             guard.MovedBackupPath))
                {
                    Directory.Move(
                        guard.MovedBackupPath,
                        guard.MojangPath);

                    Trace.WriteLine(
                        $"Restored save-data folder from backup: " +
                        $"{guard.MojangPath}");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"Could not restore save data after install: {ex}");
            }
        }

        #endregion

        #region GDK Installed Package Lookup

        private Package GetInstalledMinecraftPackage(
            MCVersion v,
            bool requireExactVersion = false)
        {
            try
            {
                List<Package> packages =
                    PM.FindPackagesForUser(
                        string.Empty,
                        Constants.GetPackageFamily(v.Type))
                    .Where(package =>
                    {
                        string location =
                            GetPackageInstalledLocation(package);

                        bool isOfficial =
                            string.IsNullOrWhiteSpace(location) ||
                            IsOfficialMinecraftPackageLocation(location);

                        if (!isOfficial)
                        {
                            Trace.WriteLine(
                                $"Ignoring loose external Minecraft " +
                                $"registration: {package.Id.FullName}");
                        }

                        return isOfficial;
                    })
                    .ToList();

                if (packages.Count == 0)
                    return null;

                Package matchingPackage =
                    packages.FirstOrDefault(
                        package =>
                            IsSamePackageVersion(
                                package,
                                v));

                if (requireExactVersion)
                    return matchingPackage;

                return matchingPackage ??
                       packages.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"Failed to resolve installed package for " +
                    $"{v.DisplayName}: {ex}");

                return null;
            }
        }

        private static string GetPackageInstalledLocation(
            Package package)
        {
            try
            {
                return package?
                    .InstalledLocation?
                    .Path ??
                    string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetPackageVersionString(
            Package package)
        {
            try
            {
                var version =
                    package.Id.Version;

                return
                    $"{version.Major}." +
                    $"{version.Minor}." +
                    $"{version.Build}." +
                    $"{version.Revision}";
            }
            catch
            {
                return "unknown";
            }
        }

        private static bool IsOfficialMinecraftPackageLocation(
            string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return false;

            try
            {
                string normalized =
                    Path.GetFullPath(location)
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);

                return
                    normalized.IndexOf(
                        @"\WindowsApps\",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||

                    normalized.IndexOf(
                        @"\XboxGames\",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||

                    normalized.IndexOf(
                        @"\ModifiableWindowsApps\",
                        StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region GDK Resource Validation

        private static string ValidateGdkResourceIdentity(
            MCVersion v,
            IEnumerable<string> urls)
        {
            string[] family =
                Constants.GetPackageFamily(v.Type)
                    .Split('_');

            string lastProblem =
                "no resource was listed.";

            foreach (string url in urls)
            {
                if (!Uri.TryCreate(
                        url,
                        UriKind.Absolute,
                        out Uri uri))
                {
                    lastProblem =
                        $"'{url}' is not a valid URL.";

                    continue;
                }

                string fileName =
                    Path.GetFileName(
                        uri.AbsolutePath);

                string[] parts =
                    Path.GetFileNameWithoutExtension(
                        uri.AbsolutePath)
                    .Split('_');

                // Microsoft.MinecraftUWP_1.26.4005.0_x64__8wekyb3d8bbwe
                //
                // Split:
                // [0] Microsoft.MinecraftUWP
                // [1] 1.26.4005.0
                // [2] x64
                // [3] empty because of "__"
                // [4] 8wekyb3d8bbwe
                if (parts.Length < 5 ||
                    !Version.TryParse(
                        parts[1],
                        out Version resourceVersion))
                {
                    lastProblem =
                        $"'{fileName}' is not a package identity.";

                    continue;
                }

                if (!string.Equals(
                        parts[0],
                        family[0],
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        parts[parts.Length - 1],
                        family[family.Length - 1],
                        StringComparison.OrdinalIgnoreCase))
                {
                    lastProblem =
                        $"'{parts[0]}…{parts[^1]}' is not the " +
                        $"{Constants.GetPackageFamily(v.Type)} family.";

                    continue;
                }

                if (!IsSameVersion(
                        resourceVersion,
                        v))
                {
                    lastProblem =
                        $"it is version {resourceVersion}, " +
                        $"not {v.Name}.";

                    continue;
                }

                if (!VersionDbExtensions.DoesVersionArchMatch(
                        Constants.CurrentArchitecture,
                        parts[2]))
                {
                    lastProblem =
                        $"it is for {parts[2]}, this PC is " +
                        $"{Constants.CurrentArchitecture}.";

                    continue;
                }

                return null;
            }

            return lastProblem;
        }

        #endregion

        #region Version Matching

        private static bool IsSamePackageVersion(
            Package package,
            MCVersion version)
        {
            try
            {
                var packageVersion =
                    package.Id.Version;

                var installedVersion =
                    new Version(
                        packageVersion.Major,
                        packageVersion.Minor,
                        packageVersion.Build,
                        packageVersion.Revision);

                return IsSameVersion(
                    installedVersion,
                    version);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Compares a Windows package identity version with the launcher
        /// GDK version format.
        ///
        /// Examples:
        /// 1.26.4005.0 -> 1.26.40.5
        /// 1.26.0.2    -> 26.0.2
        /// 1.26.2101.0 -> 26.21.1
        /// </summary>
        private static bool IsSameVersion(
            Version installedVersion,
            MCVersion version)
        {
            try
            {
                if (installedVersion == null ||
                    version == null ||
                    string.IsNullOrWhiteSpace(version.Name))
                {
                    return false;
                }

                if (!Version.TryParse(
                        version.Name,
                        out Version expectedVersion))
                {
                    return false;
                }

                if (installedVersion == expectedVersion)
                    return true;

                /*
                 * Launcher 1.x format:
                 *
                 * 1.26.40.5
                 *
                 * Windows:
                 *
                 * 1.26.4005.0
                 *
                 * Build = 40 * 100 + 5 = 4005
                 */
                if (expectedVersion.Major == 1)
                {
                    if (installedVersion.Major != 1 ||
                        installedVersion.Minor != expectedVersion.Minor ||
                        expectedVersion.Build < 0)
                    {
                        return false;
                    }

                    int expectedRevision =
                        Math.Max(
                            expectedVersion.Revision,
                            0);

                    int encodedBuild =
                        expectedVersion.Build * 100 +
                        expectedRevision;

                    if (installedVersion.Build ==
                        encodedBuild)
                    {
                        return true;
                    }

                    return expectedVersion.Revision < 0 &&
                           installedVersion.Build / 100 ==
                           expectedVersion.Build;
                }

                /*
                 * Direct GDK format:
                 *
                 * 26.0.2
                 * -> 1.26.0.2
                 *
                 * 26.10.4
                 * -> 1.26.10.4
                 */
                if (installedVersion.Major != 1 ||
                    installedVersion.Minor != expectedVersion.Major ||
                    expectedVersion.Minor < 0)
                {
                    return false;
                }

                if (installedVersion.Build ==
                    expectedVersion.Minor)
                {
                    if (expectedVersion.Build < 0)
                        return true;

                    return installedVersion.Revision ==
                           expectedVersion.Build;
                }

                /*
                 * Encoded GDK format:
                 *
                 * 26.21.1
                 * -> 1.26.2101.0
                 *
                 * 2101:
                 * 21 = feature
                 * 01 = patch
                 */
                int installedFeature =
                    installedVersion.Build / 100;

                int installedPatch =
                    installedVersion.Build % 100;

                if (installedFeature !=
                    expectedVersion.Minor)
                {
                    return false;
                }

                if (expectedVersion.Build < 0)
                    return true;

                return installedPatch ==
                       expectedVersion.Build;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        // IsPathInside and SafeDeleteFile are defined in the primary PackageHandler partial.
    }
}