using BedrockLauncher.Classes;
using BedrockLauncher.Classes.SkinPack;
using BedrockLauncher.Extensions;
using BedrockLauncher.Pages.Common;
using BedrockLauncher.Downloaders;
using Extensions;
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
using ZipProgress = Extensions.ZipFileExtensions.ZipProgress;
using BedrockLauncher.Enums;
using System.Windows.Input;
using BedrockLauncher.ViewModels;
using BedrockLauncher.Exceptions;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.UI.Components;
using BedrockLauncher.UpdateProcessor;
using BedrockLauncher.UpdateProcessor.Authentication;
using BedrockLauncher.UpdateProcessor.Handlers;

namespace BedrockLauncher.Handlers
{
    public class PackageHandler : IDisposable
    {
        private CancellationTokenSource CancelSource = new CancellationTokenSource();
        private StoreNetwork StoreNetwork = new StoreNetwork();

        public VersionDownloader VersionDownloader { get; private set; } = new VersionDownloader();
        public Process GameHandle { get; private set; } = null;
        public bool isGameRunning { get => GameHandle != null; }

        #region Public Methods

        public async Task LaunchPackage(BLVersion v, string dirPath, bool KeepLauncherOpen)
        {
            try
            {
                SetTaskState(true);

                if (!v.IsInstalled) await DownloadAndExtractPackage(v);

                await RedirectSaveData(dirPath);
                await UnregisterPackage(v, true);
                await RegisterPackage(v);

                MainViewModel.Default.InterfaceState.UpdateProgressBar(state: LauncherStateChange.isLaunching);

                var pkg = await AppDiagnosticInfo.RequestInfoForPackageAsync(Constants.MINECRAFT_PACKAGE_FAMILY);
                AppActivationResult activationResult = null;
                if (pkg.Count > 0) activationResult = await pkg[0].LaunchAsync();
                DebugLog("App launch finished!");
                if (KeepLauncherOpen && activationResult != null) await UpdatePackageHandle(activationResult);
                if (KeepLauncherOpen == false) await Application.Current.Dispatcher.InvokeAsync(() => Application.Current.MainWindow.Close());

                SetTaskState(false);
            }
            catch (PackageManagerException e)
            {
                SetException(e);
            }
            catch (Exception e)
            {
                SetException(new AppLaunchFailedException(e));
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
        public async Task RemovePackage(BLVersion v)
        {
            try
            {
                SetTaskState(true);

                await UnregisterPackage(v);
                Directory.Delete(v.GameDirectory, true);
                v.UpdateFolderSize();

                SetTaskState(false);
            }
            catch (PackageManagerException e)
            {
                SetException(e);
            }
            catch (Exception ex) 
            {
                SetException(new PackageRemovalFailedException(ex));
            }
        }
        public async Task DownloadPackage(BLVersion v)
        {
            try
            {
                SetTaskState(true);
                await DownloadAndExtractPackage(v);
                SetTaskState(false);
            }
            catch (PackageManagerException e)
            {
                SetException(e);
            }
            catch (Exception e)
            {
                SetException(new PackageDownloadAndExtractFailedException(e));
            }
        }
        public void Cancel()
        {
            if (CancelSource != null && !CancelSource.IsCancellationRequested) CancelSource.Cancel();
        }

        #endregion

        #region Private Throwable Methods

        private async Task DownloadAndExtractPackage(BLVersion v)
        {
            try
            {
                DebugLog("Download start");
                SetCancelation(true);

                string dlPath = "Minecraft-" + v.Name + ".Appx";
                await DownloadPackage(v, dlPath, CancelSource);
                await ExtractPackage(v, dlPath, CancelSource);

                SetCancelation(false);
                CancelSource = null;
                v.UpdateFolderSize();
            }
            catch (PackageManagerException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                throw new Exception("DownloadAndExtractPackage Failed", ex);
            }

        }
        private async Task DownloadPackage(BLVersion v, string dlPath, CancellationTokenSource cancelSource)
        {
            try
            {
                if (v.IsBeta) await AuthenticateBetaUser();
                MainViewModel.Default.InterfaceState.UpdateProgressBar(state: LauncherStateChange.isDownloading);
                await VersionDownloader.DownloadVersion(v.DisplayName, v.UUID, 1, dlPath, (x, y) => DownloadProgressWrapper(x, y), cancelSource.Token);
                DebugLog("Download complete");
                MainViewModel.Default.InterfaceState.UpdateProgressBar(state: LauncherStateChange.None);
            }
            catch (PackageManagerException e)
            {
                throw e;
            }
            catch (TaskCanceledException e)
            {
                throw new PackageDownloadCanceledException(e);
            }
            catch (Exception e)
            {
                throw new PackageDownloadFailedException(e);
            }
        }
        private async Task RegisterPackage(BLVersion v)
        {
            try
            {
                DebugLog("Registering package");
                MainViewModel.Default.InterfaceState.UpdateProgressBar(deploymentPackageName: v.GetPackageNameFromMainifest(), state: LauncherStateChange.isRegisteringPackage);
                await DeploymentProgressWrapper(new PackageManager().RegisterPackageAsync(new Uri(v.ManifestPath), null, DeploymentOptions.DevelopmentMode));
                MainViewModel.Default.InterfaceState.UpdateProgressBar(deploymentPackageName: "", state: LauncherStateChange.None);
                DebugLog("App re-register done!");
            }
            catch (PackageManagerException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new PackageRegistrationFailedException(e);
            }

        }
        private async Task ExtractPackage(BLVersion v, string dlPath, CancellationTokenSource cancelSource)
        {
            try
            {
                DebugLog("Extraction started");
                MainViewModel.Default.InterfaceState.UpdateProgressBar(progress: 0, state: LauncherStateChange.isExtracting);

                if (Directory.Exists(v.GameDirectory)) Directory.Delete(v.GameDirectory, true);

                var fileStream = File.OpenRead(dlPath);
                var progress = new Progress<ZipProgress>();
                progress.ProgressChanged += (s, z) => MainViewModel.Default.InterfaceState.UpdateProgressBar(progress: z.Processed, totalProgress: z.Total);
                await Task.Run(() => new ZipArchive(fileStream).ExtractToDirectory(v.GameDirectory, progress, cancelSource));

                fileStream.Close();
                File.Delete(Path.Combine(v.GameDirectory, "AppxSignature.p7x"));
                File.Delete(dlPath);

                DebugLog("Extracted successfully");
            }
            catch (PackageManagerException e)
            {
                throw e;
            }
            catch (TaskCanceledException e)
            {
                Directory.Delete(v.GameDirectory, true);
                throw new PackageExtractionCanceledException(e);
            }
            catch (Exception e)
            {
                throw new PackageExtractionFailedException(e);
            }
        }
        private async Task UnregisterPackage(BLVersion v, bool keepVersion = false)
        {
            try
            {
                foreach (var pkg in new PackageManager().FindPackages(Constants.MINECRAFT_PACKAGE_FAMILY))
                {
                    string location;

                    try { location = pkg.InstalledLocation.Path; }
                    catch (FileNotFoundException) { location = string.Empty; }

                    if (location == v.GameDirectory && keepVersion)
                    {
                        DebugLog("Skipping package removal - same path: " + pkg.Id.FullName + " " + location);
                        continue;
                    }

                    DebugLog("Removing package: " + pkg.Id.FullName);

                    MainViewModel.Default.InterfaceState.UpdateProgressBar(deploymentPackageName: pkg.Id.FullName, state: LauncherStateChange.isRemovingPackage);
                    await DeploymentProgressWrapper(new PackageManager().RemovePackageAsync(pkg.Id.FullName, RemovalOptions.PreserveApplicationData | RemovalOptions.RemoveForAllUsers));
                    MainViewModel.Default.InterfaceState.UpdateProgressBar(deploymentPackageName: "", state: LauncherStateChange.None);

                    DebugLog("Removal of package done: " + pkg.Id.FullName);
                }
            }
            catch (PackageManagerException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                throw new PackageDeregistrationFailedException(ex);
            }
        }
        private async Task RedirectSaveData(string InstallationsFolderPath)
        {
            await Task.Run(() =>
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
                        bool isSymbolic = dir.IsSymbolicLink();

                        if (!isSymbolic)
                        {
                            int i = 1;
                            var finalString = PackageBakFolder;
                            while (Directory.Exists(finalString)) finalString = $"{PackageBakFolder}.{i++}";
                            dir.MoveTo(finalString);
                        }
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
                System.Diagnostics.Trace.WriteLine("Error while Authenticating UserToken for Version Fetching:\n" + e);
                throw new BetaAuthenticationFailedException(e);
            }
        }

        private async Task UpdatePackageHandle(AppActivationResult app)
        {
            await Task.Run(() =>
            {
                try
                {
                    var result = app.AppResourceGroupInfo.GetProcessDiagnosticInfos();
                    if (result != null && result.Count > 0) return;

                    MainViewModel.Default.InterfaceState.UpdateProgressBar(isGameRunning: true);

                    var ProcessId = (int)result.First().ProcessId;
                    GameHandle = Process.GetProcessById(ProcessId);
                    GameHandle.EnableRaisingEvents = true;
                    GameHandle.Exited += OnPackageExit;
                }
                catch (PackageManagerException e)
                {
                    throw e;
                }
                catch (Exception ex)
                {
                    throw new PackageProcessHookFailedException(ex);
                }
            });


            void OnPackageExit(object sender, EventArgs e)
            {
                Process p = sender as Process;
                p.Exited -= OnPackageExit;
                GameHandle = null;
                MainViewModel.Default.InterfaceState.UpdateProgressBar(isGameRunning: false);
            }
        }

        #endregion

        #region Extensions

        protected async Task DeploymentProgressWrapper(IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> t)
        {
            TaskCompletionSource<int> src = new TaskCompletionSource<int>();
            t.Progress += (v, p) =>
            {
                DebugLog("Deployment progress: " + p.state + " " + p.percentage + "%");
                if (Properties.LauncherSettings.Default.ShowAdvancedInstallDetails)
                    MainViewModel.Default.InterfaceState.UpdateProgressBar(progress: Convert.ToInt64(p.percentage), totalProgress: UserInterfaceModel.DeploymentMaximum);

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
            if (MainViewModel.Default.InterfaceState.ProgressBar_CurrentState == LauncherStateChange.isInitializing)
            {
                DebugLog("Actual download started");
                MainViewModel.Default.InterfaceState.UpdateProgressBar(state: LauncherStateChange.isDownloading, totalProgress: total, progress: current);
            }
            MainViewModel.Default.InterfaceState.UpdateProgressBar(state: LauncherStateChange.isDownloading, totalProgress: total, progress: current);
        }
        protected void SetTaskState(bool activationState)
        {
            bool show = activationState ? true : false;
            LauncherStateChange state = activationState ? LauncherStateChange.isInitializing : LauncherStateChange.None;

            MainViewModel.Default.InterfaceState.UpdateProgressBar(show: show, state: state);
        }
        protected void SetCancelation(bool cancelState)
        {
            if (cancelState) CancelSource = new CancellationTokenSource();
            MainViewModel.Default.InterfaceState.AllowCancel = cancelState ? true : false;
            MainViewModel.Default.InterfaceState.CancelCommand = cancelState ? new RelayCommand((o) => Cancel()) : null;
        }
        protected void SetException(Exception e)
        {
            if (e.GetType() == typeof(PackageExtractionFailedException)) SetError(e, "Extraction failed", "Error_AppExtractionFailed_Title", "Error_AppExtractionFailed");
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
                SetTaskState(false);
            }

            void SetGenericError(Exception ex)
            {
                ErrorScreenShow.exceptionmsg(ex);
                SetTaskState(false);
            }

            void SetError(Exception ex2, string debugMessage, string dialogTitle, string dialogText)
            {
                Trace.WriteLine(debugMessage + ":\n" + ex2.ToString());
                ErrorScreenShow.errormsg(dialogTitle, dialogText, ex2);
                SetTaskState(false);
            }
        }
        protected void DebugLog(string message)
        {
            Trace.WriteLine(message);
        }


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
