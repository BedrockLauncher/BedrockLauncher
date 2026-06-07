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
    public class PackageHandler : IDisposable
    {
        private CancellationTokenSource CancelSource = new CancellationTokenSource();
        private StoreNetwork StoreNetwork = new StoreNetwork();
        private PackageManager PM = new PackageManager();
        private static readonly string[] MinecraftPackageExtensions = [".msixvc", ".Appx", ".appx", ".msix", ".msixbundle", ".appxbundle"];
        private static readonly string[] RequiredGdkRuntimeDlls =
        {
            "vcruntime140_1.dll",
            "concrt140_app.dll",
            "msvcp140_app.dll",
            "vcruntime140_app.dll"
        };
        private const string MsixvcExtractorHelperFileName = "BedrockLauncher.exe";
        private const string MsixvcExtractorHelperArgument = "--bedrock-msixvc-extract";

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

        public async Task LaunchPackage(MCVersion v, string dirPath, bool KeepLauncherOpen, bool LaunchEditor)
        {
            try
            {
                StartTask();
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isLaunching);

                if (!LaunchEditor && v.PackageType == PackageType.GDK && v.IsInstalledInLauncher)
                {
                    if (await TryLaunchLocalGdkExecutable(v, KeepLauncherOpen))
                    {
                        Trace.WriteLine("App launch finished through local GDK version folder.");
                        if (!KeepLauncherOpen)
                            await Application.Current.Dispatcher.InvokeAsync(() => Application.Current.MainWindow.Close());

                        return;
                    }

                    SetException(new AppLaunchFailedException(
                        $"Could not launch the local Minecraft version folder: {v.DisplayName}",
                        new Exception("Minecraft.Windows.exe could not be started from the selected version folder.")));
                    return;
                }

                bool preferPackageActivation = ShouldPreferPackageActivation(v, LaunchEditor);

                if (preferPackageActivation)
                {
                    if (await TryLaunchGdkDesktopPackageCommand(v, KeepLauncherOpen) ||
                        await TryLaunchPackageActivation(v, KeepLauncherOpen))
                    {
                        Trace.WriteLine("App launch finished through selected package activation.");
                        if (!KeepLauncherOpen)
                            await Application.Current.Dispatcher.InvokeAsync(() => Application.Current.MainWindow.Close());

                        return;
                    }

                    SetException(new AppLaunchFailedException(
                        $"Could not launch the selected Minecraft version: {v.DisplayName}",
                        new Exception("The selected package could not be activated.")));
                    return;
                }

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
            catch (Exception e)
            {
                EndTask();
                SetException(new AppLaunchFailedException(e));
            }
        }

        private static bool ShouldPreferPackageActivation(MCVersion v, bool launchEditor)
        {
            if (v == null || launchEditor) return false;
            if (v.PackageType != PackageType.GDK) return false;
            return System.Version.TryParse(v.Name, out _);
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

        private async Task<bool> TryLaunchGdkDesktopPackageCommand(MCVersion v, bool keepLauncherOpen)
        {
            try
            {
                Package package = GetInstalledMinecraftPackage(v, true);
                if (package?.InstalledLocation?.Path == null)
                {
                    Trace.WriteLine($"No installed package found for {v.DisplayName}");
                    return false;
                }

                string gamePath = Path.Combine(package.InstalledLocation.Path, "Minecraft.Windows.exe");
                if (!File.Exists(gamePath))
                {
                    Trace.WriteLine($"Minecraft.Windows.exe was not found at {gamePath}");
                    return false;
                }

                string appId = GetGdkApplicationId(package.InstalledLocation.Path);
                string powershellPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.System),
                    @"WindowsPowerShell\v1.0\powershell.exe");

                string escapedPackageFamily = EscapePowerShellSingleQuotedString(package.Id.FamilyName);
                string escapedGamePath = EscapePowerShellSingleQuotedString(gamePath);
                string escapedAppId = EscapePowerShellSingleQuotedString(appId);
                string command = $"Invoke-CommandInDesktopPackage -PackageFamilyName '{escapedPackageFamily}' -AppId '{escapedAppId}' -Command '{escapedGamePath}'";

                using Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = powershellPath,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    ArgumentList =
                    {
                        "-NoProfile",
                        "-NonInteractive",
                        "-ExecutionPolicy",
                        "Bypass",
                        "-Command",
                        command
                    }
                });

                if (process == null)
                    return false;

                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    Trace.WriteLine($"Desktop package command exited with code {process.ExitCode}");
                    return false;
                }

                if (keepLauncherOpen)
                    await GetGameHandle(Constants.MINECRAFT_PROCESS_NAME);

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Desktop package command launch failed: {ex}");
                return false;
            }
        }

        private async Task<bool> TryLaunchLocalGdkExecutable(MCVersion v, bool keepLauncherOpen)
        {
            try
            {
                string gamePath = Path.Combine(v.GameDirectory, "Minecraft.Windows.exe");
                if (!File.Exists(gamePath))
                {
                    Trace.WriteLine($"Local GDK executable was not found at {gamePath}");
                    return false;
                }

                using Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = gamePath,
                    WorkingDirectory = v.GameDirectory,
                    UseShellExecute = false
                });

                if (process == null)
                    return false;

                if (keepLauncherOpen)
                    await GetGameHandle(Constants.MINECRAFT_PROCESS_NAME);

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Local GDK executable launch failed: {ex}");
                return false;
            }
        }

        private static string GetGdkApplicationId(string packageDirectory)
        {
            try
            {
                string manifestPath = Directory.EnumerateFiles(packageDirectory, "AppxManifest.xml", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.EnumerateFiles(packageDirectory, "appxmanifest.xml", SearchOption.TopDirectoryOnly))
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(manifestPath))
                {
                    XDocument manifest = XDocument.Load(manifestPath);
                    string manifestAppId = manifest.Descendants()
                        .FirstOrDefault(x => x.Name.LocalName == "Application")?
                        .Attribute("Id")?
                        .Value;

                    if (!string.IsNullOrWhiteSpace(manifestAppId))
                        return manifestAppId.Trim();
                }

                string configPath = Path.Combine(packageDirectory, "MicrosoftGame.Config");
                if (File.Exists(configPath))
                {
                    XDocument gameConfig = XDocument.Load(configPath);
                    string configAppId = gameConfig.Descendants()
                        .FirstOrDefault(x =>
                            x.Name.LocalName == "Executable" &&
                            string.Equals((string)x.Attribute("TargetDeviceFamily"), "PC", StringComparison.OrdinalIgnoreCase))?
                        .Attribute("Id")?
                        .Value;

                    if (!string.IsNullOrWhiteSpace(configAppId))
                        return configAppId.Trim();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Unable to read GDK application id: {ex}");
            }

            return "Game";
        }

        private Package GetInstalledMinecraftPackage(MCVersion v, bool requireExactVersion = false)
        {
            try
            {
                List<Package> packages = PM.FindPackagesForUser(string.Empty, Constants.GetPackageFamily(v.Type))
                    .Where(package =>
                    {
                        string location = GetPackageInstalledLocation(package);
                        bool isOfficial = string.IsNullOrWhiteSpace(location) || IsOfficialMinecraftPackageLocation(location);
                        if (!isOfficial)
                            Trace.WriteLine("Ignoring loose external Minecraft registration for launch.");

                        return isOfficial;
                    })
                    .ToList();
                if (packages.Count == 0) return null;

                Package matchingPackage = packages.FirstOrDefault(package => IsSamePackageVersion(package, v));
                if (requireExactVersion)
                    return matchingPackage;

                return matchingPackage ?? packages.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to resolve installed package for {v.DisplayName}: {ex}");
                return null;
            }
        }

        private static string GetPackageInstalledLocation(Package package)
        {
            try
            {
                return package?.InstalledLocation?.Path ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsOfficialMinecraftPackageLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return false;

            string normalized = Path.GetFullPath(location)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return normalized.IndexOf(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf(@"\XboxGames\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf(@"\ModifiableWindowsApps\", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsSamePackageVersion(Package package, MCVersion version)
        {
            try
            {
                var packageVersion = package.Id.Version;
                if (!Version.TryParse(version.Name, out Version expectedVersion))
                    return false;

                var installedVersion = new Version(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
                if (installedVersion == expectedVersion)
                    return true;

                if (expectedVersion.Major == 1)
                {
                    if (installedVersion.Major != 1 || installedVersion.Minor != expectedVersion.Minor || expectedVersion.Build < 0)
                        return false;

                    int expectedRevision = Math.Max(expectedVersion.Revision, 0);
                    int encodedBuild = expectedVersion.Build * 100 + expectedRevision;
                    if (installedVersion.Build == encodedBuild)
                        return true;

                    return expectedVersion.Revision < 0 && installedVersion.Build / 100 == expectedVersion.Build;
                }

                if (installedVersion.Major != 1 || installedVersion.Minor != expectedVersion.Major || expectedVersion.Minor < 0)
                    return false;

                // GDK direct form:
                //   1.26.0.2  == 26.0.2
                //   1.26.10.4 == 26.10.4
                if (installedVersion.Build == expectedVersion.Minor)
                {
                    if (expectedVersion.Build < 0)
                        return true;

                    return installedVersion.Revision == expectedVersion.Build;
                }

                // GDK encoded form:
                //   1.26.2101.0 == 26.21.1
                int installedFeature = installedVersion.Build / 100;
                int installedPatch = installedVersion.Build % 100;
                if (installedFeature != expectedVersion.Minor)
                    return false;

                return expectedVersion.Build < 0 || installedPatch == expectedVersion.Build;
            }
            catch
            {
                return false;
            }
        }

        private static string EscapePowerShellSingleQuotedString(string value)
        {
            return value?.Replace("'", "''") ?? string.Empty;
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


        public async Task InstallPackage(MCVersion v, string dirPath)
        {
            try
            {
                StartTask();

                if (v.PackageType == PackageType.GDK)
                {
                    if (!v.IsInstalledInLauncher)
                    {
                        List<VersionInfoJson> gdkVersions = VersionManager.Singleton.GetVersions();
                        if (gdkVersions.Any(ver => v.UUID.CompareTo(ver.uuid.ToString()) == 0))
                        {
                            await DownloadAndExtractPackage(v);
                        }
                        else
                        {
                            throw new NoVersionAccessibleException();
                        }
                    }

                    if (v.IsInstalledInLauncher)
                    {
                        Trace.WriteLine($"Skipping app registration because GDK version is isolated in launcher folder: {v.DisplayName}");
                        return;
                    }

                    throw new PackageExtractionFailedException(
                        "The GDK package was downloaded, but it did not extract into a complete playable Minecraft folder.",
                        new InvalidDataException("Missing Minecraft.Windows.exe, data folder, MicrosoftGame.Config, or required VC runtime DLLs."));
                }

                bool canUseExternalInstallOnly = !ShouldPreferPackageActivation(v, false);

                if (canUseExternalInstallOnly && !v.IsInstalledInLauncher && v.IsInstalledExternally)
                {
                    Trace.WriteLine($"Skipping local package registration because Minecraft is already installed externally: {v.DisplayName}");
                    return;
                }

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

                if (canUseExternalInstallOnly && !v.IsInstalledInLauncher && v.IsInstalledExternally)
                {
                    Trace.WriteLine($"Skipping local package registration because Minecraft was installed through Store deployment: {v.DisplayName}");
                    return;
                }

                await UnregisterPackage(v, true);
                await RegisterPackage(v);

                await RedirectSaveData(dirPath, v.Type);
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
                        MainDataModel.Default.ProgressBarState.SetGameRunningStatus(true);
                        GameHandle = MinecraftProcesses[0];
                        GameHandle.EnableRaisingEvents = true;
                        GameHandle.Exited += OnPackageExit;


                        void OnPackageExit(object sender, EventArgs e)
                        {
                            Process p = sender as Process;
                            p.Exited -= OnPackageExit;
                            GameHandle = null;
                            MainDataModel.Default.ProgressBarState.SetGameRunningStatus(false);
                        }

                        Trace.WriteLine("Successfully attached Minecraft process");
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

        private async Task DownloadAndExtractPackage(MCVersion v)
        {
            //MCVersion debugGDKVersion = new MCVersion("", "", "1.21.120", VersionType.Release, "x64");

            try
            {
                Trace.WriteLine($"Download start: {v.PackageID}");
                SetCancelation(true);

                string subDirectory = GetPackageCacheDirectory(v);
                Directory.CreateDirectory(subDirectory);

                string packageExtension = v.PackageType == PackageType.GDK ? ".msixvc" : ".Appx";
                string packageFileName = GetMinecraftPackageFileName(v, packageExtension);
                string dlPath = Path.Combine(subDirectory, "Minecraft-" + v.Name + ".download" + packageExtension);
                string bkpsPath = Path.Combine(subDirectory, packageFileName);
                ResolvedPackageFile packageFile = await ResolvePackagePath(v, dlPath, bkpsPath, subDirectory);
                PackagePayloadKind payloadKind = GetPackagePayloadKind(v, packageFile.PackagePath, out string packageProblem);

                if (payloadKind == PackagePayloadKind.Invalid)
                    throw new PackageDownloadFailedException($"Downloaded package is not a valid Minecraft package: {packageProblem}", new InvalidDataException(packageProblem));

                if (v.PackageType == PackageType.GDK || payloadKind == PackagePayloadKind.StorePackage)
                    await InstallGdkPackage(v, packageFile.DownloadPath, packageFile.BackupPath, packageFile.PackagePath);
                else
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

        private async Task InstallGdkPackage(MCVersion v, string dlPath, string bkpsPath, string pkgPath)
        {
            try
            {
                Trace.WriteLine("Installing local Store/MSIXVC version folder");
                MainDataModel.Default.ProgressBarState.SetProgressBarText(v.DisplayName);
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isExtracting);

                string absolutePackagePath = EnsureDeploymentPackageExtension(v, pkgPath);
                if (!Path.GetExtension(absolutePackagePath).Equals(Path.GetExtension(bkpsPath), StringComparison.OrdinalIgnoreCase))
                    bkpsPath = Path.ChangeExtension(bkpsPath, Path.GetExtension(absolutePackagePath));

                string persistentPackagePath = PersistGdkDeploymentPackage(absolutePackagePath, bkpsPath);

                string nativeExtractionProblem = string.Empty;
                if (await TryInstallGdkPackageWithNativeExtractor(v, persistentPackagePath, dlPath, CancelSource, problem => nativeExtractionProblem = problem))
                {
                    DeletePackageCopies(dlPath);
                    MCVersion.ClearInstallProbeCache();
                    Trace.WriteLine("Store/MSIXVC package extracted as isolated launcher version");
                    return;
                }

                string helperExtractionProblem = string.Empty;
                if (await TryInstallGdkPackageWithHelperExtractor(v, persistentPackagePath, CancelSource, problem => helperExtractionProblem = problem))
                {
                    DeletePackageCopies(dlPath);
                    MCVersion.ClearInstallProbeCache();
                    Trace.WriteLine("Store/MSIXVC package extracted through helper as isolated launcher version");
                    return;
                }

                throw new InvalidOperationException(BuildGdkExtractionFailureMessage(v, nativeExtractionProblem, helperExtractionProblem));
            }
            catch (PackageManagerException e)
            {
                ResetTask();
                throw e;
            }
            catch (Exception e)
            {
                ResetTask();
                throw new PackageExtractionFailedException(
                    "The GDK package could not be extracted into a complete playable local version folder.",
                    e);
            }
            finally
            {
                ResetTask();
            }
        }

        private async Task<bool> TryInstallGdkPackageWithNativeExtractor(
            MCVersion v,
            string packagePath,
            string dlPath,
            CancellationTokenSource cancelSource,
            Action<string> reportFailure)
        {
            if (!Path.GetExtension(packagePath).Equals(".msixvc", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                Trace.WriteLine($"Attempting native MSIXVC extraction: {packagePath}");
                MainDataModel.Default.ProgressBarState.SetProgressBarText(v.DisplayName);
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isExtracting);

                EnsureSafeLauncherVersionDirectory(v.GameDirectory);
                if (Directory.Exists(v.GameDirectory))
                    await DirectoryExtensions.DeleteAsync(v.GameDirectory, (x, y, phase) => ProgressWrapper(x, y, phase));

                Directory.CreateDirectory(v.GameDirectory);

                string extractionError = string.Empty;
                bool extracted = await Task.Run(() =>
                    NativeMsixvcExtractor.TryExtract(packagePath, v.GameDirectory, (current, total) =>
                    {
                        MainDataModel.Default.ProgressBarState.SetProgressBarProgress(current, total);
                    }, out extractionError),
                    cancelSource?.Token ?? CancellationToken.None);

                if (!extracted)
                {
                    Trace.WriteLine($"Native MSIXVC extraction failed; Windows Store deployment is intentionally disabled: {extractionError}");
                    reportFailure?.Invoke(extractionError);
                    await DeleteDirectoryIfExists(v.GameDirectory);
                    return false;
                }

                return await FinalizeExtractedGdkVersionFolder(
                    v,
                    cancelSource?.Token ?? CancellationToken.None,
                    "Native MSIXVC extraction");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Native MSIXVC install path failed; Windows Store deployment is intentionally disabled: {ex}");
                reportFailure?.Invoke(ex.Message);
                await DeleteDirectoryIfExists(v.GameDirectory);
                return false;
            }
        }

        private async Task<bool> TryInstallGdkPackageWithHelperExtractor(
            MCVersion v,
            string packagePath,
            CancellationTokenSource cancelSource,
            Action<string> reportFailure)
        {
            if (!Path.GetExtension(packagePath).Equals(".msixvc", StringComparison.OrdinalIgnoreCase))
                return false;

            string errorFilePath = Path.Combine(Path.GetTempPath(), "BedrockLauncher-msixvc-" + Guid.NewGuid().ToString("N") + ".err");
            CancellationToken cancellationToken = cancelSource?.Token ?? CancellationToken.None;

            try
            {
                Trace.WriteLine($"Attempting helper MSIXVC extraction: {packagePath}");
                MainDataModel.Default.ProgressBarState.SetProgressBarText(v.DisplayName);
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isExtracting);

                EnsureSafeLauncherVersionDirectory(v.GameDirectory);
                if (Directory.Exists(v.GameDirectory))
                    await DirectoryExtensions.DeleteAsync(v.GameDirectory, (x, y, phase) => ProgressWrapper(x, y, phase));

                Directory.CreateDirectory(v.GameDirectory);

                string helperPath = EnsureMsixvcExtractorHelperExecutable();
                using Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = helperPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.StartInfo.ArgumentList.Add(MsixvcExtractorHelperArgument);
                process.StartInfo.ArgumentList.Add(packagePath);
                process.StartInfo.ArgumentList.Add(v.GameDirectory);
                process.StartInfo.ArgumentList.Add(errorFilePath);

                if (!process.Start())
                    throw new InvalidOperationException("MSIXVC helper process did not start.");

                Task outputTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        string line = await process.StandardOutput.ReadLineAsync();
                        if (line == null) break;
                        TryUpdateHelperExtractionProgress(line);
                    }
                }, cancellationToken);
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                try
                {
                    await process.WaitForExitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    TryKillProcess(process);
                    throw;
                }

                await outputTask;
                string stderr = await errorTask;

                if (process.ExitCode != 0)
                {
                    string helperProblem = ReadHelperExtractionError(errorFilePath, stderr, process.ExitCode);
                    Trace.WriteLine($"Helper MSIXVC extraction failed: {helperProblem}");
                    reportFailure?.Invoke(helperProblem);
                    await DeleteDirectoryIfExists(v.GameDirectory);
                    return false;
                }

                if (!await FinalizeExtractedGdkVersionFolder(v, cancellationToken, "Helper MSIXVC extraction"))
                {
                    reportFailure?.Invoke("helper extractor did not produce a complete Minecraft folder");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Helper MSIXVC install path failed; Windows Store deployment is intentionally disabled: {ex}");
                reportFailure?.Invoke(ex.Message);
                await DeleteDirectoryIfExists(v.GameDirectory);
                return false;
            }
            finally
            {
                SafeDeleteFile(errorFilePath);
            }
        }

        private async Task<bool> FinalizeExtractedGdkVersionFolder(MCVersion v, CancellationToken cancellationToken, string extractionLabel)
        {
            await EnsureGdkRuntimeDlls(v.GameDirectory, cancellationToken);

            if (!IsCompleteGdkVersionFolder(v.GameDirectory))
            {
                Trace.WriteLine($"{extractionLabel} did not produce a complete Minecraft folder.");
                await DeleteDirectoryIfExists(v.GameDirectory);
                return false;
            }

            await EnsureLauncherGdkManifest(v, v.GameDirectory);
            await File.WriteAllTextAsync(v.IdentificationPath, v.PackageID, cancellationToken);
            await WriteLauncherVersionMetadata(v, registered: false, cancellationToken);
            SafeDeleteFile(Path.Combine(v.GameDirectory, "AppxSignature.p7x"));

            return true;
        }

        private string EnsureMsixvcExtractorHelperExecutable()
        {
            string currentExecutable = null;

            try
            {
                currentExecutable = Process.GetCurrentProcess().MainModule?.FileName;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Could not read current executable path: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(currentExecutable) || !File.Exists(currentExecutable))
                currentExecutable = Path.Combine(AppContext.BaseDirectory, "BedrockLauncher.exe");

            if (!File.Exists(currentExecutable))
                throw new FileNotFoundException("Current launcher executable was not found.", currentExecutable);

            string helperPath = Path.Combine(AppContext.BaseDirectory, MsixvcExtractorHelperFileName);
            if (string.Equals(Path.GetFullPath(currentExecutable), Path.GetFullPath(helperPath), StringComparison.OrdinalIgnoreCase))
                return helperPath;

            bool needsCopy = !File.Exists(helperPath);
            if (!needsCopy)
            {
                FileInfo currentInfo = new FileInfo(currentExecutable);
                FileInfo helperInfo = new FileInfo(helperPath);
                needsCopy = currentInfo.Length != helperInfo.Length || currentInfo.LastWriteTimeUtc > helperInfo.LastWriteTimeUtc;
            }

            if (needsCopy)
            {
                try
                {
                    File.Copy(currentExecutable, helperPath, true);
                    Trace.WriteLine($"Prepared MSIXVC extractor helper: {helperPath}");
                }
                catch (IOException ex) when (File.Exists(helperPath))
                {
                    Trace.WriteLine($"Could not refresh MSIXVC extractor helper, using existing copy: {ex.Message}");
                }
            }

            return helperPath;
        }

        private void TryUpdateHelperExtractionProgress(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("PROGRESS ", StringComparison.OrdinalIgnoreCase))
                return;

            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                return;

            if (long.TryParse(parts[1], out long current) && long.TryParse(parts[2], out long total) && total > 0)
                MainDataModel.Default.ProgressBarState.SetProgressBarProgress(current, total);
        }

        private static string ReadHelperExtractionError(string errorFilePath, string stderr, int exitCode)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(errorFilePath) && File.Exists(errorFilePath))
                {
                    string helperError = File.ReadAllText(errorFilePath);
                    if (!string.IsNullOrWhiteSpace(helperError))
                        return helperError.Trim();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Could not read MSIXVC helper error file: {ex.Message}");
            }

            if (!string.IsNullOrWhiteSpace(stderr))
                return stderr.Trim();

            return $"MSIXVC helper exited with code {exitCode}.";
        }

        private static void TryKillProcess(Process process)
        {
            try
            {
                if (process != null && !process.HasExited)
                    process.Kill(true);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Could not stop MSIXVC helper process: {ex.Message}");
            }
        }

        private string BuildGdkExtractionFailureMessage(MCVersion v, string nativeExtractionProblem, string helperExtractionProblem)
        {
            string nativeReason = string.IsNullOrWhiteSpace(nativeExtractionProblem)
                ? "unknown native extractor failure"
                : nativeExtractionProblem.Trim();
            string helperReason = string.IsNullOrWhiteSpace(helperExtractionProblem)
                ? "unknown helper extractor failure"
                : helperExtractionProblem.Trim();

            return
                $"MSIXVC extraction failed for {v.DisplayName}. Native reason: {nativeReason}. Helper reason: {helperReason}. " +
                "BedrockLauncher did not modify your Microsoft Store Minecraft install. " +
                "Windows package staging is disabled so downloaded versions are not registered as the default Minecraft app. " +
                "The package stayed cached in BedrockLauncher's installers folder, so you can retry without redownloading it.";
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

        private string PersistGdkDeploymentPackage(string absolutePackagePath, string backupPath)
        {
            string persistentPackagePath = Path.GetFullPath(backupPath);
            Directory.CreateDirectory(Path.GetDirectoryName(persistentPackagePath));

            if (!absolutePackagePath.Equals(persistentPackagePath, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(absolutePackagePath, persistentPackagePath, true);
                Trace.WriteLine($"Persisted GDK package for deployment: {persistentPackagePath}");
            }

            return persistentPackagePath;
        }

        private async Task CopyDeployedGdkPackageToLauncherVersion(MCVersion v, string sourceDirectory, CancellationTokenSource cancelSource)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException($"Minecraft package source directory was not found: {sourceDirectory}");

            string targetDirectory = v.GameDirectory;
            Trace.WriteLine($"Copying deployed package from {sourceDirectory} to {targetDirectory}");
            MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isExtracting);

            if (Directory.Exists(targetDirectory))
                await DirectoryExtensions.DeleteAsync(targetDirectory, (x, y, phase) => ProgressWrapper(x, y, phase));

            await Task.Run(() =>
            {
                string[] files = Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories).ToArray();
                long current = 0;
                long total = Math.Max(files.LongLength, 1);

                Directory.CreateDirectory(targetDirectory);
                foreach (string sourceFile in files)
                {
                    cancelSource?.Token.ThrowIfCancellationRequested();

                    string relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
                    string destinationFile = Path.Combine(targetDirectory, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                    File.Copy(sourceFile, destinationFile, true);
                    current++;
                    ProgressWrapper(current, total, "Files");
                }
            }, cancelSource?.Token ?? CancellationToken.None);

            string gameExe = Path.Combine(targetDirectory, "Minecraft.Windows.exe");
            string gameConfig = Path.Combine(targetDirectory, "MicrosoftGame.Config");
            if (!File.Exists(gameExe))
                throw new FileNotFoundException("Minecraft.Windows.exe was not found after copying the package.", gameExe);
            if (!File.Exists(gameConfig))
                throw new FileNotFoundException("MicrosoftGame.Config was not found after copying the package.", gameConfig);

            await EnsureGdkRuntimeDlls(targetDirectory, cancelSource?.Token ?? CancellationToken.None);
            await EnsureLauncherGdkManifest(v, targetDirectory);
            await File.WriteAllTextAsync(v.IdentificationPath, v.PackageID, cancelSource?.Token ?? CancellationToken.None);
            await WriteLauncherVersionMetadata(v, registered: false, cancelSource?.Token ?? CancellationToken.None);
            SafeDeleteFile(Path.Combine(targetDirectory, "AppxSignature.p7x"));
        }

        private static bool IsCompleteGdkVersionFolder(string versionDirectory)
        {
            if (string.IsNullOrWhiteSpace(versionDirectory))
                return false;

            return File.Exists(Path.Combine(versionDirectory, "Minecraft.Windows.exe")) &&
                File.Exists(Path.Combine(versionDirectory, "MicrosoftGame.Config")) &&
                Directory.Exists(Path.Combine(versionDirectory, "data")) &&
                RequiredGdkRuntimeDlls.All(fileName => File.Exists(Path.Combine(versionDirectory, fileName)));
        }

        private async Task EnsureGdkRuntimeDlls(string versionDirectory, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(versionDirectory);

            foreach (string runtimeDll in RequiredGdkRuntimeDlls)
            {
                string targetPath = Path.Combine(versionDirectory, runtimeDll);
                if (File.Exists(targetPath))
                    continue;

                string sourcePath = FindGdkRuntimeDll(runtimeDll);
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    Trace.WriteLine($"Could not find required GDK runtime DLL for local version folder: {runtimeDll}");
                    continue;
                }

                await Task.Run(() => File.Copy(sourcePath, targetPath, true), cancellationToken);
                Trace.WriteLine($"Copied {runtimeDll} into local GDK version folder: {targetPath}");
            }
        }

        private string FindGdkRuntimeDll(string fileName)
        {
            foreach (string candidate in GetGdkRuntimeDllCandidates(fileName))
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private IEnumerable<string> GetGdkRuntimeDllCandidates(string fileName)
        {
            string executableDirectory = MainDataModel.Default.FilePaths.ExecutableDirectory;
            string baseDirectory = AppContext.BaseDirectory;

            yield return Path.Combine(executableDirectory, "native", "launchercore", fileName);
            yield return Path.Combine(baseDirectory, "native", "launchercore", fileName);
            yield return Path.Combine(executableDirectory, fileName);
            yield return Path.Combine(baseDirectory, fileName);

            foreach (string sourceDirectory in GetInstalledMinecraftDirectories(VersionType.Release).Concat(GetInstalledMinecraftDirectories(VersionType.Preview)))
                yield return Path.Combine(sourceDirectory, fileName);

            foreach (string vcLibsDirectory in GetVCLibsRuntimeDirectories())
                yield return Path.Combine(vcLibsDirectory, fileName);
        }

        private IEnumerable<string> GetVCLibsRuntimeDirectories()
        {
            foreach (string windowsAppsDirectory in GetWindowsAppsDirectories())
            {
                foreach (string directory in EnumerateDirectoriesSafe(windowsAppsDirectory, $"Microsoft.VCLibs.140.00_*_{Constants.CurrentArchitecture}__8wekyb3d8bbwe")
                    .Concat(EnumerateDirectoriesSafe(windowsAppsDirectory, $"Microsoft.VCLibs.140.00.UWPDesktop_*_{Constants.CurrentArchitecture}__8wekyb3d8bbwe"))
                    .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
                {
                    yield return directory;
                }
            }
        }

        private IEnumerable<string> GetWindowsAppsDirectories()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
                yield return Path.Combine(programFiles, "WindowsApps");

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                yield return Path.Combine(drive.RootDirectory.FullName, "Program Files", "WindowsApps");
            }
        }

        private IEnumerable<string> GetInstalledMinecraftDirectories(VersionType type)
        {
            List<string> directories = new List<string>();
            string releaseFolderName = type == VersionType.Preview ? "Minecraft Preview" : "Minecraft for Windows";
            string packagePrefix = type == VersionType.Preview ? "Microsoft.MinecraftWindowsBeta_" : "Microsoft.MinecraftUWP_";

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;

                string root = drive.RootDirectory.FullName;
                directories.Add(Path.Combine(root, "XboxGames", releaseFolderName, "Content"));
                directories.Add(Path.Combine(root, "Program Files", "ModifiableWindowsApps", releaseFolderName));

                string windowsApps = Path.Combine(root, "Program Files", "WindowsApps");
                directories.AddRange(EnumerateDirectoriesSafe(windowsApps, packagePrefix + "*"));
            }

            try
            {
                foreach (Package package in PM.FindPackagesForUser(string.Empty, Constants.GetPackageFamily(type)))
                {
                    string installedLocation = package?.InstalledLocation?.Path;
                    if (!string.IsNullOrWhiteSpace(installedLocation))
                        directories.Add(installedLocation);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Unable to enumerate installed Minecraft package directories: {ex}");
            }

            return directories
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> EnumerateDirectoriesSafe(string path, string pattern)
        {
            try
            {
                if (!Directory.Exists(path)) return Enumerable.Empty<string>();
                return Directory.EnumerateDirectories(path, pattern).ToList();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        private async Task WriteLauncherVersionMetadata(MCVersion v, bool registered, CancellationToken cancellationToken)
        {
            var metadata = new
            {
                name = v.VersionFolderName,
                gameVersion = v.Name,
                type = v.Type.ToString().ToLowerInvariant(),
                enableIsolation = true,
                enableConsole = false,
                enableEditorMode = false,
                enableRenderDragon = false,
                enableCtrlRReloadResources = false,
                launchArgs = string.Empty,
                envVars = string.Empty,
                createdAt = DateTimeOffset.Now,
                registered
            };

            string metadataPath = Path.Combine(v.GameDirectory, "version.json");
            string json = JsonConvert.SerializeObject(metadata, Formatting.Indented);
            await File.WriteAllTextAsync(metadataPath, json, cancellationToken);
        }

        private async Task EnsureLauncherGdkManifest(MCVersion v, string packageDirectory)
        {
            string configPath = Path.Combine(packageDirectory, "MicrosoftGame.Config");
            XDocument gameConfig = XDocument.Load(configPath);
            XElement identity = gameConfig.Descendants().FirstOrDefault(x => x.Name.LocalName == "Identity");
            XElement shellVisuals = gameConfig.Descendants().FirstOrDefault(x => x.Name.LocalName == "ShellVisuals");
            XElement executable = gameConfig.Descendants().FirstOrDefault(x =>
                x.Name.LocalName == "Executable" &&
                string.Equals((string)x.Attribute("TargetDeviceFamily"), "PC", StringComparison.OrdinalIgnoreCase));

            string executableName = ((string)executable?.Attribute("Name"))?.Trim();
            string applicationId = ((string)executable?.Attribute("Id"))?.Trim();

            if (string.IsNullOrWhiteSpace(executableName) || string.IsNullOrWhiteSpace(applicationId))
                throw new InvalidDataException("MicrosoftGame.Config does not contain a valid PC executable.");

            string existingManifestPath = Directory.EnumerateFiles(packageDirectory, "AppxManifest.xml", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(packageDirectory, "appxmanifest.xml", SearchOption.TopDirectoryOnly))
                .FirstOrDefault();

            XDocument manifest = !string.IsNullOrWhiteSpace(existingManifestPath)
                ? XDocument.Load(existingManifestPath)
                : BuildLauncherGdkManifest(gameConfig, identity, shellVisuals, executableName, applicationId);

            XElement app = manifest.Descendants().FirstOrDefault(x =>
                x.Name.LocalName == "Application" &&
                string.Equals((string)x.Attribute("Id"), applicationId, StringComparison.OrdinalIgnoreCase))
                ?? manifest.Descendants().FirstOrDefault(x => x.Name.LocalName == "Application");

            if (app == null)
                throw new InvalidDataException("AppxManifest.xml does not contain an Application entry.");

            app.SetAttributeValue("Id", applicationId);
            app.SetAttributeValue("Executable", executableName);
            app.SetAttributeValue("EntryPoint", "Windows.FullTrustApplication");

            XElement manifestIdentity = manifest.Descendants().FirstOrDefault(x => x.Name.LocalName == "Identity");
            if (manifestIdentity != null && identity != null)
            {
                foreach (string attributeName in new[] { "Name", "Publisher", "Version" })
                {
                    string value = ((string)identity.Attribute(attributeName))?.Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                        manifestIdentity.SetAttributeValue(attributeName, value);
                }
            }

            if (!VersionDbExtensions.DoesVerionArchMatch(Constants.CurrentArchitecture, (string)manifestIdentity?.Attribute("ProcessorArchitecture")))
                manifestIdentity?.SetAttributeValue("ProcessorArchitecture", Constants.CurrentArchitecture);

            await File.WriteAllTextAsync(v.ManifestPath, manifest.ToString(SaveOptions.DisableFormatting));
        }

        private XDocument BuildLauncherGdkManifest(XDocument gameConfig, XElement identity, XElement shellVisuals, string executableName, string applicationId)
        {
            if (identity == null || shellVisuals == null)
                throw new InvalidDataException("MicrosoftGame.Config is missing identity or visual metadata.");

            XNamespace appx = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
            XNamespace uap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
            XNamespace rescap = "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities";
            XNamespace desktop6 = "http://schemas.microsoft.com/appx/manifest/desktop/windows10/6";

            var resources = gameConfig.Descendants()
                .Where(x => x.Name.LocalName == "Resource")
                .Select(x => ((string)x.Attribute("Language"))?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (resources.Count == 0)
                resources.Add("en-us");

            string displayName = ((string)shellVisuals.Attribute("DefaultDisplayName"))?.Trim() ?? "Minecraft for Windows";
            string publisherDisplayName = ((string)shellVisuals.Attribute("PublisherDisplayName"))?.Trim() ?? "Microsoft Studios";
            string description = ((string)shellVisuals.Attribute("Description"))?.Trim() ?? displayName;
            string storeLogo = ((string)shellVisuals.Attribute("StoreLogo"))?.Trim() ?? "StoreLogo.png";
            string logo150 = ((string)shellVisuals.Attribute("Square150x150Logo"))?.Trim() ?? "Logo.png";
            string logo44 = ((string)shellVisuals.Attribute("Square44x44Logo"))?.Trim() ?? "SmallLogo.png";
            string splash = ((string)shellVisuals.Attribute("SplashScreenImage"))?.Trim() ?? "MCSplashScreen.png";
            string foreground = ((string)shellVisuals.Attribute("ForegroundText"))?.Trim() ?? "light";
            string background = ((string)shellVisuals.Attribute("BackgroundColor"))?.Trim() ?? "transparent";

            var packageElement = new XElement(appx + "Package",
                new XAttribute(XNamespace.Xmlns + "uap", uap),
                new XAttribute(XNamespace.Xmlns + "rescap", rescap),
                new XAttribute(XNamespace.Xmlns + "desktop6", desktop6),
                new XAttribute("IgnorableNamespaces", "uap rescap desktop6"),
                new XElement(appx + "Identity",
                    new XAttribute("Name", ((string)identity.Attribute("Name"))?.Trim() ?? "MICROSOFT.MINECRAFTUWP"),
                    new XAttribute("Publisher", ((string)identity.Attribute("Publisher"))?.Trim() ?? "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US"),
                    new XAttribute("Version", ((string)identity.Attribute("Version"))?.Trim() ?? "1.0.0.0"),
                    new XAttribute("ProcessorArchitecture", Constants.CurrentArchitecture)),
                new XElement(appx + "Properties",
                    new XElement(appx + "DisplayName", displayName),
                    new XElement(appx + "PublisherDisplayName", publisherDisplayName),
                    new XElement(appx + "Logo", storeLogo),
                    new XElement(appx + "Description", description),
                    new XElement(desktop6 + "RegistryWriteVirtualization", "disabled"),
                    new XElement(desktop6 + "FileSystemWriteVirtualization", "disabled")),
                new XElement(appx + "Dependencies",
                    new XElement(appx + "TargetDeviceFamily",
                        new XAttribute("Name", "Windows.Desktop"),
                        new XAttribute("MinVersion", "10.0.18362.0"),
                        new XAttribute("MaxVersionTested", "10.0.18362.0")),
                    new XElement(appx + "PackageDependency",
                        new XAttribute("Name", "Microsoft.VCLibs.140.00.UWPDesktop"),
                        new XAttribute("MinVersion", "14.0.33728.0"),
                        new XAttribute("Publisher", "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US"))),
                new XElement(appx + "Resources", resources.Select(resource => new XElement(appx + "Resource", new XAttribute("Language", resource)))),
                new XElement(appx + "Applications",
                    new XElement(appx + "Application",
                        new XAttribute("Id", applicationId),
                        new XAttribute("Executable", executableName),
                        new XAttribute("EntryPoint", "Windows.FullTrustApplication"),
                        new XElement(uap + "VisualElements",
                            new XAttribute("DisplayName", displayName),
                            new XAttribute("Square150x150Logo", logo150),
                            new XAttribute("Square44x44Logo", logo44),
                            new XAttribute("Description", description),
                            new XAttribute("ForegroundText", foreground),
                            new XAttribute("BackgroundColor", background),
                            new XElement(uap + "SplashScreen", new XAttribute("Image", splash))))),
                new XElement(appx + "Capabilities",
                    new XElement(appx + "Capability", new XAttribute("Name", "internetClient")),
                    new XElement(rescap + "Capability", new XAttribute("Name", "runFullTrust")),
                    new XElement(rescap + "Capability", new XAttribute("Name", "appLicensing")),
                    new XElement(rescap + "Capability", new XAttribute("Name", "unvirtualizedResources"))));

            return new XDocument(new XDeclaration("1.0", "UTF-8", null), packageElement);
        }

        private Task EnsureGdkLauncherVersionRegistered(MCVersion v)
        {
            Trace.WriteLine($"Skipping GDK registration for isolated launcher version: {v.DisplayName}");
            return Task.CompletedTask;
        }

        private async Task DownloadPackage(MCVersion v, string dlPath, CancellationTokenSource cancelSource)
        {
            try
            {
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

                if (v.PackageType == PackageType.GDK)
                {
                    await RegisterLoosePackageWithPowerShell(v.ManifestPath);
                    Trace.WriteLine("GDK package registration completed through loose package PowerShell path.");
                    return;
                }

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
            string escapedManifestPath = EscapePowerShellSingleQuotedString(Path.GetFullPath(manifestPath));
            string command = $"Add-AppxPackage -ForceApplicationShutdown -Register '{escapedManifestPath}'";
            await RunPowerShellPackageCommand(command, "loose package registration");
        }

        private async Task RemovePackageWithPowerShell(string packageFullName)
        {
            string escapedPackageName = EscapePowerShellSingleQuotedString(packageFullName);
            string command = $"Remove-AppxPackage -Package '{escapedPackageName}' -PreserveRoamableApplicationData";
            await RunPowerShellPackageCommand(command, "package removal");
        }

        private async Task RunPowerShellPackageCommand(string command, string operationName)
        {
            string powershellPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"WindowsPowerShell\v1.0\powershell.exe");

            using Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = powershellPath,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    ArgumentList =
                    {
                        "-NoProfile",
                        "-NonInteractive",
                        "-ExecutionPolicy",
                        "Bypass",
                        "-Command",
                        command
                    }
                }
            };

            if (!process.Start())
                throw new InvalidOperationException($"Could not start PowerShell for {operationName}.");

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            string output = await outputTask;
            string error = await errorTask;

            if (process.ExitCode != 0)
            {
                string details = string.Join(Environment.NewLine, new[] { output, error }.Where(x => !string.IsNullOrWhiteSpace(x)));
                throw new InvalidOperationException($"{operationName} failed with exit code {process.ExitCode}. {details}");
            }
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

            if (v.PackageType != PackageType.GDK)
                yield break;

            string legacyBackupDirectory = Path.Combine(MainDataModel.Default.FilePaths.VersionsFolder, "AppxBackups");
            foreach (string extension in MinecraftPackageExtensions)
                yield return Path.Combine(legacyBackupDirectory, GetMinecraftPackageFileName(v, extension));
        }

        private string GetPackageCacheDirectory(MCVersion v)
        {
            if (v.PackageType == PackageType.GDK)
                return MainDataModel.Default.FilePaths.InstallersFolder;

            return Path.Combine(MainDataModel.Default.FilePaths.VersionsFolder, "AppxBackups");
        }

        private string GetMinecraftPackageFileName(MCVersion v, string extension)
        {
            return "Minecraft-" + v.Name + extension;
        }

        private string EnsureDeploymentPackageExtension(MCVersion v, string pkgPath)
        {
            string absolutePackagePath = Path.GetFullPath(pkgPath);
            PackagePayloadKind payloadKind = GetPackagePayloadKind(v, absolutePackagePath, out _);
            string extension = Path.GetExtension(absolutePackagePath);

            if (payloadKind != PackagePayloadKind.StorePackage || IsDeploymentPackageExtension(extension))
                return absolutePackagePath;

            string deploymentPath = Path.ChangeExtension(absolutePackagePath, ".msixvc");
            if (!absolutePackagePath.Equals(deploymentPath, StringComparison.OrdinalIgnoreCase))
            {
                SafeDeleteFile(deploymentPath);
                File.Move(absolutePackagePath, deploymentPath);
                absolutePackagePath = deploymentPath;
            }

            return absolutePackagePath;
        }

        private bool IsDeploymentPackageExtension(string extension)
        {
            return extension.Equals(".msixvc", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".msix", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".msixbundle", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".appxbundle", StringComparison.OrdinalIgnoreCase);
        }

        private bool LooksLikeTextResponse(string path, out string problem)
        {
            problem = string.Empty;

            try
            {
                byte[] buffer = new byte[Math.Min(1024, (int)new FileInfo(path).Length)];
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

                if (new FileInfo(path).Length < 64 * 1024)
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
                        await RemovePackageWithPowerShell(pkg.Id.FullName);
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
                    Trace.WriteLine("Deployment failed: " + v.GetResults().ErrorText);
                    src.SetException(new Exception("Deployment failed: " + v.GetResults().ErrorText));
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
            if (e.GetType() == typeof(PackageExtractionFailedException) && IsNativeGdkExtractionFailure(e)) SetNativeGdkExtractionError(e);
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

            void SetNativeGdkExtractionError(Exception ex)
            {
                Trace.WriteLine("Native GDK extraction failed:\n" + ex.ToString());
                _ = ErrorScreenShow.exceptionmsg("Extraction failed", new Exception(GetNativeGdkExtractionMessage(ex)));
            }

            void SetError(Exception ex2, string debugMessage, string dialogTitle, string dialogText)
            {
                Trace.WriteLine(debugMessage + ":\n" + ex2.ToString());
                MainDataModel.BackwardsCommunicationHost.errormsg(dialogTitle, dialogText, ex2);
            }
        }

        private static bool IsNativeGdkExtractionFailure(Exception exception)
        {
            string details = exception?.ToString() ?? string.Empty;
            return details.Contains("Native MSIXVC extraction failed", StringComparison.OrdinalIgnoreCase) ||
                details.Contains("NH_ERR_UNAUTHORIZED_CALLER", StringComparison.OrdinalIgnoreCase) ||
                details.Contains("Importable extracted versions currently found", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetNativeGdkExtractionMessage(Exception exception)
        {
            string message = exception?.InnerException?.Message ?? exception?.Message ?? string.Empty;
            if (string.IsNullOrWhiteSpace(message))
            {
                return "This GDK package could not be extracted locally. Your Microsoft Store Minecraft install was not modified.";
            }

            return message;
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
