using BedrockLauncher.Classes;
using BedrockLauncher.Classes.SkinPack;
using BedrockLauncher.Extensions;
using BedrockLauncher.Pages.Common;
using BedrockLauncher.Downloaders;
using JemExtensions;
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
using ZipProgress = JemExtensions.ZipFileExtensions.ZipProgress;
using BedrockLauncher.Enums;
using System.Windows.Input;
using BedrockLauncher.ViewModels;
using BedrockLauncher.Exceptions;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.UI.Components;
using BedrockLauncher.UpdateProcessor;
using BedrockLauncher.UpdateProcessor.Authentication;
using BedrockLauncher.UpdateProcessor.Handlers;
using BedrockLauncher.Classes.Launcher;
using Windows.System.Diagnostics;
using BedrockLauncher.UpdateProcessor.Enums;

namespace BedrockLauncher.Handlers
{
    public class PackageHandler : IDisposable
    {
        private CancellationTokenSource CancelSource = new CancellationTokenSource();
        private StoreNetwork StoreNetwork = new StoreNetwork();
        private PackageManager PM = new PackageManager();

        public VersionDownloader VersionDownloader { get; private set; } = new VersionDownloader();
        public Process GameHandle { get; private set; } = null;
        public bool isGameRunning { get => GameHandle != null; }

        #region Public Methods

        public async Task LaunchPackage(MCVersion v, string dirPath, bool KeepLauncherOpen)
        {
            try
            {
                StartTask();

                if (!v.IsInstalled) await DownloadAndExtractPackage(v);

                await RedirectSaveData(dirPath, v.Type);
                await UnregisterPackage(v, true);
                await RegisterPackage(v);

                MainViewModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isLaunching);

                var pkg = await AppDiagnosticInfo.RequestInfoForPackageAsync(Constants.GetPackageFamily(v.Type));
                AppActivationResult activationResult = null;
                if (pkg.Count > 0) activationResult = await pkg[0].LaunchAsync();
                Trace.WriteLine("App launch finished!");
                if (KeepLauncherOpen && activationResult != null) await UpdatePackageHandle(activationResult);
                if (KeepLauncherOpen == false) await Application.Current.Dispatcher.InvokeAsync(() => Application.Current.MainWindow.Close());
            }
            catch (PackageManagerException e)
            {
                SetException(e);
            }
            catch (Exception e)
            {
                SetException(new AppLaunchFailedException(e));
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
                var title = Application.Current.FindResource("Dialog_KillGame_Title") as string;
                var content = Application.Current.FindResource("Dialog_KillGame_Text") as string;

                var result = await DialogPrompt.ShowDialog_YesNo(title, content);

                if (result == System.Windows.Forms.DialogResult.Yes) GameHandle.Kill();
            }
        }
        public async Task RemovePackage(MCVersion v)
        {
            try
            {
                StartTask();

                MainViewModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isUninstalling);
                await UnregisterPackage(v);
                await DirectoryExtensions.DeleteAsync(v.GameDirectory, (x, y, phase) => ProgressWrapper(x, y, phase), "Files", "Folders");
                v.UpdateFolderSize();
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

        private async Task DownloadAndExtractPackage(MCVersion v)
        {
            try
            {
                Trace.WriteLine("Download start");
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
            }

        }
        private async Task DownloadPackage(MCVersion v, string dlPath, CancellationTokenSource cancelSource)
        {
            try
            {
                if (v.IsBeta) await AuthenticateBetaUser();
                MainViewModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isDownloading);
                Trace.WriteLine("Download starting");
                await VersionDownloader.DownloadVersion(v.DisplayName, v.UUID, 1, dlPath, (x, y) => ProgressWrapper(x, y), cancelSource.Token, v.Type);
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
                MainViewModel.Default.ProgressBarState.SetProgressBarText(v.GetPackageNameFromMainifest());
                MainViewModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isRegisteringPackage);
                await DeploymentProgressWrapper(PM.RegisterPackageAsync(new Uri(v.ManifestPath), null, Constants.PackageDeploymentOptions));
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
        private async Task ExtractPackage(MCVersion v, string dlPath, CancellationTokenSource cancelSource)
        {
            try
            {
                Trace.WriteLine("Extraction started");
                MainViewModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isExtracting);

                if (Directory.Exists(v.GameDirectory))
                    await DirectoryExtensions.DeleteAsync(v.GameDirectory, (x, y, phase) => ProgressWrapper(x, y, phase));

                var fileStream = File.OpenRead(dlPath);
                var progress = new Progress<ZipProgress>();
                progress.ProgressChanged += (s, z) => MainViewModel.Default.ProgressBarState.SetProgressBarProgress(currentProgress: z.Processed, totalProgress: z.Total);
                await Task.Run(() => new ZipArchive(fileStream).ExtractToDirectory(v.GameDirectory, progress, cancelSource));

                fileStream.Close();
                File.Delete(Path.Combine(v.GameDirectory, "AppxSignature.p7x"));
                File.Delete(dlPath);

                Trace.WriteLine("Extracted successfully");
            }
            catch (PackageManagerException e)
            {
                ResetTask();
                throw e;
            }
            catch (TaskCanceledException e)
            {
                MainViewModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isCanceling);
                await DirectoryExtensions.DeleteAsync(v.GameDirectory, (x, y, phase) => ProgressWrapper(x, y, phase));
                ResetTask();
                throw new PackageExtractionCanceledException(e);
            }
            catch (Exception e)
            {
                ResetTask();
                throw new PackageExtractionFailedException(e);
            }
            finally
            {
                ResetTask();
            }
        }
        private async Task UnregisterPackage(MCVersion v, bool keepVersion = false)
        {
            try
            {
                foreach (var pkg in PM.FindPackages(Constants.GetPackageFamily(v.Type)))
                {
                    string location;

                    try { location = pkg.InstalledLocation.Path; }
                    catch (FileNotFoundException) { location = string.Empty; }

                    if (location == v.GameDirectory && keepVersion)
                    {
                        Trace.WriteLine("Skipping package removal - same path: " + pkg.Id.FullName + " " + location);
                        continue;
                    }

                    Trace.WriteLine("Removing package: " + pkg.Id.FullName);

                    MainViewModel.Default.ProgressBarState.SetProgressBarText(pkg.Id.FullName);
                    MainViewModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isRemovingPackage);
                    await DeploymentProgressWrapper(PM.RemovePackageAsync(pkg.Id.FullName, Constants.PackageRemovalOptions));
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
                    List<ProcessDiagnosticInfo> result = app.AppResourceGroupInfo.GetProcessDiagnosticInfos().ToList();
                    if (result == null || result.Count == 0) return;

                    MainViewModel.Default.ProgressBarState.SetGameRunningStatus(true);

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
                MainViewModel.Default.ProgressBarState.SetGameRunningStatus(false);
            }
        }

        #endregion

        #region Helpers

        protected async Task DeploymentProgressWrapper(IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> t)
        {
            TaskCompletionSource<int> src = new TaskCompletionSource<int>();
            t.Progress += (v, p) => MainViewModel.Default.ProgressBarState.SetProgressBarProgress(currentProgress: Convert.ToInt64(p.percentage), totalProgress: 100);
            t.Completed += (v, p) =>
            {
                MainViewModel.Default.ProgressBarState.ResetProgressBarProgress();

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
            MainViewModel.Default.ProgressBarState.SetProgressBarProgress(current, total);
            MainViewModel.Default.ProgressBarState.SetProgressBarText(text);
        }
        protected void ResetTask()
        {
            MainViewModel.Default.ProgressBarState.ResetProgressBarProgress();
            MainViewModel.Default.ProgressBarState.SetProgressBarText();
            MainViewModel.Default.ProgressBarState.SetProgressBarState(LauncherState.None);
        }
        protected void EndTask()
        {
            MainViewModel.Default.ProgressBarState.ResetProgressBarProgress();
            MainViewModel.Default.ProgressBarState.SetProgressBarText();
            MainViewModel.Default.ProgressBarState.SetProgressBarState(LauncherState.None);
            MainViewModel.Default.ProgressBarState.SetProgressBarVisibility(false);
        }
        protected void StartTask()
        {
            MainViewModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isInitializing);
            MainViewModel.Default.ProgressBarState.SetProgressBarVisibility(true);

        }
        protected void SetCancelation(bool cancelState)
        {
            if (cancelState) CancelSource = new CancellationTokenSource();
            MainViewModel.Default.ProgressBarState.AllowCancel = cancelState ? true : false;
            MainViewModel.Default.ProgressBarState.CancelCommand = cancelState ? new RelayCommand((o) => Cancel()) : null;
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
            }

            void SetGenericError(Exception ex)
            {
                ErrorScreenShow.exceptionmsg(ex);
            }

            void SetError(Exception ex2, string debugMessage, string dialogTitle, string dialogText)
            {
                Trace.WriteLine(debugMessage + ":\n" + ex2.ToString());
                ErrorScreenShow.errormsg(dialogTitle, dialogText, ex2);
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
