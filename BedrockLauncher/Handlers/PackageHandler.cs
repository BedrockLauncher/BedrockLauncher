using BedrockLauncher.Classes;
using BedrockLauncher.Classes;
using BedrockLauncher.Classes.SkinPack;
using BedrockLauncher.Components;
using BedrockLauncher.Methods;
using BedrockLauncher.Pages.Common;
using BedrockLauncher.Downloaders;
using ExtensionsDotNET;
using SymbolicLinkSupport;
using System;
using System.Collections.Generic;
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
using ZipProgress = ExtensionsDotNET.ZipFileExtensions.ZipProgress;
using BedrockLauncher.Enums;
using System.Windows.Input;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Handlers
{
    public class PackageHandler
    {
        public VersionDownloader VersionDownloader { get; private set; } = new VersionDownloader();
        private CancellationTokenSource CancelSource = new CancellationTokenSource();
        private readonly Task UserAuthorizationTask;
        private volatile int UserAuthorizationTaskStarted;

        public PackageHandler()
        {
            UserAuthorizationTask = new Task(VersionDownloader.EnableUserAuthorization);
        }

        public Process GameHandle { get; private set; } = null;
        public bool isGameRunning { get => GameHandle != null; }


        public async Task<bool> DownloadAndExtractPackage(BLVersion v)
        {
            DebugLog("Download start");
            bool wasCanceled = false;

            CancelSource = new CancellationTokenSource();
            UpdateProgressBar(show: true);
            UpdateProgressBarCanceling(true, new RelayCommand((o) => CancelTask()));

            try
            {
                string dlPath = "Minecraft-" + v.Name + ".Appx";
                await DownloadPackage(v, dlPath, CancelSource);
                await ExtractPackage(v, dlPath, CancelSource);
            }
            catch (TaskCanceledException)
            {
                wasCanceled = true;
            }
            catch (Exception ex)
            {
                ErrorScreenShow.exceptionmsg(ex);
                wasCanceled = true;
            }

            if (wasCanceled) UpdateProgressBar(show: false, state: LauncherStateChange.None);
            UpdateProgressBarCanceling(false, null);
            CancelSource = null;
            v.UpdateInstallStatus();

            return wasCanceled;
        }
        public async Task<bool> PreparePackage(BLVersion v, string dirPath)
        {
            try
            {
                await Task.Run(() => RedirectSaveData(dirPath));
                await UnregisterPackage(v, true);
                await RegisterPackage(v);
                return true;
            }
            catch
            {
                return false;
            }

            void RedirectSaveData(string InstallationsFolderPath)
            {
                try
                {
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                    string LocalStateFolder = Path.Combine(localAppData, "Packages", Constants.MINECRAFT_PACKAGE_FAMILY, "LocalState");
                    string PackageFolder = Path.Combine(localAppData, "Packages", Constants.MINECRAFT_PACKAGE_FAMILY, "LocalState", "games", "com.mojang");
                    string PackageBakFolder = Path.Combine(localAppData, "Packages", Constants.MINECRAFT_PACKAGE_FAMILY, "LocalState", "games", "com.mojang.default");
                    string ProfileFolder = Path.GetFullPath(InstallationsFolderPath);

                    if (Directory.Exists(PackageFolder))
                    {
                        var dir = new DirectoryInfo(PackageFolder);
                        if (!dir.IsSymbolicLink()) dir.MoveTo(PackageBakFolder);
                        else dir.Delete(true);
                    }

                    DirectoryInfo profileDir = Directory.CreateDirectory(ProfileFolder);
                    SymLinkHelper.CreateSymbolicLink(PackageFolder, ProfileFolder, SymLinkHelper.SymbolicLinkType.Directory);
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
                    profileSecurity.SetOwner(owner);
                    profileSecurity.AddAccessRule(au_access_rules);
                    profileSecurity.AddAccessRule(owner_access_rules);
                    needed_rules.ForEach(x => profileSecurity.AddAccessRule(x));
                    profileDir.SetAccessControl(profileSecurity);
                }
                catch (Exception e)
                {
                    ErrorScreenShow.exceptionmsg(e);
                    throw e;
                }
            }
        }
        public async Task LaunchPackage(BLVersion v, string dirPath, bool KeepLauncherOpen)
        {
            try
            {
                UpdateProgressBar(state: LauncherStateChange.isLaunching, show: true);
                bool success = await PreparePackage(v, dirPath);
                if (success)
                {
                    var pkg = await AppDiagnosticInfo.RequestInfoForPackageAsync(Constants.MINECRAFT_PACKAGE_FAMILY);
                    AppActivationResult activationResult = null;
                    if (pkg.Count > 0) activationResult = await pkg[0].LaunchAsync();
                    DebugLog("App launch finished!");
                    if (KeepLauncherOpen && activationResult != null) UpdatePackageHandle(activationResult);
                    if (KeepLauncherOpen == false) await Application.Current.Dispatcher.InvokeAsync(() => Application.Current.MainWindow.Close());
                    else UpdateProgressBar(state: LauncherStateChange.None, show: false);
                }
            }
            catch (Exception e)
            {
                SetError(e, "App launch failed", "Error_AppLaunchFailed_Title", "Error_AppLaunchFailed");
                UpdateProgressBar(state: LauncherStateChange.None, show: false);
            }


            void UpdatePackageHandle(AppActivationResult app)
            {
                try
                {
                    var result = app.AppResourceGroupInfo.GetProcessDiagnosticInfos();
                    if (result != null && result.Count > 0) return;

                    UpdateProgressBar(isGameRunning: true);

                    var ProcessId = (int)result.First().ProcessId;
                    GameHandle = Process.GetProcessById(ProcessId);
                    GameHandle.EnableRaisingEvents = true;
                    GameHandle.Exited += OnPackageExit;
                }
                catch (Exception ex)
                {
                    DebugLog(ex.ToString());
                    throw ex;
                }
            }

            void OnPackageExit(object sender, EventArgs e)
            {
                Process p = sender as Process;
                p.Exited -= OnPackageExit;
                GameHandle = null;
                UpdateProgressBar(isGameRunning: false);
            }
        }
        public async Task ClosePackage()
        {
            if (GameHandle != null)
            {
                var title = Application.Current.FindResource("Dialog_KillGame_Title") as string;
                var content = Application.Current.FindResource("Dialog_KillGame_Text") as string;

                var result = await DialogPrompt.ShowDialog_YesNo(title, content);

                if (result == System.Windows.Forms.DialogResult.Yes) GameHandle.Kill();
            }
        }
        public async Task DownloadPackage(BLVersion v, string dlPath, CancellationTokenSource cancelSource)
        {
            try
            {
                if (v.IsBeta)
                {
                    if (Interlocked.CompareExchange(ref UserAuthorizationTaskStarted, 1, 0) == 0) UserAuthorizationTask.Start();
                    DebugLog("Waiting for authentication");
                    try
                    {
                        await UserAuthorizationTask;
                        DebugLog("Authentication complete");
                    }
                    catch (Exception e)
                    {
                        SetError(e, "Authentication failed", "Error_AuthenticationFailed_Title", "Error_AuthenticationFailed");
                        throw new TaskCanceledException();
                    }
                }
                UpdateProgressBar(state: LauncherStateChange.isInitializing);
                await VersionDownloader.Download(v.DisplayName, v.UUID, 1, dlPath, (x, y) => DownloadProgressWrapper(x, y), cancelSource.Token);
                DebugLog("Download complete");
                UpdateProgressBar(state: LauncherStateChange.None);
            }
            catch (TaskCanceledException e)
            {
                UpdateProgressBar(state: LauncherStateChange.None);
                throw e;
            }
            catch (Exception e)
            {
                UpdateProgressBar(state: LauncherStateChange.None);
                SetError(e, "Download failed", "Error_AppDownloadFailed_Title", "Error_AppDownloadFailed");
                throw e;
            }
        }
        public async Task RemovePackage(BLVersion v)
        {
            UpdateProgressBar(show: true, state: LauncherStateChange.isUninstalling);

            try
            {
                await UnregisterPackage(v);
                Directory.Delete(v.GameDirectory, true);
            }
            catch (Exception ex) { ErrorScreenShow.exceptionmsg(ex); }

            v.UpdateInstallStatus();
            UpdateProgressBar(show: false, state: LauncherStateChange.None);
        }
        public async Task RegisterPackage(BLVersion v)
        {
            try
            {
                DebugLog("Registering package");
                UpdateProgressBar(deploymentPackageName: v.GetPackageNameFromMainifest(), state: LauncherStateChange.isRegisteringPackage);
                await DeploymentProgressWrapper(new PackageManager().RegisterPackageAsync(new Uri(v.ManifestPath), null, DeploymentOptions.DevelopmentMode));
                UpdateProgressBar(deploymentPackageName: "", state: LauncherStateChange.None);
                DebugLog("App re-register done!");
            }
            catch (Exception e)
            {
                SetError(e, "App re-register failed", "Error_AppReregisterFailed_Title", "Error_AppReregisterFailed");
                throw e;
            }

        }
        public async Task ExtractPackage(BLVersion v, string dlPath, CancellationTokenSource cancelSource)
        {
            try
            {
                DebugLog("Extraction started");
                UpdateProgressBar(progress: 0, state: LauncherStateChange.isExtracting);

                if (Directory.Exists(v.GameDirectory)) Directory.Delete(v.GameDirectory, true);

                var progress = new Progress<ZipProgress>();
                progress.ProgressChanged += (s, z) => UpdateProgressBar(progress: z.Processed, totalProgress: z.Total);
                await Task.Run(() => new ZipArchive(File.OpenRead(dlPath)).ExtractToDirectory(v.GameDirectory, progress, cancelSource));

                File.Delete(Path.Combine(v.GameDirectory, "AppxSignature.p7x"));
                File.Delete(dlPath);

                DebugLog("Extracted successfully");
            }
            catch (TaskCanceledException e)
            {
                Directory.Delete(v.GameDirectory, true);
                throw e;
            }
            catch (Exception e)
            {
                SetError(e, "Extraction failed", "Error_AppExtractionFailed_Title", "Error_AppExtractionFailed");
                UpdateProgressBar(state: LauncherStateChange.None);
                throw new TaskCanceledException();
            }
        }
        public async Task UnregisterPackage(BLVersion v, bool keepVersion = false)
        {
            foreach (var pkg in new PackageManager().FindPackages(Constants.MINECRAFT_PACKAGE_FAMILY))
            {
                string location = GetPackagePath(pkg);
                if (location == v.GameDirectory && keepVersion)
                {
                    DebugLog("Skipping package removal - same path: " + pkg.Id.FullName + " " + location);
                    continue;
                }

                DebugLog("Removing package: " + pkg.Id.FullName);

                UpdateProgressBar(deploymentPackageName: pkg.Id.FullName, state: LauncherStateChange.isRemovingPackage);
                await DeploymentProgressWrapper(new PackageManager().RemovePackageAsync(pkg.Id.FullName, RemovalOptions.PreserveApplicationData | RemovalOptions.RemoveForAllUsers));
                UpdateProgressBar(deploymentPackageName: "", state: LauncherStateChange.None);

                DebugLog("Removal of package done: " + pkg.Id.FullName);
            }

            string GetPackagePath(Package pkg)
            {
                try { return pkg.InstalledLocation.Path; }
                catch (FileNotFoundException) { return ""; }
            }
        }

        public void CancelTask()
        {
            if (CancelSource != null && !CancelSource.IsCancellationRequested) CancelSource.Cancel();
        }


        #region Extensions

        protected async Task DeploymentProgressWrapper(IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> t)
        {
            TaskCompletionSource<int> src = new TaskCompletionSource<int>();
            t.Progress += (v, p) =>
            {
                DebugLog("Deployment progress: " + p.state + " " + p.percentage + "%");
                if (Properties.LauncherSettings.Default.ShowAdvancedInstallDetails)
                    UpdateProgressBar(progress: Convert.ToInt64(p.percentage), totalProgress: UserInterfaceHandler.DeploymentMaximum);

            };
            t.Completed += (v, p) =>
            {
                if (p == AsyncStatus.Error)
                {
                    DebugLog("Deployment failed: " + v.GetResults().ErrorText);
                    src.SetException(new Exception("Deployment failed: " + v.GetResults().ErrorText));
                }
                else
                {
                    DebugLog("Deployment done: " + p);
                    src.SetResult(1);
                }
            };
            await src.Task;
        }
        protected void DownloadProgressWrapper(long current, long? total)
        {
            if (MainViewModel.Default.ProgressBarState.ProgressBar_CurrentState == LauncherStateChange.isInitializing)
            {
                DebugLog("Actual download started");
                UpdateProgressBar(state: LauncherStateChange.isDownloading, totalProgress: total, progress: current);
            }
            UpdateProgressBar(state: LauncherStateChange.isDownloading, totalProgress: total, progress: current);
        }
        protected void UpdateProgressBar(string deploymentPackageName = null, LauncherStateChange? state = null, long? progress = null, long? totalProgress = null, bool? show = null, bool? isGameRunning = null)
        {
            if (deploymentPackageName != null) MainViewModel.Default.ProgressBarState.DeploymentPackageName = deploymentPackageName;
            if (state != null) MainViewModel.Default.ProgressBarState.ProgressBar_CurrentState = state.Value;
            if (progress != null) MainViewModel.Default.ProgressBarState.ProgressBar_CurrentProgress = progress.Value;
            if (totalProgress != null) MainViewModel.Default.ProgressBarState.ProgressBar_TotalProgress = totalProgress.Value;
            if (show != null) MainViewModel.Default.ProgressBarState.ProgressBar_Show = show.Value;
            if (isGameRunning != null) MainViewModel.Default.ProgressBarState.IsGameRunning = isGameRunning.Value;
        }
        protected void UpdateProgressBarCanceling(bool allowCancel, ICommand cancelCommand)
        {
            MainViewModel.Default.ProgressBarState.AllowCancel = allowCancel;
            MainViewModel.Default.ProgressBarState.CancelCommand = cancelCommand;
        }
        protected void SetError(Exception e, string debugMessage, string dialogTitle, string dialogText)
        {
            DebugLog(debugMessage + ":\n" + e.ToString());
            ErrorScreenShow.errormsg(dialogTitle, dialogText, e);
        }
        protected void DebugLog(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        #endregion


    }
}
