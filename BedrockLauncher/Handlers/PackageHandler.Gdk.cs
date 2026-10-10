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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace BedrockLauncher.Handlers
{
    // GDK pipeline. The decisions live in GdkLaunchPipeline; this file adapts it to Windows, the UI and the downloader.
    public partial class PackageHandler
    {
        private static readonly TimeSpan GdkProcessStartTimeout = TimeSpan.FromSeconds(60);

        // How long the game still gets to appear once the launch helper has exited without an error.
        private static readonly TimeSpan GdkHelperExitGrace = TimeSpan.FromSeconds(15);

        private const string GamingServicesFamilyName = "Microsoft.GamingServices_8wekyb3d8bbwe";

        /// <summary>%LOCALAPPDATA%\&lt;this&gt;: save data moved out of a package's app data folder before Windows removed it.</summary>
        internal const string SaveDataBackupFolderName = @"BedrockLauncher\SaveDataBackups";

        private readonly GdkLaunchHelperDeployment GdkLaunchHelper = new GdkLaunchHelperDeployment(
            Path.Combine(AppContext.BaseDirectory, "native", "gamelaunchhelper", GdkLaunchHelperDeployment.HelperLibraryName));

        #region GDK Public Methods

        public async Task PlayGdkPackage(
            MCVersion v,
            string installationDataPath,
            bool keepLauncherOpen,
            bool launchEditor)
        {
            try
            {
                StartTask();

                GdkLaunchRequest request = CreateGdkLaunchRequest(v, launchEditor, installationDataPath);
                var platform = new WindowsGdkPlatform(this, v);
                var pipeline = new GdkLaunchPipeline(platform, LogGdk);

                MainDataModel.Default.ProgressBarState.SetProgressBarText(v.DisplayName);

                InstalledPackageInfo package = await pipeline.LaunchAsync(request);

                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isLaunching);

                Process process;
                using (Process helper = platform.LaunchHelperProcess)
                    process = await WaitForPackageProcess(v, package.FullName, helper, GdkProcessStartTimeout);

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
            MCVersion v,
            string installationDataPath = null)
        {
            try
            {
                StartTask();

                GdkLaunchRequest request = CreateGdkLaunchRequest(v, launchEditor: false, installationDataPath);
                var platform = new WindowsGdkPlatform(this, v);
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

        /// <summary>
        /// Installs the package on the drive the Xbox app installs games to (see <see cref="GetDeploymentVolumes"/>) and,
        /// if Windows rejects it there, on the other package volumes. Gaming Services can fail to stage a GDK package on a
        /// drive it does not use for games (e.g. error 0xD05E0138 on C: when the Xbox app installs games on D:), while
        /// the same package installs on its games drive.
        /// </summary>
        private async Task RunWindowsDeployment(
            string packagePath,
            string preferredVolumeName = null)
        {
            List<PackageVolume> volumes = GetDeploymentVolumes(preferredVolumeName);

            for (int i = 0; i < volumes.Count; i++)
            {
                try
                {
                    await DeployToVolume(packagePath, volumes[i]);
                    return;
                }
                catch (GdkDeploymentRejectedException e) when (
                    i < volumes.Count - 1 &&
                    e.DeploymentHResult != GdkLaunchPipeline.ErrorInstallPackageDowngrade)
                {
                    LogGdk($"Windows rejected the package on {DescribeVolume(volumes[i])}; trying {DescribeVolume(volumes[i + 1])}.");
                }
            }
        }

        /// <summary>
        /// The online package volumes, best first:
        /// 0. the volume the Minecraft package removed for this install was on (where the Xbox app put it);
        /// 1. a drive the Xbox app installs games to that holds installed games;
        /// 2. a drive the Xbox app is set up to install games to;
        /// 3. the others. Ties keep Windows' default package volume first.
        /// A null entry stands for the default volume when the volumes cannot be listed.
        /// </summary>
        private List<PackageVolume> GetDeploymentVolumes(string preferredVolumeName)
        {
            try
            {
                PackageVolume defaultVolume = PM.GetDefaultPackageVolume();

                List<PackageVolume> volumes = PM.FindPackageVolumes()
                    .Where(volume => !volume.IsOffline)
                    .Select(volume => (volume, rank: GetVolumeRank(volume, preferredVolumeName)))
                    .OrderBy(entry => entry.rank)
                    .ThenBy(entry => IsSameVolume(entry.volume, defaultVolume) ? 0 : 1)
                    .Select(entry => entry.volume)
                    .ToList();

                if (volumes.Count > 0)
                {
                    LogGdk("Install volumes, in order: " + string.Join(", ", volumes.Select(volume =>
                        $"{volume.PackageStorePath} (rank {GetVolumeRank(volume, preferredVolumeName)}{(IsSameVolume(volume, defaultVolume) ? ", Windows default" : string.Empty)})")));
                    return volumes;
                }
            }
            catch (Exception ex)
            {
                LogGdk($"Could not list the package volumes; only the default one is used: {ex.Message}");
            }

            return new List<PackageVolume> { null };
        }

        private static int GetVolumeRank(PackageVolume volume, string preferredVolumeName)
        {
            if (preferredVolumeName != null && string.Equals(volume.Name, preferredVolumeName, StringComparison.OrdinalIgnoreCase))
                return 0;

            string gamesFolder = GetXboxGamesFolder(Path.GetPathRoot(volume.PackageStorePath));
            if (gamesFolder == null)
                return 3;

            return HasInstalledGames(gamesFolder) ? 1 : 2;
        }

        private static bool IsSameVolume(PackageVolume a, PackageVolume b) =>
            a != null && b != null && string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The folder the Xbox app installs games to on this drive, or null when the drive is not set up for games.
        /// The Xbox app marks such drives with a hidden ".GamingRoot" file: "RGBX", a 32-bit version, then the games
        /// folder relative to the drive root as a null-terminated UTF-16 string (usually "XboxGames").
        /// </summary>
        private static string GetXboxGamesFolder(string driveRoot)
        {
            try
            {
                if (string.IsNullOrEmpty(driveRoot))
                    return null;

                string marker = Path.Combine(driveRoot, ".GamingRoot");
                if (!File.Exists(marker))
                    return null;

                byte[] bytes = File.ReadAllBytes(marker);
                string folder = bytes.Length > 8 && bytes[0] == 'R' && bytes[1] == 'G' && bytes[2] == 'B' && bytes[3] == 'X'
                    ? System.Text.Encoding.Unicode.GetString(bytes, 8, bytes.Length - 8).TrimEnd('\0')
                    : string.Empty;

                return Path.Combine(driveRoot, string.IsNullOrWhiteSpace(folder) ? "XboxGames" : folder);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                return null;
            }
        }

        /// <summary>Games the Xbox app installed live in "&lt;games folder&gt;\&lt;game&gt;\Content".</summary>
        private static bool HasInstalledGames(string gamesFolder)
        {
            try
            {
                return Directory.Exists(gamesFolder) &&
                       Directory.EnumerateDirectories(gamesFolder)
                           .Any(game => Directory.Exists(Path.Combine(game, "Content")));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>The package volume a package is installed on (for the current user), or null.</summary>
        private string FindPackageVolumeName(string packageFullName)
        {
            try
            {
                PackageVolume defaultVolume = PM.GetDefaultPackageVolume();

                // A package on another drive can also be listed on the system volume (where it is mounted), so a
                // non-default volume that has it wins.
                List<PackageVolume> holders = PM.FindPackageVolumes()
                    .Where(volume => !volume.IsOffline && volume.FindPackageForUser(string.Empty, packageFullName).Any())
                    .ToList();

                PackageVolume holder = holders.FirstOrDefault(volume => !IsSameVolume(volume, defaultVolume)) ?? holders.FirstOrDefault();
                if (holder != null)
                    LogGdk($"{packageFullName} is installed on {holder.PackageStorePath}; the replacement is installed there too.");

                return holder?.Name;
            }
            catch (Exception ex)
            {
                LogGdk($"Could not find the volume of {packageFullName}: {ex.Message}");
                return null;
            }
        }

        private static string DescribeVolume(PackageVolume volume) =>
            volume == null ? "the default package volume" : $"the package volume {volume.PackageStorePath}";

        private async Task DeployToVolume(
            string packagePath,
            PackageVolume volume)
        {
            string fileName =
                Path.GetFileName(packagePath);

            try
            {
                LogGdk($"Requesting Windows to install {fileName} on {DescribeVolume(volume)}");

                MainDataModel.Default.ProgressBarState
                    .SetProgressBarText(fileName);

                MainDataModel.Default.ProgressBarState
                    .SetProgressBarState(
                        LauncherState.isRegisteringPackage);

                var packageUri = new Uri(Path.GetFullPath(packagePath));

                await DeploymentProgressWrapper(volume == null
                    ? PM.AddPackageAsync(
                        packageUri,
                        null,
                        Constants.StorePackageDeploymentOptions)
                    : PM.AddPackageAsync(
                        packageUri,
                        Array.Empty<Uri>(),
                        Constants.StorePackageDeploymentOptions,
                        volume,
                        Array.Empty<string>(),
                        Array.Empty<Uri>()));
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

                if (ex.Data[DeploymentActivityIdKey] is Guid activityId)
                    await LogDeploymentEvents(activityId);

                throw new GdkDeploymentRejectedException(
                    $"Windows rejected the installation of {fileName} on {DescribeVolume(volume)}" +
                    $"{code}: {ex.Message}",
                    hresult,
                    ex);
            }
            finally
            {
                ResetTask();
            }
        }

        /// <summary>
        /// Writes Windows' own deployment log of a failed install to the launcher log. The error text of the deployment
        /// result often only names the failing step; the cause is in these events.
        /// </summary>
        private static async Task LogDeploymentEvents(Guid activityId)
        {
            try
            {
                string events = await PowerShellPackageCommand.RunForOutputAsync(
                    $"Get-AppPackageLog -ActivityID '{activityId}' | ForEach-Object {{ \"$($_.TimeCreated.ToString('HH:mm:ss')) [$($_.Id)] $($_.Message)\" }}",
                    "deployment log");

                LogGdk($"Windows deployment log (ActivityId {activityId}):{Environment.NewLine}{events.Trim()}");
            }
            catch (Exception ex)
            {
                LogGdk($"Could not read the Windows deployment log (ActivityId {activityId}): {ex.Message}");
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

        /// <summary>
        /// Waits for the Minecraft process that runs with exactly the given package identity, or throws
        /// AppLaunchFailedException saying why it did not appear. When the launch helper is known, its exit shortens the
        /// wait: BedrockLauncher-dll exits with the Win32 error when it could not start the game (reported at once), and
        /// with 0 otherwise, after which the game only gets <see cref="GdkHelperExitGrace"/> more to show up (the exit
        /// code cannot always be read, e.g. when the helper was gone before the launcher opened it).
        /// </summary>
        private static async Task<Process> WaitForPackageProcess(MCVersion v, string packageFullName, Process helper, TimeSpan timeout)
        {
            Stopwatch elapsed = Stopwatch.StartNew();
            var reported = new HashSet<int>();
            GdkPackageIdentity.TryParseFullName(packageFullName, out GdkPackageIdentity required);
            string otherPackage = null;
            bool helperExited = false;
            int? helperExitCode = null;
            TimeSpan deadline = timeout;

            while (true)
            {
                foreach (Process candidate in Process.GetProcessesByName(Constants.MINECRAFT_PROCESS_NAME))
                {
                    string candidatePackage = PackageProcessInfo.GetPackageFullName(candidate.Id);

                    if (string.Equals(candidatePackage, packageFullName, StringComparison.OrdinalIgnoreCase))
                        return candidate;

                    if (required != null &&
                        GdkPackageIdentity.TryParseFullName(candidatePackage, out GdkPackageIdentity running) &&
                        running.IsSameFamily(required.FamilyName))
                    {
                        otherPackage = candidatePackage;
                    }

                    if (reported.Add(candidate.Id))
                        LogGdk($"Ignoring {Constants.MINECRAFT_PROCESS_NAME} process {candidate.Id} (package {candidatePackage ?? "none"}); waiting for {packageFullName}.");

                    candidate.Dispose();
                }

                if (elapsed.Elapsed >= deadline)
                    break;

                if (!helperExited && HasExited(helper))
                {
                    helperExited = true;
                    helperExitCode = TryGetExitCode(helper);
                    LogGdk($"Launch helper exited (code {helperExitCode?.ToString() ?? "unknown"}).");

                    // A non-zero code is the helper's own error: the game was not started. (The game is created before
                    // the helper exits, so the scan above already covered it.)
                    if (helperExitCode is int code && code != 0)
                        break;

                    TimeSpan graceEnd = elapsed.Elapsed + GdkHelperExitGrace;
                    if (graceEnd < deadline)
                        deadline = graceEnd;
                    continue;
                }

                await Task.Delay(250);
            }

            if (otherPackage != null)
            {
                LogGdk($"Launch: FAILED — {otherPackage} started instead of {packageFullName}.");
                throw new AppLaunchFailedException(
                    $"Minecraft {v.Name} requires {packageFullName}, but {otherPackage} started instead.",
                    new InvalidOperationException("A different Minecraft package than the validated one is running."));
            }

            if (helperExitCode is int exitCode && exitCode != 0)
            {
                LogGdk($"Launch: FAILED — the launch helper exited with code {exitCode} and no {Constants.MINECRAFT_PROCESS_NAME} process of {packageFullName} is running.");
                var error = new Win32Exception(exitCode);
                throw new AppLaunchFailedException(
                    $"The launch helper could not start Minecraft {v.Name} ({packageFullName}): error {exitCode} ({error.Message}).",
                    error);
            }

            if (helperExited)
            {
                LogGdk($"Launch: FAILED — the launch helper exited and no {Constants.MINECRAFT_PROCESS_NAME} process of {packageFullName} appeared within {GdkHelperExitGrace.TotalSeconds:0}s.");
                throw new AppLaunchFailedException(
                    $"Minecraft {v.Name} ({packageFullName}) did not start, or closed right after it started.",
                    new InvalidOperationException(
                        "The game process exited on its own or never appeared. Start Minecraft once from the Start menu " +
                        "to see the game's own error."));
            }

            LogGdk($"Launch: FAILED — no {Constants.MINECRAFT_PROCESS_NAME} process of {packageFullName} appeared within {timeout.TotalSeconds:0}s.");
            throw new AppLaunchFailedException(
                $"Minecraft {v.Name} ({packageFullName}) did not start within {timeout.TotalSeconds:0} seconds.",
                new TimeoutException("The launch helper did not start the game process."));
        }

        private static bool HasExited(Process process)
        {
            if (process == null)
                return false;

            try
            {
                return process.HasExited;
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
            {
                // Not inspectable: only the game process decides the wait then.
                return false;
            }
        }

        /// <summary>The exit code, or null when Windows does not give it (the process was gone before it was opened).</summary>
        private static int? TryGetExitCode(Process process)
        {
            try
            {
                return process.ExitCode;
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
            {
                return null;
            }
        }

        #endregion

        #region Save Data

        private sealed class SaveDataGuard
        {
            public string PackageDataPath { get; set; }

            public string MojangPath { get; set; }

            public string LinkTarget { get; set; }

            public string MovedBackupPath { get; set; }
        }

        /// <summary>
        /// Before a Windows removal of a Minecraft package: removing the package deletes its app data folder
        /// (%LOCALAPPDATA%\Packages\&lt;family&gt;), so a com.mojang link to an installation is detached first (Windows
        /// must not walk into the linked worlds), and real save data is moved out of that folder.
        /// </summary>
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

                guard.PackageDataPath =
                    Path.Combine(
                        localAppData,
                        "Packages",
                        Constants.GetPackageFamily(type));

                string mojang =
                    Path.Combine(
                        guard.PackageDataPath,
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

                // Outside the package's app data folder (which Windows deletes), on the same drive so it is a move.
                string backup =
                    Path.Combine(
                        localAppData,
                        SaveDataBackupFolderName,
                        $"{Constants.GetPackageFamily(type)}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");

                Directory.CreateDirectory(
                    Path.GetDirectoryName(backup));

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

        /// <summary>
        /// After the removal. When Windows deleted the package's app data folder, nothing is recreated there: the folder
        /// Windows creates when the next package registers carries the package's access rights, while one created here
        /// would not, and the game could not open its data ("Storage is full"). The installation's data folder is linked
        /// again on install / launch, and moved real data stays in the backup (found by "Backup Save Data").
        /// </summary>
        private void RestoreSaveData(
            SaveDataGuard guard)
        {
            if (guard == null ||
                string.IsNullOrEmpty(guard.MojangPath))
            {
                return;
            }

            if (!string.IsNullOrEmpty(guard.LinkTarget))
            {
                // Not reattached: the link belongs to whichever installation was played last, and every UWP install /
                // launch links the installation being played (RedirectSaveData). Reattaching the old one here only
                // pointed the game at the wrong installation until then.
                Trace.WriteLine(
                    $"Save-data link to {guard.LinkTarget} left detached; the next UWP install / launch links the " +
                    "installation being played.");
                return;
            }

            if (string.IsNullOrEmpty(guard.MovedBackupPath))
            {
                return;
            }

            try
            {
                if (!Directory.Exists(guard.PackageDataPath))
                {
                    Trace.WriteLine(
                        $"The package data folder was removed with the package; the save data stays at " +
                        $"{guard.MovedBackupPath} (Settings > Backup Save Data imports it as an installation).");
                    return;
                }

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

                if (Directory.Exists(
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
            private readonly MCVersion version;

            // The package volume of the Xbox app / Store package removed to make room for this install, if any.
            private string preferredVolumeName;

            public WindowsGdkPlatform(PackageHandler handler, MCVersion version)
            {
                this.handler = handler;
                this.version = version;
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

                // Windows hands MSIXVC packages to Gaming Services; without it every install is rejected, so this is
                // checked before downloading the package.
                if (!handler.PM.FindPackagesForUser(string.Empty, GamingServicesFamilyName).Any())
                {
                    throw new GdkBootstrapException(
                        "Gaming Services is not installed, and Windows needs it to install GDK versions of Minecraft. " +
                        "Install \"Gaming Services\" from the Microsoft Store (or open the Xbox app, which installs it), then try again.");
                }

                string packagePath =
                    await handler.EnsureMsixvcDownloaded(
                        version,
                        request.RequiredPackage);

                try
                {
                    await handler.RunWindowsDeployment(
                        packagePath,
                        preferredVolumeName);
                }
                catch (GdkDeploymentRejectedException e) when (e.DeploymentHResult != GdkLaunchPipeline.ErrorInstallPackageDowngrade)
                {
                    // A damaged download would be rejected on every retry; removing it makes the next install download
                    // it again. (A downgrade refusal says nothing about the file, so it is kept for that case.)
                    LogGdk($"Removing {packagePath} after Windows rejected it, so the next install downloads it again.");
                    handler.SafeDeleteFile(packagePath);
                    version.UpdateFolderSize();
                    throw;
                }

                // "Keep Appx Package": keep the package in the version folder so switching back to this version
                // reinstalls it without downloading it again. Otherwise it is removed once Windows has installed it.
                if (!Properties.LauncherSettings.Default.KeepAppx)
                {
                    handler.SafeDeleteFile(packagePath);
                    version.UpdateFolderSize();
                }
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
                    // Installed by the Xbox app / Store: its drive is where the replacement goes.
                    preferredVolumeName = handler.FindPackageVolumeName(package.FullName);
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

                LaunchHelperProcess = GdkRegistration.Activate(
                    package.FamilyName,
                    GdkRegistration.GetApplicationId(package.InstallLocation));

                LogGdk(LaunchHelperProcess != null
                    ? $"Launch helper started (process {LaunchHelperProcess.Id})"
                    : "Launch helper started and already exited");
                return true;
            }

            /// <summary>The GameLaunchHelper.exe Windows started, when the launch went through package activation.</summary>
            public Process LaunchHelperProcess { get; private set; }
        }

        #endregion
    }
}
