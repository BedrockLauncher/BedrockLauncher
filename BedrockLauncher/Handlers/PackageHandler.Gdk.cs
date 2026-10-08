using BedrockLauncher.Classes;
using BedrockLauncher.Enums;
using BedrockLauncher.Exceptions;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.ViewModels;
using JemExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.ApplicationModel;

namespace BedrockLauncher.Handlers
{
    // GDK pipeline. The decisions live in GdkLaunchPipeline; this file adapts it to Windows, the UI and the downloader.
    public partial class PackageHandler
    {
        private static readonly TimeSpan GdkProcessStartTimeout = TimeSpan.FromSeconds(60);

        private readonly GdkLaunchHelperDeployment GdkLaunchHelper = new GdkLaunchHelperDeployment(
            Path.Combine(AppContext.BaseDirectory, "native", "gamelaunchhelper", GdkLaunchHelperDeployment.HelperLibraryName));

        #region GDK Public Methods

        public async Task PlayGdkPackage(
            BLProfile profile,
            MCVersion v,
            string installationDataPath,
            bool keepLauncherOpen,
            bool launchEditor)
        {
            try
            {
                StartTask();

                GdkLaunchRequest request = CreateGdkLaunchRequest(v, launchEditor, installationDataPath);
                var pipeline = new GdkLaunchPipeline(new WindowsGdkPlatform(this, profile, v), LogGdk);

                MainDataModel.Default.ProgressBarState.SetProgressBarText(v.DisplayName);

                InstalledPackageInfo package = await pipeline.LaunchAsync(request);

                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isLaunching);

                Process process = await WaitForPackageProcess(package.FullName, GdkProcessStartTimeout);
                if (process == null)
                {
                    LogGdk($"Launch: FAILED — no {Constants.MINECRAFT_PROCESS_NAME} process of {package.FullName} appeared within {GdkProcessStartTimeout.TotalSeconds:0}s.");
                    throw new AppLaunchFailedException(
                        $"Minecraft {v.Name} ({package.FullName}) did not start within {GdkProcessStartTimeout.TotalSeconds:0} seconds.",
                        new TimeoutException("The launch helper did not start the game process."));
                }

                LogGdk($"Launch: OK (process {process.Id}, package {package.FullName})");

                if (keepLauncherOpen)
                {
                    AttachGameProcess(process);
                    EndTask();
                }
                else
                {
                    process.Dispose();
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

        /// <summary>
        /// Install / Repair: makes the exact required package ready to launch, without launching it. When an installation
        /// is given, its data folder is linked too (like the UWP install does).
        /// </summary>
        public async Task InstallGdkPackage(
            BLProfile profile,
            MCVersion v,
            string installationDataPath = null)
        {
            try
            {
                StartTask();

                GdkLaunchRequest request = CreateGdkLaunchRequest(v, launchEditor: false, installationDataPath);
                var platform = new WindowsGdkPlatform(this, profile, v);
                var pipeline = new GdkLaunchPipeline(platform, LogGdk);

                await pipeline.EnsureReadyAsync(request);

                if (!string.IsNullOrWhiteSpace(installationDataPath))
                    platform.PrepareSaveData(request);
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
                    new AppInstallFailedException(e));
            }
            finally
            {
                EndTask();
            }
        }

        #endregion

        #region GDK Pipeline Helpers

        private static GdkLaunchRequest CreateGdkLaunchRequest(MCVersion v, bool launchEditor, string installationDataPath)
        {
            return new GdkLaunchRequest
            {
                MinecraftVersion = v.Name,
                VersionUuid = v.UUID,
                VersionType = v.Type,
                PackageType = v.PackageType,
                RequiredPackage = v.RequiredGdkPackage,
                LaunchEditor = launchEditor,
                InstallationDataPath = installationDataPath
            };
        }

        private static bool IsMinecraftFamilyRunning(string packageFamilyName)
        {
            foreach (Process candidate in Process.GetProcessesByName(Constants.MINECRAFT_PROCESS_NAME))
            {
                using (candidate)
                {
                    string packageFullName = PackageProcessInfo.GetPackageFullName(candidate.Id);
                    if (GdkPackageIdentity.TryParseFullName(packageFullName, out GdkPackageIdentity running) &&
                        running.IsSameFamily(packageFamilyName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void LogGdk(string message)
        {
            Trace.WriteLine("[GDK] " + message);
        }

        private static InstalledPackageInfo ToInstalledPackageInfo(Package package)
        {
            PackageId id = package.Id;
            PackageVersion version = id.Version;

            return new InstalledPackageInfo
            {
                FullName = id.FullName,
                Name = id.Name,
                Version = new Version(version.Major, version.Minor, version.Build, version.Revision),
                Architecture = id.Architecture.ToString().ToLowerInvariant(),
                PublisherId = id.PublisherId,
                InstallLocation = GetPackageInstalledLocation(package),
                IsDevelopmentMode = package.IsDevelopmentMode
            };
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
            catch (FileNotFoundException)
            {
                // The registration exists but its location is gone (e.g. a deleted loose folder).
                return string.Empty;
            }
        }

        private async Task<string> EnsureMsixvcDownloaded(
            MCVersion v,
            GdkPackageIdentity required)
        {
            // The package lives in the version folder and is named after the exact package identity, so it can only
            // ever be used for this version, and switching back to the version does not download it again.
            EnsureSafeLauncherVersionDirectory(v.GameDirectory);
            Directory.CreateDirectory(v.GameDirectory);

            string packagePath = v.GdkPackageFilePath;

            AdoptCachedMsixvc(v, required, packagePath);

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
                    MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isDownloading);

                    await VersionDownloader.DownloadGdkPackage(
                        v,
                        packagePath,
                        (x, y) => ProgressWrapper(x, y),
                        CancelSource.Token);
                }
                catch (OperationCanceledException e)
                {
                    throw new PackageDownloadCanceledException(e);
                }
                catch (Exception e) when (e is not PackageManagerException)
                {
                    throw new PackageDownloadFailedException(e);
                }
                finally
                {
                    SetCancelation(false);
                    ResetTask();
                }
            }
            else
            {
                LogGdk($"Reusing cached package file: {packagePath}");
            }

            if (!File.Exists(packagePath))
            {
                throw new PackageDownloadFailedException(
                    $"The package download for {required.FullName} did not create a file.",
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
                    $"The CDN did not return a package for {required.FullName}: " +
                    downloadProblem,
                    new InvalidDataException(downloadProblem));
            }

            return packagePath;
        }

        /// <summary>
        /// Moves a package earlier launcher builds left in versions\AppxBackups (named after the identity, or the older
        /// Minecraft-&lt;version&gt;.msixvc) into the version folder, so it is not downloaded again.
        /// </summary>
        private void AdoptCachedMsixvc(MCVersion v, GdkPackageIdentity required, string packagePath)
        {
            if (File.Exists(packagePath))
                return;

            string backups = GetPackageCacheDirectory(v);
            string[] candidates =
            {
                Path.Combine(backups, required.FullName + GdkPackageIdentity.MsixvcExtension),
                Path.Combine(backups, GetMinecraftPackageFileName(v, GdkPackageIdentity.MsixvcExtension))
            };

            foreach (string candidate in candidates)
            {
                if (!File.Exists(candidate))
                    continue;

                try
                {
                    File.Move(candidate, packagePath);
                    LogGdk($"Moved cached package {candidate} to {packagePath}; Windows validates its identity on install.");
                    return;
                }
                catch (IOException ex)
                {
                    LogGdk($"Could not move cached package {candidate}: {ex.Message}");
                }
            }
        }

        private async Task RunWindowsDeployment(
            string packagePath)
        {
            string fileName =
                Path.GetFileName(packagePath);

            try
            {
                LogGdk($"Requesting Windows to install {fileName}");

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

        /// <summary>Removes a package installed by Windows (not a launcher loose registration).</summary>
        private async Task RemoveWindowsPackage(
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
                Trace.WriteLine($"WinRT removal of {packageFullName} failed, trying PowerShell: {winrtError}");

                string command =
                    $"Remove-AppxPackage -Package " +
                    $"'{PowerShellPackageCommand.Escape(packageFullName)}'";

                await PowerShellPackageCommand.RunAsync(
                    command,
                    "package removal");
            }
        }

        /// <summary>Waits for the Minecraft process that runs with exactly the given package identity.</summary>
        private static async Task<Process> WaitForPackageProcess(string packageFullName, TimeSpan timeout)
        {
            Stopwatch elapsed = Stopwatch.StartNew();
            var reported = new HashSet<int>();

            while (elapsed.Elapsed < timeout)
            {
                foreach (Process candidate in Process.GetProcessesByName(Constants.MINECRAFT_PROCESS_NAME))
                {
                    string candidatePackage = PackageProcessInfo.GetPackageFullName(candidate.Id);

                    if (string.Equals(candidatePackage, packageFullName, StringComparison.OrdinalIgnoreCase))
                        return candidate;

                    if (reported.Add(candidate.Id))
                        LogGdk($"Ignoring {Constants.MINECRAFT_PROCESS_NAME} process {candidate.Id} (package {candidatePackage ?? "none"}); waiting for {packageFullName}.");

                    candidate.Dispose();
                }

                await Task.Delay(500);
            }

            return null;
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
                    $"Could not restore save data after removal: {ex}");
            }
        }

        #endregion

        #region Windows Platform Adapter

        /// <summary>Windows / UI / downloader implementation of the GDK pipeline operations for one version.</summary>
        private sealed class WindowsGdkPlatform : IGdkPlatform
        {
            private readonly PackageHandler handler;
            private readonly BLProfile profile;
            private readonly MCVersion version;

            public WindowsGdkPlatform(PackageHandler handler, BLProfile profile, MCVersion version)
            {
                this.handler = handler;
                this.profile = profile;
                this.version = version;
            }

            public async Task VerifyEntitlementAsync(GdkLaunchRequest request)
            {
                GdkEntitlementResult entitlement =
                    await GdkEntitlementService.VerifyAsync(
                        profile,
                        request.VersionType);

                if (entitlement == null ||
                    !entitlement.IsEntitled)
                {
                    throw new GdkEntitlementException(
                        entitlement?.Message ??
                        "You are not entitled to use this Minecraft version.");
                }
            }

            public IReadOnlyList<InstalledPackageInfo> GetInstalledPackages(string packageFamilyName)
            {
                return handler.PM
                    .FindPackagesForUser(string.Empty, packageFamilyName)
                    .Select(ToInstalledPackageInfo)
                    .ToList();
            }

            public bool IsLauncherOwnedLocation(string location)
            {
                return IsPathInside(MainDataModel.Default.FilePaths.VersionsFolder, location);
            }

            public async Task InstallExactAsync(GdkLaunchRequest request)
            {
                if (!File.Exists(version.GdkPackageFilePath) &&
                    !handler.VersionDownloader.HasGdkDownloadResource(version))
                {
                    throw new GdkVersionUnavailableException(
                        $"{request.RequiredPackage.FullName} is not installed, it is not downloaded in the version folder, " +
                        $"and the catalog lists no download resource for exactly that package (Minecraft {request.MinecraftVersion}).");
                }

                // The package stays in the version folder after installing: it is this version's local copy.
                string packagePath =
                    await handler.EnsureMsixvcDownloaded(
                        version,
                        request.RequiredPackage);

                await handler.RunWindowsDeployment(
                    packagePath);
            }

            public async Task<bool> ConfirmReplaceAsync(GdkLaunchRequest request, GdkInstallEvaluation evaluation)
            {
                string reason = evaluation.Status == GdkInstallStatus.WrongArchitecture
                    ? "is installed for a different architecture"
                    : "is a newer version (for example installed by a Microsoft Store update)";

                var answer =
                    await DialogPrompt.ShowDialog_YesNo(
                        "Switch Minecraft version", //TODO: Localize String
                        $"Minecraft {request.MinecraftVersion} requires {request.RequiredPackage.FullName}, but " +
                        $"{evaluation.Installed.FullName} {reason}. Windows keeps one Minecraft package installed at a " +
                        "time and does not install an older version over a newer one, so the installed package has to " +
                        "be removed first. Your worlds and settings are kept. Continue?");

                return answer == System.Windows.Forms.DialogResult.Yes;
            }

            public async Task RemovePackageAsync(InstalledPackageInfo package, bool launcherRegistration)
            {
                if (!launcherRegistration)
                {
                    await handler.RemoveOccupyingPackageAsync(package.FullName, package.FamilyName, version.Type);
                    return;
                }

                MainDataModel.Default.ProgressBarState.SetProgressBarText(package.FullName);
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isRemovingPackage);

                try
                {
                    await GdkRegistration.UnregisterAsync(package.FullName);
                }
                finally
                {
                    handler.ResetTask();
                }

                handler.EnsurePackageRemoved(package.FullName, package.FamilyName);
                LogGdk($"Removed launcher registration {package.FullName}");
            }

            public void PrepareBootstrap(GdkLaunchRequest request, InstalledPackageInfo package)
            {
                string applicationId = GdkRegistration.GetApplicationId(package.InstallLocation);

                GdkLaunchHelperState state = handler.GdkLaunchHelper.EnsureDeployed(
                    package.InstallLocation,
                    package.Architecture,
                    applicationId);

                LogGdk($"Launch helper: BedrockLauncher-dll {state} in {package.InstallLocation} (sha256 {handler.GdkLaunchHelper.SourceSha256})");
            }

            public void RecordInstall(GdkLaunchRequest request, InstalledPackageInfo package)
            {
                try
                {
                    EnsureSafeLauncherVersionDirectory(version.GameDirectory);

                    GdkInstallRecord.Write(version.GameDirectory, new GdkInstallRecord.Data
                    {
                        MinecraftVersion = request.MinecraftVersion,
                        VersionUuid = request.VersionUuid,
                        VersionType = request.VersionType.ToString(),
                        RequiredPackageFullName = request.RequiredPackage.FullName,
                        InstalledPackageFullName = package.FullName,
                        PackageFamilyName = package.FamilyName,
                        Architecture = package.Architecture,
                        InstallLocation = package.InstallLocation,
                        LaunchHelperSha256 = handler.GdkLaunchHelper.SourceSha256,
                        ValidatedUtc = DateTime.UtcNow.ToString("o")
                    });

                    version.UpdateFolderSize();
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
                {
                    // The record is an audit of the validated install; the launch does not depend on it.
                    LogGdk($"Could not write the install record for {request.MinecraftVersion}: {ex.Message}");
                }
            }

            public void PrepareSaveData(GdkLaunchRequest request)
            {
                string gameDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    MinecraftPackageFamilies.GetGdkDataFolderName(request.VersionType));

                // A running game has its data folder open; the launch only brings that instance to the front.
                if (IsMinecraftFamilyRunning(request.RequiredPackage.FamilyName))
                {
                    LogGdk($"Save data: Minecraft is already running, '{gameDataPath}' is left as is.");
                    return;
                }

                var redirector = new GdkSaveDataRedirector((link, target) =>
                    SymLinkHelper.CreateSymbolicLinkSafe(link, target, SymLinkHelper.SymbolicLinkType.Directory));

                try
                {
                    var (state, backupPath) = redirector.Redirect(gameDataPath, request.InstallationDataPath);

                    LogGdk($"Save data: {gameDataPath} -> {request.InstallationDataPath} ({state})");
                    if (backupPath != null)
                        LogGdk($"Save data: the existing data folder was moved to {backupPath}");
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    throw new SaveRedirectionFailedException(ex);
                }
            }

            public async Task<bool> ActivateAsync(GdkLaunchRequest request, InstalledPackageInfo package)
            {
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isLaunching);

                if (request.LaunchEditor)
                {
                    return await handler.TryLaunchUriInPackage(
                        Constants.GetEditorUri(request.VersionType),
                        package.FamilyName);
                }

                return GdkRegistration.Activate(
                    package.FamilyName,
                    GdkRegistration.GetApplicationId(package.InstallLocation));
            }
        }

        #endregion
    }
}
