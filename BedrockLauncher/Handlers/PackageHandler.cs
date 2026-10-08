using BedrockLauncher.Classes;
using BedrockLauncher.Downloaders;
using JemExtensions;
using SymbolicLinkSupport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Deployment;
using Windows.System;
using ZipProgress = JemExtensions.ZipFileExtensions.ZipProgress;
using BedrockLauncher.Enums;
using System.Windows.Input;
using BedrockLauncher.ViewModels;
using BedrockLauncher.Exceptions;
using BedrockLauncher.UpdateProcessor;
using BedrockLauncher.UpdateProcessor.Authentication;
using BedrockLauncher.UpdateProcessor.Handlers;
using BedrockLauncher.Classes.Launcher;
using Windows.System.Diagnostics;
using BedrockLauncher.UpdateProcessor.Enums;
using JemExtensions.WPF.Commands;
using BedrockLauncher.UI.Pages.Common;
using System.Collections;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Extensions;
using Newtonsoft.Json;

namespace BedrockLauncher.Handlers
{
    // UWP pipeline lives in this file, GDK pipeline in PackageHandler.Gdk.cs.
    public partial class PackageHandler : IDisposable
    {
        private CancellationTokenSource CancelSource = new CancellationTokenSource();
        private StoreNetwork StoreNetwork = new StoreNetwork();
        private PackageManager PM = new PackageManager();
        private static readonly string[] MinecraftPackageExtensions = [".Appx", ".appx", ".msix", ".msixbundle", ".appxbundle"];

        public VersionDownloader VersionDownloader { get; private set; } = new VersionDownloader();
        public Process GameHandle { get; private set; } = null;
        public bool isGameRunning { get => GameHandle != null; }

        private enum PackagePayloadKind
        {
            Invalid,
            ZipAppx,
            StorePackage
        }

        private sealed class ResolvedPackageFile
        {
            public string DownloadPath { get; set; }
            public string BackupPath { get; set; }
            public string PackagePath { get; set; }
        }

        #region Public Methods

        /// <summary>
        /// Entry point of the Play button: the persisted package type of the version selects its pipeline, and the
        /// two pipelines never fall back into each other.
        /// UWP: download → extract → register → validate registration → launch.
        /// GDK: required package → entitlement → exact install → validation → launch helper → launch (PackageHandler.Gdk.cs).
        /// </summary>
        public async Task Play(BLProfile profile, MCVersion v, string dirPath, bool keepLauncherOpen, bool launchEditor)
        {
            switch (v.PackageType)
            {
                case PackageType.GDK:
                    await PlayGdkPackage(profile, v, keepLauncherOpen, launchEditor);
                    break;

                case PackageType.UWP:
                    if (await InstallPackage(v, dirPath))
                        await LaunchPackage(v, dirPath, keepLauncherOpen, launchEditor);
                    break;

                default:
                    SetException(new AppLaunchFailedException(
                        new InvalidOperationException($"Minecraft {v.Name} has an unknown package type ({v.PackageType}).")));
                    break;
            }
        }

        /// <summary>UWP launch. The registered package of the family must be the selected version's loose registration.</summary>
        public async Task LaunchPackage(MCVersion v, string dirPath, bool KeepLauncherOpen, bool LaunchEditor)
        {
            try
            {
                StartTask();
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isLaunching);

                ValidateUwpRegistration(v);

                if (!LaunchEditor && await TryLaunchPackageActivation(v, KeepLauncherOpen))
                {
                    Trace.WriteLine("App launch finished through package activation fallback.");
                    if (!KeepLauncherOpen)
                        await Application.Current.Dispatcher.InvokeAsync(() => Application.Current.MainWindow.Close());

                    return;
                }

                Uri launchUri = new Uri($"{Constants.GetUri(v.Type)}:?Editor={LaunchEditor}");
                if (await TryLaunchSupportedUri(launchUri))
                {
                    Trace.WriteLine("App launch finished!");
                    if (!KeepLauncherOpen)
                        await Application.Current.Dispatcher.InvokeAsync(() => Application.Current.MainWindow.Close());
                    else
                        await GetGameHandle(Constants.MINECRAFT_PROCESS_NAME);
                }
                else if (!LaunchEditor)
                {
                    SetException(new AppLaunchFailedException(
                        $"Could not launch Minecraft: no installed package or supported {Constants.GetUri(v.Type)} URI was found.",
                        new Exception("The selected Minecraft package could not be activated and the URI protocol is not registered.")));
                }
                else
                {
                    SetException(new AppLaunchFailedException($"Impossible to launch Editor: Failed to open {Constants.GetUri(v.Type)} URI", new Exception()));
                }
            }
            catch (AppLaunchFailedException e)
            {
                EndTask();
                SetException(e);
            }
            catch (Exception e)
            {
                EndTask();
                SetException(new AppLaunchFailedException(e));
            }
        }

        private async Task<bool> TryLaunchSupportedUri(Uri launchUri)
        {
            try
            {
                LaunchQuerySupportStatus status = await Launcher.QueryUriSupportAsync(launchUri, LaunchQuerySupportType.Uri);
                if (status != LaunchQuerySupportStatus.Available)
                {
                    Trace.WriteLine($"Skipping unsupported URI launch for {launchUri.Scheme}: query support returned {status}.");
                    return false;
                }

                return await Launcher.LaunchUriAsync(launchUri);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"URI launch failed without showing the Windows protocol prompt: {ex}");
                return false;
            }
        }

        /// <summary>
        /// UWP validation: the package Windows has registered for the version's family must be the loose registration
        /// of this version's folder, with the version its manifest declares. Anything else (another launcher version,
        /// a Store install or a GDK package of the same family) blocks the launch instead of being started by mistake.
        /// </summary>
        private void ValidateUwpRegistration(MCVersion v)
        {
            if (v.PackageType != PackageType.UWP)
                throw new InvalidOperationException($"Minecraft {v.Name} is not a UWP version.");

            string family = Constants.GetPackageFamily(v.Type);
            var (_, manifestVersion, _) = MCVersionExtensions.GetCommonPackageValues(v.ManifestPath);

            List<Package> registered = PM.FindPackagesForUser(string.Empty, family).ToList();

            Package match = registered.FirstOrDefault(package =>
            {
                string location;
                try { location = package.InstalledLocation.Path; }
                catch (FileNotFoundException) { location = string.Empty; }

                PackageVersion version = package.Id.Version;
                string installedVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

                return PackageRegistrationMatcher.MatchesRegistration(
                        family,
                        package.Id.FamilyName,
                        sameInstallDirectory: PathsEqual(location, v.GameDirectory),
                        signedPackageRegistration: false,
                        expectedVersion: manifestVersion,
                        installedVersion: installedVersion) &&
                    PackageRegistrationMatcher.SameVersion(manifestVersion, installedVersion);
            });

            if (match == null)
            {
                string found = registered.Count == 0
                    ? "nothing"
                    : string.Join(", ", registered.Select(package => package.Id.FullName));

                throw new AppLaunchFailedException(
                    $"UWP validation failed for Minecraft {v.Name}: Windows has {found} registered for {family}, " +
                    $"not the package in {v.GameDirectory}.",
                    new InvalidOperationException("The selected UWP version is not the registered package."));
            }

            Trace.WriteLine($"UWP validation: OK ({match.Id.FullName} at {v.GameDirectory})");
        }

        private async Task<bool> TryLaunchPackageActivation(MCVersion v, bool keepLauncherOpen)
        {
            try
            {
                Trace.WriteLine($"Attempting direct package activation for {v.DisplayName}");
                var pkg = await AppDiagnosticInfo.RequestInfoForPackageAsync(Constants.GetPackageFamily(v.Type));
                if (pkg.Count == 0)
                {
                    Trace.WriteLine($"No package diagnostic entry found for {Constants.GetPackageFamily(v.Type)}");
                    return false;
                }

                AppActivationResult activationResult = await pkg[0].LaunchAsync();
                if (activationResult == null)
                {
                    Trace.WriteLine("Direct package activation returned no result.");
                    return false;
                }

                if (keepLauncherOpen)
                    await GetGameHandle(Constants.MINECRAFT_PROCESS_NAME);

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Direct package activation failed: {ex}");
                return false;
            }
        }


        /// <summary>UWP install: download → extract → register. GDK versions never come through here (see InstallGdkPackage).</summary>
        /// <returns>True when the version is registered and ready to launch.</returns>
        public async Task<bool> InstallPackage(MCVersion v, string dirPath)
        {
            try
            {
                StartTask();

                if (v.PackageType != PackageType.UWP)
                    throw new InvalidOperationException($"Minecraft {v.Name} is a {v.PackageType} version and cannot use the UWP pipeline.");

                if (!v.IsInstalled)
                {
                    List<VersionInfoJson> versions = VersionManager.Singleton.GetVersions();
                    if (versions.Any(ver => v.UUID.CompareTo(ver.uuid.ToString()) == 0))
                    {
                        await DownloadAndExtractPackage(v);
                    }
                    else
                    {
                        throw new NoVersionAccessibleException();
                    }
                }

                ValidateUwpManifestIdentity(v);

                await UnregisterPackage(v, true);
                await EnsureUwpFamilyAvailable(v);
                await RegisterPackage(v);

                await RedirectSaveData(dirPath, v.Type);
                return true;
            }
            catch (PackageManagerException e)
            {
                SetException(e);
            }
            catch (NoVersionAccessibleException e)
            {
                SetException(e);
            }
            catch (Exception e)
            {
                SetException(new AppInstallFailedException(e));
            }
            finally
            {
                EndTask();
            }

            return false;
        }

        /// <summary>The extracted manifest must be the package of the version's own family (release vs preview).</summary>
        private static void ValidateUwpManifestIdentity(MCVersion v)
        {
            var (name, _, _) = MCVersionExtensions.GetCommonPackageValues(v.ManifestPath);
            string expected = MinecraftPackageFamilies.GetIdentityName(v.Type);

            if (!string.Equals(name, expected, StringComparison.OrdinalIgnoreCase))
            {
                throw new PackageRegistrationFailedException(new InvalidDataException(
                    $"The manifest in {v.GameDirectory} identifies '{name}', but Minecraft {v.Name} ({v.Type}) is {expected}."));
            }
        }

        /// <summary>
        /// Windows keeps one package per family per user. After the launcher removed its own registrations, a package
        /// left in the family (a GDK install or a Store install outside the launcher folder) prevents the loose
        /// registration. It is removed only after the user confirms; otherwise the install stops with an explicit error.
        /// </summary>
        private async Task EnsureUwpFamilyAvailable(MCVersion v)
        {
            string family = Constants.GetPackageFamily(v.Type);

            foreach (Package package in PM.FindPackagesForUser(string.Empty, family).ToList())
            {
                string location;
                try { location = package.InstalledLocation.Path; }
                catch (FileNotFoundException) { location = string.Empty; }

                if (PathsEqual(location, v.GameDirectory))
                    continue;

                string fullName = package.Id.FullName;

                var answer = await DialogPrompt.ShowDialog_YesNo(
                    "Switch Minecraft version", //TODO: Localize String
                    $"Minecraft {v.Name} (UWP) needs the {family} package slot, but {fullName} is installed there " +
                    $"('{location}'). Windows keeps one Minecraft package installed at a time, so it has to be removed first. " +
                    "Your worlds and settings are kept, and the launcher reinstalls it when you play that version again. Continue?");

                if (answer != System.Windows.Forms.DialogResult.Yes)
                {
                    throw new PackageRegistrationFailedException(new InvalidOperationException(
                        $"{fullName} (at '{location}') is installed for {family}. Windows keeps one package per family, " +
                        $"so Minecraft {v.Name} (UWP) cannot be registered while it is installed. The package was left untouched."));
                }

                Trace.WriteLine($"Removing {fullName} so Minecraft {v.Name} (UWP) can be registered (confirmed).");
                await RemoveOccupyingPackageAsync(fullName, family, v.Type);
            }
        }

        /// <summary>
        /// Removes a package installed by Windows (GDK or Store) that occupies a family another version needs. Callers
        /// must have the user's confirmation. Save data in the package's LocalState is moved aside and restored, the
        /// removal is verified, and GDK install records of the removed package are cleared so no version is shown as
        /// installed when it is not.
        /// </summary>
        private async Task RemoveOccupyingPackageAsync(string packageFullName, string familyName, VersionType type)
        {
            MainDataModel.Default.ProgressBarState.SetProgressBarText(packageFullName);
            MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isRemovingPackage);

            SaveDataGuard guard = ProtectSaveData(type);
            try
            {
                await RemoveWindowsPackage(packageFullName);
            }
            finally
            {
                RestoreSaveData(guard);
                ResetTask();
            }

            EnsurePackageRemoved(packageFullName, familyName);
            ClearGdkInstallRecords(packageFullName);

            Trace.WriteLine($"Removed {packageFullName}");
        }

        private void EnsurePackageRemoved(string packageFullName, string familyName)
        {
            if (PM.FindPackagesForUser(string.Empty, familyName).Any(package =>
                    string.Equals(package.Id.FullName, packageFullName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new PackageRemovalFailedException(
                    new InvalidOperationException($"Windows still reports {packageFullName} as installed after removal."));
            }
        }

        /// <summary>Deletes the install record of every GDK version whose required package was just removed.</summary>
        private static void ClearGdkInstallRecords(string packageFullName)
        {
            foreach (MCVersion version in MainDataModel.Default.Versions.ToList())
            {
                if (version.PackageType != PackageType.GDK ||
                    !string.Equals(version.RequiredGdkPackage?.FullName, packageFullName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string record = Path.Combine(version.GameDirectory, GdkInstallRecord.FileName);
                try
                {
                    if (File.Exists(record))
                        File.Delete(record);

                    version.UpdateFolderSize();
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    Trace.WriteLine($"Could not clear the install record {record}: {ex.Message}");
                }
            }
        }

        public async Task ClosePackage()
        {
            if (GameHandle != null)
            {
                string title = BedrockLauncher.Localization.Language.LanguageManager.GetResource("Dialog_KillGame_Title") as string;
                string content = BedrockLauncher.Localization.Language.LanguageManager.GetResource("Dialog_KillGame_Text") as string;
                var result = await DialogPrompt.ShowDialog_YesNo(title, content);

                if (result == System.Windows.Forms.DialogResult.Yes) GameHandle.Kill();
            }
        }
        public async Task RemovePackage(MCVersion v)
        {
            try
            {
                StartTask();

                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isUninstalling);
                await UnregisterPackage(v, false, true);
                EnsureSafeLauncherVersionDirectory(v.GameDirectory);
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isUninstalling);
                await DirectoryExtensions.DeleteAsync(v.GameDirectory, (x, y, phase) => ProgressWrapper(x, y, phase), "Files", "Folders");
                if (Directory.Exists(v.GameDirectory)) Directory.Delete(v.GameDirectory, true);
                v.UpdateFolderSize();
                await Task.Run(Program.OnApplicationRefresh);
                foreach (var ver in MainDataModel.Default.Versions) ver.UpdateFolderSize();
            }
            catch (PackageManagerException e)
            {
                SetException(e);
            }
            catch (Exception ex)
            {
                SetException(new PackageRemovalFailedException(ex));
            }
            finally
            {
                EndTask();
            }
        }
        public async Task AddPackage(string packagePath)
        {
            try
            {
                if (!File.Exists(packagePath)) return;
                StartTask();
                var outputDirectoryName = FileExtensions.GetAvaliableFileName(Path.GetFileNameWithoutExtension(packagePath), MainDataModel.Default.FilePaths.VersionsFolder);
                var outputDirectoryPath = Path.Combine(MainDataModel.Default.FilePaths.VersionsFolder, outputDirectoryName);
                Trace.WriteLine("Extraction started");
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isExtracting);
                if (Directory.Exists(outputDirectoryPath)) Directory.Delete(outputDirectoryPath, true);
                var fileStream = File.OpenRead(packagePath);
                var progress = new Progress<ZipProgress>();
                progress.ProgressChanged += (s, z) => MainDataModel.Default.ProgressBarState.SetProgressBarProgress(currentProgress: z.Processed, totalProgress: z.Total);
                await Task.Run(() => new ZipArchive(fileStream).ExtractToDirectory(outputDirectoryPath, progress, CancelSource));
                fileStream.Close();
                File.Delete(Path.Combine(outputDirectoryPath, "AppxSignature.p7x"));
                string backupDirectory = Path.Combine(MainDataModel.Default.FilePaths.VersionsFolder, "AppxBackups");
                Directory.CreateDirectory(backupDirectory);
                File.Move(packagePath, Path.Combine(backupDirectory, Path.GetFileName(packagePath)));
                Trace.WriteLine("Extracted successfully");
                await Task.Run(Program.OnApplicationRefresh);
                foreach (var ver in MainDataModel.Default.Versions) ver.UpdateFolderSize();
            }
            catch (PackageManagerException e)
            {
                SetException(e);
            }
            catch (Exception e)
            {
                SetException(new PackageAddFailedException(e));
            }
            finally
            {
                EndTask();
            }
        }
        public async Task DownloadPackage(MCVersion v)
        {
            try
            {
                StartTask();
                await DownloadAndExtractPackage(v);
            }
            catch (PackageManagerException e)
            {
                SetException(e);
            }
            catch (Exception e)
            {
                SetException(new PackageDownloadAndExtractFailedException(e));
            }
            finally
            {
                EndTask();
            }
        }
        public void Cancel()
        {
            if (CancelSource != null && !CancelSource.IsCancellationRequested) CancelSource.Cancel();
        }

        #endregion

        #region Private Throwable Methods

        private async Task GetGameHandle(string processName)
        {
            await Task.Run(() =>
            {
                try
                {
                    Process[] MinecraftProcesses = Process.GetProcessesByName(processName);
                    Stopwatch attachTimeout = Stopwatch.StartNew();
                    while (MinecraftProcesses.Length == 0 && attachTimeout.Elapsed < TimeSpan.FromSeconds(30))
                    {
                        Thread.Sleep(500);
                        MinecraftProcesses = Process.GetProcessesByName(processName);
                    }

                    if (MinecraftProcesses.Length == 0)
                    {
                        Trace.WriteLine("Failed to attach Minecraft process: Timed out waiting for process.");
                        GameHandle = null;
                        MainDataModel.Default.ProgressBarState.SetGameRunningStatus(false);
                        return;
                    }

                    if (MinecraftProcesses.Length == 1)
                    {
                        AttachGameProcess(MinecraftProcesses[0]);
                    }
                    else
                    {
                        Trace.WriteLine("Failed to attach Minecraft process: Too many processes found");
                        GameHandle = null;
                        MainDataModel.Default.ProgressBarState.SetGameRunningStatus(false);
                    }
                }
                catch (InvalidOperationException e)
                {
                    throw e;
                }
                catch (Exception e)
                {
                    throw new PackageProcessHookFailedException(e);
                }
                finally
                {
                    EndTask();
                }
            });

        }

        private void AttachGameProcess(Process process)
        {
            MainDataModel.Default.ProgressBarState.SetGameRunningStatus(true);
            GameHandle = process;
            GameHandle.EnableRaisingEvents = true;
            GameHandle.Exited += OnPackageExit;

            void OnPackageExit(object sender, EventArgs e)
            {
                Process p = sender as Process;
                p.Exited -= OnPackageExit;
                GameHandle = null;
                MainDataModel.Default.ProgressBarState.SetGameRunningStatus(false);
            }

            Trace.WriteLine($"Successfully attached Minecraft process {process.Id}");
        }

        private async Task DownloadAndExtractPackage(MCVersion v)
        {
            try
            {
                Trace.WriteLine($"Download start: {v.PackageID}");
                SetCancelation(true);

                string subDirectory = GetPackageCacheDirectory(v);
                Directory.CreateDirectory(subDirectory);

                string packageFileName = GetMinecraftPackageFileName(v, ".Appx");
                string dlPath = Path.Combine(subDirectory, "Minecraft-" + v.Name + ".download.Appx");
                string bkpsPath = Path.Combine(subDirectory, packageFileName);
                ResolvedPackageFile packageFile = await ResolvePackagePath(v, dlPath, bkpsPath, subDirectory);
                PackagePayloadKind payloadKind = GetPackagePayloadKind(v, packageFile.PackagePath, out string packageProblem);

                if (payloadKind == PackagePayloadKind.Invalid)
                    throw new PackageDownloadFailedException($"Downloaded package is not a valid Minecraft package: {packageProblem}", new InvalidDataException(packageProblem));

                await ExtractPackage(v, packageFile.DownloadPath, packageFile.BackupPath, packageFile.PackagePath, CancelSource);

                v.UpdateFolderSize();
            }
            catch (PackageManagerException e)
            {
                ResetTask();
                throw e;
            }
            catch (Exception ex)
            {
                ResetTask();
                throw new Exception("DownloadAndExtractPackage Failed", ex);
            }
            finally
            {
                ResetTask();
                SetCancelation(false);
                CancelSource = null;
            }

        }
        private async Task<ResolvedPackageFile> ResolvePackagePath(MCVersion v, string dlPath, string bkpsPath, string backupDirectory)
        {
            string cachedPath = FindUsableCachedPackage(v, backupDirectory, bkpsPath);

            if (!string.IsNullOrEmpty(cachedPath))
                return new ResolvedPackageFile
                {
                    DownloadPath = dlPath,
                    BackupPath = cachedPath,
                    PackagePath = cachedPath
                };

            SafeDeleteFile(dlPath);
            await DownloadPackage(v, dlPath, CancelSource);

            if (!IsPackageFileUsable(v, dlPath, out string downloadProblem))
            {
                Trace.WriteLine($"Downloaded package is unusable, retrying once: {dlPath}. Reason: {downloadProblem}");
                SafeDeleteFile(dlPath);

                await DownloadPackage(v, dlPath, CancelSource);

                if (!IsPackageFileUsable(v, dlPath, out downloadProblem))
                {
                    SafeDeleteFile(dlPath);
                    throw new PackageDownloadFailedException(
                        $"Downloaded package is not a valid Minecraft package: {downloadProblem}",
                        new InvalidDataException(downloadProblem));
                }
            }

            return new ResolvedPackageFile
            {
                DownloadPath = dlPath,
                BackupPath = bkpsPath,
                PackagePath = dlPath
            };
        }

        private async Task DeleteDirectoryIfExists(string directory)
        {
            try
            {
                if (!IsPathInside(MainDataModel.Default.FilePaths.VersionsFolder, directory))
                {
                    Trace.WriteLine($"Skipping unsafe directory delete outside launcher versions folder: {directory}");
                    return;
                }

                if (Directory.Exists(directory))
                    await DirectoryExtensions.DeleteAsync(directory, (x, y, phase) => ProgressWrapper(x, y, phase));

                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Unable to clean directory {directory}: {ex}");
            }
        }

        private async Task DownloadPackage(MCVersion v, string dlPath, CancellationTokenSource cancelSource)
        {
            try
            {
                // UWP only: GDK packages are downloaded by EnsureMsixvcDownloaded (PackageHandler.Gdk.cs).
                if (v.PackageType != PackageType.UWP)
                    throw new InvalidOperationException($"Minecraft {v.Name} is a {v.PackageType} version and cannot use the UWP download.");

                if (v.IsBeta) await AuthenticateBetaUser();
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isDownloading);
                Trace.WriteLine("Download starting");
                await VersionDownloader.DownloadVersion(v.DisplayName, v.PackageID, 1, dlPath, (x, y) => ProgressWrapper(x, y), cancelSource.Token, v.Type);
                Trace.WriteLine("Download complete");
            }
            catch (PackageManagerException e)
            {
                ResetTask();
                throw e;
            }
            catch (TaskCanceledException e)
            {
                ResetTask();
                throw new PackageDownloadCanceledException(e);
            }
            catch (Exception e)
            {
                ResetTask();
                throw new PackageDownloadFailedException(e);
            }
            finally
            {
                ResetTask();
            }
        }
        private async Task RegisterPackage(MCVersion v)
        {
            try
            {
                Trace.WriteLine("Registering package");
                MainDataModel.Default.ProgressBarState.SetProgressBarText(v.GetPackageNameFromMainifest());
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isRegisteringPackage);

                try
                {
                    await DeploymentProgressWrapper(PM.RegisterPackageAsync(new Uri(v.ManifestPath), null, Constants.PackageDeploymentOptions));
                }
                catch (Exception registerException)
                {
                    Trace.WriteLine($"WinRT package registration failed, trying loose package registration: {registerException}");
                    await RegisterLoosePackageWithPowerShell(v.ManifestPath);
                }

                Trace.WriteLine("App re-register done!");
            }
            catch (PackageManagerException e)
            {
                ResetTask();
                throw e;
            }
            catch (Exception e)
            {
                ResetTask();
                throw new PackageRegistrationFailedException(e);
            }
            finally
            {
                ResetTask();
            }

        }

        private async Task RegisterLoosePackageWithPowerShell(string manifestPath)
        {
            string escapedManifestPath = PowerShellPackageCommand.Escape(Path.GetFullPath(manifestPath));
            string command = $"Add-AppxPackage -ForceApplicationShutdown -Register '{escapedManifestPath}'";
            await PowerShellPackageCommand.RunAsync(command, "loose package registration");
        }

        private async Task RemovePackageWithPowerShell(string packageFullName)
        {
            string escapedPackageName = PowerShellPackageCommand.Escape(packageFullName);
            string command = $"Remove-AppxPackage -Package '{escapedPackageName}' -PreserveRoamableApplicationData";
            await PowerShellPackageCommand.RunAsync(command, "package removal");
        }

        private async Task ExtractPackage(MCVersion v, string dlPath, string bkpsPath, string pkgPath, CancellationTokenSource cancelSource)
        {
            try
            {
                Trace.WriteLine("Extraction started");
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isExtracting);

                if (GetPackagePayloadKind(v, pkgPath, out string packageProblem) != PackagePayloadKind.ZipAppx)
                {
                    DeletePackageCopies(dlPath, bkpsPath, pkgPath);
                    throw new InvalidDataException($"Package archive is not extractable: {packageProblem}");
                }

                EnsureSafeLauncherVersionDirectory(v.GameDirectory);
                if (Directory.Exists(v.GameDirectory))
                    await DirectoryExtensions.DeleteAsync(v.GameDirectory, (x, y, phase) => ProgressWrapper(x, y, phase));

                using var fileStream = File.OpenRead(pkgPath);
                var progress = new Progress<ZipProgress>();
                progress.ProgressChanged += (s, z) => MainDataModel.Default.ProgressBarState.SetProgressBarProgress(currentProgress: z.Processed, totalProgress: z.Total);
                await Task.Run(() =>
                {
                    using var zipArchive = new ZipArchive(fileStream);
                    zipArchive.ExtractToDirectory(v.GameDirectory, progress, cancelSource);
                });

                await File.WriteAllTextAsync(v.IdentificationPath, v.PackageID);
                File.Delete(Path.Combine(v.GameDirectory, "AppxSignature.p7x"));

                if (!File.Exists(bkpsPath))
                {
                    if (Properties.LauncherSettings.Default.KeepAppx)
                        File.Move(dlPath, bkpsPath);
                    else
                        File.Delete(dlPath);
                }

                Trace.WriteLine("Extracted successfully");
            }
            catch (PackageManagerException e)
            {
                ResetTask();
                throw e;
            }
            catch (TaskCanceledException e)
            {
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isCanceling);
                await DeleteDirectoryIfExists(v.GameDirectory);
                ResetTask();
                throw new PackageExtractionCanceledException(e);
            }
            catch (Exception e)
            {
                if (e is InvalidDataException || e.InnerException is InvalidDataException)
                    DeletePackageCopies(dlPath, bkpsPath, pkgPath);

                ResetTask();
                throw new PackageExtractionFailedException(e);
            }
            finally
            {
                ResetTask();
            }
        }
        private bool IsPackageFileUsable(MCVersion v, string path, out string problem)
        {
            return GetPackagePayloadKind(v, path, out problem) != PackagePayloadKind.Invalid;
        }

        private PackagePayloadKind GetPackagePayloadKind(MCVersion v, string path, out string problem)
        {
            problem = string.Empty;

            if (!File.Exists(path))
            {
                problem = "file does not exist";
                return PackagePayloadKind.Invalid;
            }

            FileInfo fileInfo = new FileInfo(path);
            if (fileInfo.Length <= 0)
            {
                problem = "file is empty";
                return PackagePayloadKind.Invalid;
            }

            if (LooksLikeTextResponse(path, out problem))
                return PackagePayloadKind.Invalid;

            try
            {
                using FileStream stream = File.OpenRead(path);
                using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

                if (!zipArchive.Entries.Any(entry => entry.FullName.Equals(MCVersionExtensions.MainifestFileName, StringComparison.OrdinalIgnoreCase)))
                {
                    bool isBundle = zipArchive.Entries.Any(entry =>
                        entry.FullName.EndsWith("AppxBundleManifest.xml", StringComparison.OrdinalIgnoreCase));

                    if (isBundle)
                        return PackagePayloadKind.StorePackage;

                    problem = "AppxManifest.xml was not found in the archive";
                    return PackagePayloadKind.Invalid;
                }

                return PackagePayloadKind.ZipAppx;
            }
            catch (InvalidDataException ex)
            {
                if (Path.GetExtension(path).Equals(".Appx", StringComparison.OrdinalIgnoreCase))
                    Trace.WriteLine($"Package is not a ZIP/Appx archive and will be handled through Store deployment: {path}. Reason: {ex.Message}");

                problem = string.Empty;
                return PackagePayloadKind.StorePackage;
            }
            catch (Exception ex)
            {
                problem = ex.Message;
                return PackagePayloadKind.Invalid;
            }
        }

        private string FindUsableCachedPackage(MCVersion v, string backupDirectory, string preferredBackupPath)
        {
            foreach (string candidatePath in GetBackupCandidates(v, backupDirectory, preferredBackupPath))
            {
                if (!File.Exists(candidatePath))
                    continue;

                if (IsPackageFileUsable(v, candidatePath, out string backupProblem))
                    return candidatePath;

                Trace.WriteLine($"Deleting unusable cached package: {candidatePath}. Reason: {backupProblem}");
                SafeDeleteFile(candidatePath);
            }

            return null;
        }

        private IEnumerable<string> GetBackupCandidates(MCVersion v, string backupDirectory, string preferredBackupPath)
        {
            HashSet<string> emittedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string candidatePath in GetRawBackupCandidates(v, backupDirectory, preferredBackupPath))
            {
                if (string.IsNullOrWhiteSpace(candidatePath))
                    continue;

                string fullPath = Path.GetFullPath(candidatePath);
                if (emittedPaths.Add(fullPath))
                    yield return fullPath;
            }
        }

        private IEnumerable<string> GetRawBackupCandidates(MCVersion v, string backupDirectory, string preferredBackupPath)
        {
            yield return preferredBackupPath;

            foreach (string extension in MinecraftPackageExtensions)
                yield return Path.Combine(backupDirectory, GetMinecraftPackageFileName(v, extension));
        }

        private string GetPackageCacheDirectory(MCVersion v)
        {
            return Path.Combine(MainDataModel.Default.FilePaths.VersionsFolder, "AppxBackups");
        }

        private string GetMinecraftPackageFileName(MCVersion v, string extension)
        {
            return "Minecraft-" + v.Name + extension;
        }

        private bool LooksLikeTextResponse(string path, out string problem)
        {
            problem = string.Empty;

            try
            {
                // Size the preview buffer with 64-bit math: GDK packages are multi-gigabyte, so an int cast of the
                // file length overflows to a negative value and new byte[] throws "Arithmetic operation resulted in an overflow".
                long fileLength = new FileInfo(path).Length;
                int bufferSize = (int)Math.Min(1024L, fileLength);
                byte[] buffer = new byte[bufferSize];
                using (FileStream stream = File.OpenRead(path))
                    stream.Read(buffer, 0, buffer.Length);

                int start = 0;
                while (start < buffer.Length && (buffer[start] == 0xEF || buffer[start] == 0xBB || buffer[start] == 0xBF || char.IsWhiteSpace((char)buffer[start])))
                    start++;

                string preview = System.Text.Encoding.UTF8.GetString(buffer, start, buffer.Length - start).TrimStart();
                if (preview.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
                    || preview.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
                    || preview.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
                    || preview.StartsWith("{", StringComparison.OrdinalIgnoreCase)
                    || preview.StartsWith("[", StringComparison.OrdinalIgnoreCase)
                    || preview.StartsWith("Error", StringComparison.OrdinalIgnoreCase)
                    || preview.StartsWith("Not Found", StringComparison.OrdinalIgnoreCase))
                {
                    problem = "the download returned a text/web response instead of a Minecraft package";
                    return true;
                }

                if (fileLength < 64 * 1024)
                {
                    int printable = buffer.Count(x => x == 9 || x == 10 || x == 13 || (x >= 32 && x <= 126));
                    if (buffer.Length > 0 && printable / (double)buffer.Length > 0.9)
                    {
                        problem = "the downloaded file is too small and looks like a text response, not a Minecraft package";
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                problem = ex.Message;
                return true;
            }

            return false;
        }

        private void DeletePackageCopies(params string[] paths)
        {
            foreach (string path in paths.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                SafeDeleteFile(path);
        }

        private void SafeDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Unable to delete package file {path}: {ex}");
            }
        }

        private async Task UnregisterPackage(MCVersion v, bool keepVersion = false, bool mustMatchVersion = false)
        {
            try
            {
                foreach (var pkg in PM.FindPackagesForUser(string.Empty, Constants.GetPackageFamily(v.Type)))
                {
                    string location;

                    try { location = pkg.InstalledLocation.Path; }
                    catch (FileNotFoundException) { location = string.Empty; }

                    bool isLauncherVersionLocation = PathsEqual(location, v.GameDirectory);
                    bool isSafeLauncherPackageLocation = IsPathInside(MainDataModel.Default.FilePaths.VersionsFolder, location);

                    if (!isSafeLauncherPackageLocation)
                    {
                        Trace.WriteLine($"Skipping package removal outside launcher versions folder: {pkg.Id.FullName} {location}");
                        continue;
                    }

                    if (isLauncherVersionLocation && keepVersion)
                    {
                        Trace.WriteLine("Skipping package removal - same path: " + pkg.Id.FullName + " " + location);
                        continue;
                    }

                    if (v.PackageType == PackageType.GDK && !isLauncherVersionLocation)
                    {
                        Trace.WriteLine($"Skipping GDK package removal outside launcher version folder: {pkg.Id.FullName} {location}");
                        continue;
                    }

                    if (!isLauncherVersionLocation && mustMatchVersion) continue;

                    Trace.WriteLine("Removing package: " + pkg.Id.FullName);

                    MainDataModel.Default.ProgressBarState.SetProgressBarText(pkg.Id.FullName);
                    MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isRemovingPackage);

                    if (v.PackageType == PackageType.GDK)
                    {
                        await GdkRegistration.UnregisterAsync(pkg.Id.FullName);
                    }
                    else
                    {
                        try
                        {
                            await DeploymentProgressWrapper(PM.RemovePackageAsync(pkg.Id.FullName, Constants.PackageRemovalOptions));
                        }
                        catch (Exception removeException)
                        {
                            Trace.WriteLine($"WinRT package removal failed, trying package removal through PowerShell: {removeException}");
                            await RemovePackageWithPowerShell(pkg.Id.FullName);
                        }
                    }

                    Trace.WriteLine("Removal of package done: " + pkg.Id.FullName);
                }
            }
            catch (PackageManagerException e)
            {
                ResetTask();
                throw e;
            }
            catch (Exception ex)
            {
                ResetTask();
                throw new PackageDeregistrationFailedException(ex);
            }
            finally
            {
                ResetTask();
            }
        }

        private static bool PathsEqual(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            try
            {
                string normalizedLeft = Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string normalizedRight = Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void EnsureSafeLauncherVersionDirectory(string directory)
        {
            if (IsPathInside(MainDataModel.Default.FilePaths.VersionsFolder, directory))
                return;

            throw new InvalidOperationException($"Refusing to modify a Minecraft directory outside the launcher versions folder: {directory}");
        }

        private static bool IsPathInside(string root, string target)
        {
            if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(target))
                return false;

            try
            {
                string normalizedRoot = Path.GetFullPath(root)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string normalizedTarget = Path.GetFullPath(target)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                return string.Equals(normalizedRoot, normalizedTarget, StringComparison.OrdinalIgnoreCase) ||
                    normalizedTarget.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    normalizedTarget.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private async Task RedirectSaveData(string InstallationsFolderPath, VersionType type)
        {
            await Task.Run(() =>
            {
                try
                {
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                    string LocalStateFolder = Path.Combine(localAppData, "Packages", Constants.GetPackageFamily(type), "LocalState");
                    string PackageFolder = Path.Combine(localAppData, "Packages", Constants.GetPackageFamily(type), "LocalState", "games", "com.mojang");
                    string PackageBakFolder = Path.Combine(localAppData, "Packages", Constants.GetPackageFamily(type), "LocalState", "games", "com.mojang.default");
                    string ProfileFolder = Path.GetFullPath(InstallationsFolderPath);

                    string RequiredDir = Directory.GetParent(PackageFolder).FullName;
                    if (Directory.Exists(PackageFolder))
                    {
                        DirectoryInfo packageInfo = new DirectoryInfo(PackageFolder);
                        if ((packageInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                        {
                            Directory.Delete(PackageFolder, true);
                        }
                        else
                        {
                            string backupFolder = PackageBakFolder;
                            int backupIndex = 1;
                            while (Directory.Exists(backupFolder))
                            {
                                backupFolder = PackageBakFolder + "_" + backupIndex;
                                backupIndex++;
                            }

                            Directory.Move(PackageFolder, backupFolder);
                            Trace.WriteLine($"Moved existing Minecraft data folder to backup instead of deleting it: {backupFolder}");
                        }
                    }
                    if (!Directory.Exists(RequiredDir)) Directory.CreateDirectory(RequiredDir);
                    DirectoryInfo profileDir = Directory.CreateDirectory(ProfileFolder);
                    
                    // Attempt to create a symlink without elevated privileges
                    bool symlinkCreated = SymLinkHelper.CreateSymbolicLinkSafe(PackageFolder, ProfileFolder, SymLinkHelper.SymbolicLinkType.Directory);
                    if (!symlinkCreated)
                    {
                        throw new SaveRedirectionFailedException(new Exception("Failed to create symbolic link. Ensure Developer Mode is enabled or run as administrator."));
                    }
                    
                    DirectoryInfo pkgDir = Directory.CreateDirectory(PackageFolder);
                    DirectoryInfo lsDir = Directory.CreateDirectory(LocalStateFolder);

                    SecurityIdentifier owner = WindowsIdentity.GetCurrent().User;
                    SecurityIdentifier authenticated_users_identity = new SecurityIdentifier("S-1-5-11");

                    FileSystemAccessRule owner_access_rules = new FileSystemAccessRule(owner, FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow);
                    FileSystemAccessRule au_access_rules = new FileSystemAccessRule(authenticated_users_identity, FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow);

                    var lsSecurity = lsDir.GetAccessControl();
                    AuthorizationRuleCollection rules = lsSecurity.GetAccessRules(true, true, typeof(NTAccount));
                    List<FileSystemAccessRule> needed_rules = new List<FileSystemAccessRule>();
                    foreach (AccessRule rule in rules)
                    {
                        if (rule.IdentityReference is SecurityIdentifier)
                        {
                            var required_rule = new FileSystemAccessRule(rule.IdentityReference, FileSystemRights.FullControl, rule.InheritanceFlags, rule.PropagationFlags, rule.AccessControlType);
                            needed_rules.Add(required_rule);
                        }
                    }

                    var pkgSecurity = pkgDir.GetAccessControl();
                    pkgSecurity.SetOwner(owner);
                    pkgSecurity.AddAccessRule(au_access_rules);
                    pkgSecurity.AddAccessRule(owner_access_rules);
                    pkgDir.SetAccessControl(pkgSecurity);

                    var profileSecurity = profileDir.GetAccessControl();
                    //profileSecurity.SetOwner(owner);
                    profileSecurity.AddAccessRule(au_access_rules);
                    profileSecurity.AddAccessRule(owner_access_rules);
                    needed_rules.ForEach(x => profileSecurity.AddAccessRule(x));
                    profileDir.SetAccessControl(profileSecurity);
                }
                catch (PackageManagerException e)
                {
                    throw e;
                }
                catch (Exception e)
                {
                    throw new SaveRedirectionFailedException(e);
                }
            });

        }
        private async Task AuthenticateBetaUser()
        {
            try
            {
                var userIndex = Properties.LauncherSettings.Default.CurrentInsiderAccountIndex;
                var token = await Task.Run(() => AuthenticationManager.Default.GetWUToken(userIndex));
                StoreNetwork.setMSAUserToken(token);
            }
            catch (PackageManagerException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine("Error while Authenticating UserToken for Version Fetching:\n" + e); //TODO: Localize Error Message
                throw new BetaAuthenticationFailedException(e);
            }
        }
        #endregion

        #region Helpers

        protected async Task DeploymentProgressWrapper(IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> t)
        {
            TaskCompletionSource<int> src = new TaskCompletionSource<int>();
            t.Progress += (v, p) => MainDataModel.Default.ProgressBarState.SetProgressBarProgress(currentProgress: Convert.ToInt64(p.percentage), totalProgress: 100);
            t.Completed += (v, p) =>
            {
                MainDataModel.Default.ProgressBarState.ResetProgressBarProgress();

                if (p == AsyncStatus.Error)
                {
                    string errorText;
                    try { errorText = v.GetResults().ErrorText; }
                    catch (Exception ex) { errorText = ex.Message; }

                    // The inner exception carries the HRESULT Windows reported (read by the GDK pipeline).
                    Trace.WriteLine("Deployment failed: " + errorText);
                    src.SetException(new Exception("Deployment failed: " + errorText, v.ErrorCode));
                }
                else
                {
                    Trace.WriteLine("Deployment done: " + p);
                    src.SetResult(1);
                }
            };
            await src.Task;
        }
        protected void ProgressWrapper(long current, long total, string text = null)
        {
            MainDataModel.Default.ProgressBarState.SetProgressBarProgress(current, total);
            MainDataModel.Default.ProgressBarState.SetProgressBarText(text);
        }
        protected void ResetTask()
        {
            MainDataModel.Default.ProgressBarState.ResetProgressBarProgress();
            MainDataModel.Default.ProgressBarState.SetProgressBarText();
            MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.None);
        }
        protected void EndTask()
        {
            MainDataModel.Default.ProgressBarState.ResetProgressBarProgress();
            MainDataModel.Default.ProgressBarState.SetProgressBarText();
            MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.None);
            MainDataModel.Default.ProgressBarState.SetProgressBarVisibility(false);
        }
        protected void StartTask()
        {
            MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isInitializing);
            MainDataModel.Default.ProgressBarState.SetProgressBarVisibility(true);

        }
        protected void SetCancelation(bool cancelState)
        {
            if (cancelState) CancelSource = new CancellationTokenSource();
            MainDataModel.Default.ProgressBarState.AllowCancel = cancelState ? true : false;
            MainDataModel.Default.ProgressBarState.CancelCommand = cancelState ? new RelayCommand((o) => Cancel()) : null;
        }
        protected void SetException(Exception e)
        {
            if (e.GetType() == typeof(GdkEntitlementException)) SetGdkError(e, "Microsoft account");
            else if (e.GetType() == typeof(GdkVersionUnavailableException)) SetGdkError(e, "Minecraft version unavailable");
            else if (e.GetType() == typeof(GdkDeploymentRejectedException)) SetGdkError(e, "Windows rejected the package");
            else if (e.GetType() == typeof(GdkVersionMismatchException)) SetGdkError(e, "Wrong Minecraft GDK package installed");
            else if (e.GetType() == typeof(GdkRequirementUnresolvedException)) SetGdkError(e, "Minecraft GDK package unknown");
            else if (e.GetType() == typeof(GdkBootstrapException)) SetGdkError(e, "Minecraft launch helper unavailable");
            else if (e.GetType() == typeof(PackageExtractionFailedException)) SetError(e, "Extraction failed", "Error_AppExtractionFailed_Title", "Error_AppExtractionFailed");
            else if (e.GetType() == typeof(PackageDownloadFailedException)) SetError(e, "Download failed", "Error_AppDownloadFailed_Title", "Error_AppDownloadFailed");
            else if (e.GetType() == typeof(BetaAuthenticationFailedException)) SetError(e, "Authentication failed", "Error_AuthenticationFailed_Title", "Error_AuthenticationFailed");
            else if (e.GetType() == typeof(AppLaunchFailedException)) SetError(e, "App launch failed", "Error_AppLaunchFailed_Title", "Error_AppLaunchFailed");
            else if (e.GetType() == typeof(PackageRegistrationFailedException)) SetError(e, "App registeration failed", "Error_AppReregisterFailed_Title", "Error_AppReregisterFailed");
            else if (e.GetType() == typeof(PackageRemovalFailedException)) SetError(e, "App uninstall failed", "Error_AppUninstallFailed_Title", "Error_AppUninstallFailed");
            else if (e.GetType() == typeof(SaveRedirectionFailedException)) SetError(e, "Save redirection failed", "Error_SaveDirectoryRedirectionFailed_Title", "Error_SaveDirectoryRedirectionFailed");
            else if (e.GetType() == typeof(PackageDeregistrationFailedException)) SetError(e, "App deregisteration failed", "Error_AppDeregisteringFailed_Title", "Error_AppDeregisteringFailed");

            else if (e.GetType() == typeof(PackageDownloadAndExtractFailedException)) SetGenericError(e);
            else if (e.GetType() == typeof(PackageProcessHookFailedException)) SetGenericError(e);

            else if (e.GetType() == typeof(PackageExtractionCanceledException)) CancelAction();
            else if (e.GetType() == typeof(PackageDownloadCanceledException)) CancelAction();

            else SetGenericError(e);

            void CancelAction()
            {
                SetCancelation(false);
            }

            void SetGenericError(Exception ex)
            {
                _ = MainDataModel.BackwardsCommunicationHost.exceptionmsg(ex);
            }

            void SetGdkError(Exception ex, string dialogTitle)
            {
                Trace.WriteLine(dialogTitle + ":\n" + ex.ToString());
                _ = ErrorScreenShow.exceptionmsg(dialogTitle, new Exception(ex.Message)); //TODO: Localize String
            }

            void SetError(Exception ex2, string debugMessage, string dialogTitle, string dialogText)
            {
                Trace.WriteLine(debugMessage + ":\n" + ex2.ToString());
                MainDataModel.BackwardsCommunicationHost.errormsg(dialogTitle, dialogText, ex2);
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            CancelSource?.Dispose();
        }

        #endregion







    }
}