using BedrockLauncher.Classes;
using BedrockLauncher.Downloaders;
using BedrockLauncher.Core.Classes;
using BedrockLauncher.Core.Classes.SkinPack;
using BedrockLauncher.Core.Components;
using BedrockLauncher.Core.Controls;
using BedrockLauncher.Core.Methods;
using BedrockLauncher.Core.Pages.Common;
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
using System.Windows.Input;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Core;
using Windows.Management.Deployment;
using Windows.System;
using ZipProgress = ExtensionsDotNET.ZipFileExtensions.ZipProgress;

namespace BedrockLauncher.ViewModels
{
    public class GameManager: NotifyPropertyChangedBase
    {
        #region Threading Tasks

        public CancellationTokenSource cancelSource = new CancellationTokenSource();
        private readonly Task _userVersionDownloaderLoginTask;
        private volatile int _userVersionDownloaderLoginTaskStarted;

        #endregion

        #region Constants

        public static readonly string MINECRAFT_PACKAGE_FAMILY = "Microsoft.MinecraftUWP_8wekyb3d8bbwe";
        public static readonly string MINECRAFT_EXE_NAME = "Minecraft.Windows";

        #endregion

        #region Game Detection

        public Process GameProcess { get; set; } = null;
        public async void GetGameProcess(AppActivationResult v)
        {
            await Task.Run(() =>
            {
                try
                {
                    var result = v.AppResourceGroupInfo.GetProcessDiagnosticInfos();
                    if (result != null && result.Count > 0)
                    {
                        var ProcessId = (int)result.First().ProcessId;
                        var Process = System.Diagnostics.Process.GetProcessById(ProcessId);

                        ViewModels.LauncherModel.Default.IsGameRunning = true;
                        GameProcess = Process;
                        Process.EnableRaisingEvents = true;
                        Process.Exited += GameProcessExited;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    throw ex;
                }



            });
        }
        private void GameProcessExited(object sender, EventArgs e)
        {
            Process p = sender as Process;
            p.Exited -= GameProcessExited;
            GameProcess = null;
            ViewModels.LauncherModel.Default.IsGameRunning = false;

        }

        #endregion

        #region Init

        public GameManager()
        {
            _userVersionDownloaderLoginTask = new Task(() =>
            {
                LauncherModel.Default.VersionDownloader.EnableUserAuthorization();
            });
        }

        #endregion

        #region General Methods
        public async void KillGame()
        {
            if (LauncherModel.Default.GameManager.GameProcess != null)
            {
                var title = Application.Current.FindResource("Dialog_KillGame_Title") as string;
                var content = Application.Current.FindResource("Dialog_KillGame_Text") as string;

                var result = await DialogPrompt.ShowDialog_YesNo(title, content);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    GameProcess.Kill();
                }
            }
        }
        public async void Backup()
        {
            await Task.Run(() =>
            {
                ViewModels.LauncherModel.Default.ShowProgressBar = true;
                ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.isBackingUp;

                try
                {
                    var data = ApplicationDataManager.CreateForPackageFamily(MINECRAFT_PACKAGE_FAMILY);
                    string dataPath;

                    try { dataPath = Path.Combine(data.LocalFolder.Path, "games", "com.mojang"); }
                    catch { dataPath = string.Empty; }

                    if (dataPath != string.Empty)
                    {
                        var dirData = GenerateStrings();
                        if (!Directory.Exists(dirData.Item2)) Directory.CreateDirectory(dirData.Item2);
                        System.Diagnostics.Debug.WriteLine("Moving backup Minecraft data to: " + dirData.Item2);
                        RestoreCopy(dataPath, dirData.Item2);
                        Application.Current.Dispatcher.Invoke(() => LauncherModel.Default.ConfigManager.CreateInstallation(dirData.Item1, null, dirData.Item2));
                    }
                }
                catch (Exception ex) { ErrorScreenShow.exceptionmsg(ex); }

                ViewModels.LauncherModel.Default.ShowProgressBar = false;
                ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.None;
            });

            Tuple<string, string> GenerateStrings(string name = "Recovery Data", string dir = "RecoveryData")
            {
                string recoveryName = name;
                string recoveryDir = dir;
                string directoryPath = string.Empty;
                int i = 1;


                while (i < 100)
                {
                    if (i != 1)
                    {
                        recoveryName = string.Format("{0} ({1})", name, i);
                        recoveryDir = string.Format("{0}_{1}", dir, i);
                    }
                    directoryPath = Path.Combine(LauncherModel.Default.FilepathManager.GetInstallationsFolderPath(LauncherModel.Default.ConfigManager.CurrentProfileName, recoveryDir));
                    if (!Directory.Exists(directoryPath)) break;
                    i++;
                }

                if (i >= 100) throw new Exception("Too many backups made");

                return new Tuple<string, string>(recoveryName, directoryPath);

            }

            void RestoreCopy(string from, string to)
            {
                int Total = Directory.GetFiles(from, "*", SearchOption.AllDirectories).Length;

                ViewModels.LauncherModel.Default.TotalProgress = Total;
                ViewModels.LauncherModel.Default.CurrentProgress = 0;

                RestoreCopy_Step(from, to);
            }

            void RestoreCopy_Step(string from, string to)
            {
                foreach (var f in Directory.EnumerateFiles(from))
                {
                    string ft = Path.Combine(to, Path.GetFileName(f));
                    File.Copy(f, ft);
                    ViewModels.LauncherModel.Default.CurrentProgress += 1;
                }
                foreach (var f in Directory.EnumerateDirectories(from))
                {
                    string tp = Path.Combine(to, Path.GetFileName(f));
                    Directory.CreateDirectory(tp);
                    RestoreCopy_Step(f, tp);
                }
            }
        }
        public async void Repair(BLVersion v)
        {
            await Download(v, false);
        }
        public async void Remove(BLVersion v)
        {
            ViewModels.LauncherModel.Default.ShowProgressBar = true;
            ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.isUninstalling;

            try
            {
                await UnregisterPackage(v, Path.GetFullPath(v.GameDirectory));
                Directory.Delete(v.GameDirectory, true);
            }
            catch (Exception ex)
            {
                ErrorScreenShow.exceptionmsg(ex);
            }

            v.UpdateInstallStatus();
            ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.None;
            ViewModels.LauncherModel.Default.ShowProgressBar = false;
            return;
        }
        public async void Play(MCProfile p, BLInstallation i, bool KeepLauncherOpen, bool Save = true)
        {
            if (i == null) return;

            if (Save)
            {
                Properties.LauncherSettings.Default.CurrentInstallation = LauncherModel.Default.ConfigManager.CurrentInstallations.IndexOf(i);
                Properties.LauncherSettings.Default.Save();
            }


            switch (i.Version.DisplayInstallStatus.ToString())
            {
                case "Not installed":
                    var wasCanceled = await Download(i.Version, true);
                    if (!wasCanceled) Launch();
                    break;
                case "Installed":
                    Launch();
                    break;
            }


            async void Launch()
            {
                var v = i.Version;
                try
                {
                    StartLaunch();
                    await SetInstallationDataPath(p, i);
                    string gameDir = Path.GetFullPath(v.GameDirectory);
                    await ReRegisterPackage(v, gameDir);
                    bool closable = await LaunchGame(v, KeepLauncherOpen);
                    if (!closable) EndLaunch();
                }
                catch
                {
                    EndLaunch();
                }

                void StartLaunch()
                {
                    ViewModels.LauncherModel.Default.ShowProgressBar = true;
                    ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.isLaunching;
                }

                void EndLaunch()
                {
                    ViewModels.LauncherModel.Default.ShowProgressBar = false;
                    ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.None;
                }
            }
        }
        public void OpenFolder(object i)
        {
            string Directory = string.Empty;

            if (i is BLInstallation) Directory = LauncherModel.Default.FilepathManager.GetInstallationsFolderPath(LauncherModel.Default.ConfigManager.CurrentProfileName, (i as BLInstallation).DirectoryName_Full);
            else if (i is BLVersion) Directory = Path.GetFullPath((i as BLVersion).GameDirectory);
            else if (i is MCSkinPack) Directory = (i as MCSkinPack).Directory;

            if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);
            Process.Start("explorer.exe", Directory);
        }
        public void Cancel()
        {
            if (cancelSource != null && !cancelSource.IsCancellationRequested) cancelSource.Cancel();
        }

        #endregion

        #region Task Methods

        private async Task<bool> Download(BLVersion v, bool RunAfterwards)
        {
            System.Diagnostics.Debug.WriteLine("Download start");
            bool wasCanceled = false;

            cancelSource = new CancellationTokenSource();
            ViewModels.LauncherModel.Default.ShowProgressBar = true;
            ViewModels.LauncherModel.Default.AllowCancel = true;
            ViewModels.LauncherModel.Default.CancelCommand = new RelayCommand((o) => Cancel());

            try
            {
                string dlPath = "Minecraft-" + v.Name + ".Appx";
                VersionDownloader downloader = LauncherModel.Default.VersionDownloader;
                if (v.IsBeta) await BetaAuthenticate();
                await DownloadVersion(v, downloader, dlPath, cancelSource);
                await ExtractPackage(v, dlPath, cancelSource);
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

            if (!RunAfterwards || wasCanceled) ViewModels.LauncherModel.Default.ShowProgressBar = false;
            ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.None;
            ViewModels.LauncherModel.Default.AllowCancel = false;
            ViewModels.LauncherModel.Default.CancelCommand = null;
            cancelSource = null;
            v.UpdateInstallStatus();

            return wasCanceled;


        }
        private async Task DownloadVersion(BLVersion v, VersionDownloader downloader, string dlPath, CancellationTokenSource cancelSource)
        {
            try
            {
                ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.isInitializing;
                await downloader.Download(v.DisplayName, v.UUID, 1, dlPath, (current, total) =>
                {
                    if (ViewModels.LauncherModel.Default.CurrentState == ViewModels.LauncherModel.StateChange.isInitializing)
                    {
                        ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.isDownloading;
                        System.Diagnostics.Debug.WriteLine("Actual download started");
                        if (total.HasValue) ViewModels.LauncherModel.Default.TotalProgress = total.Value;
                    }
                    ViewModels.LauncherModel.Default.CurrentProgress = current;
                }, cancelSource.Token);
                System.Diagnostics.Debug.WriteLine("Download complete");
                ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.None;
            }
            catch (Exception e)
            {
                ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.None;
                System.Diagnostics.Debug.WriteLine("Download failed:\n" + e.ToString());
                throw e;
            }
        }
        private async Task BetaAuthenticate()
        {
            if (Interlocked.CompareExchange(ref _userVersionDownloaderLoginTaskStarted, 1, 0) == 0) _userVersionDownloaderLoginTask.Start();
            System.Diagnostics.Debug.WriteLine("Waiting for authentication");
            try
            {
                await _userVersionDownloaderLoginTask;
                System.Diagnostics.Debug.WriteLine("Authentication complete");
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Authentication failed:\n" + e.ToString());
                ErrorScreenShow.errormsg("autherror");
                throw e;
            }
        }
        private async Task<bool> LaunchGame(MCVersion v, bool KeepLauncherOpen)
        {
            try
            {
                ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.isLaunching;
                var pkg = await AppDiagnosticInfo.RequestInfoForPackageAsync(MINECRAFT_PACKAGE_FAMILY);
                AppActivationResult activationResult = null;
                if (pkg.Count > 0) activationResult = await pkg[0].LaunchAsync();
                System.Diagnostics.Debug.WriteLine("App launch finished!");
                if (KeepLauncherOpen && activationResult != null) GetGameProcess(activationResult);
                if (KeepLauncherOpen == false)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Application.Current.MainWindow.Close();
                    });
                    return true;
                }
                else
                {
                    ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.None;
                    return false;
                }
            }
            catch (Exception e)
            {
                ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.None;
                System.Diagnostics.Debug.WriteLine("App launch failed:\n" + e.ToString());
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ErrorScreenShow.errormsg("applauncherror");
                });
                throw e;
            }
        }
        private async Task SetInstallationDataPath(MCProfile p, BLInstallation i)
        {
            await Task.Run(() =>
            {
                try
                {
                    string LocalStateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", MINECRAFT_PACKAGE_FAMILY, "LocalState");
                    string PackageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", MINECRAFT_PACKAGE_FAMILY, "LocalState", "games", "com.mojang");
                    string PackageBakFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", MINECRAFT_PACKAGE_FAMILY, "LocalState", "games", "com.mojang.default");
                    string ProfileFolder = Path.GetFullPath(LauncherModel.Default.FilepathManager.GetInstallationsFolderPath(p.Name, i.DirectoryName_Full));

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
            });
            Thread.Sleep(1000);
        }
        private async Task ExtractPackage(BLVersion v, string dlPath, CancellationTokenSource cancelSource)
        {
            Stream zipReadingStream = null;
            try
            {
                System.Diagnostics.Debug.WriteLine("Extraction started");
                ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.isExtracting;
                ViewModels.LauncherModel.Default.CurrentProgress = 0;
                string dirPath = v.GameDirectory;
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, true);

                zipReadingStream = File.OpenRead(dlPath);
                ZipArchive zip = new ZipArchive(zipReadingStream);
                var progress = new Progress<ZipProgress>();
                progress.ProgressChanged += (s, z) =>
                {
                    ViewModels.LauncherModel.Default.CurrentProgress = z.Processed;
                    ViewModels.LauncherModel.Default.TotalProgress = z.Total;
                };
                await Task.Run(() => zip.ExtractToDirectory(dirPath, progress, cancelSource));

                zipReadingStream.Close();

                File.Delete(Path.Combine(dirPath, "AppxSignature.p7x"));
                File.Delete(dlPath);
                System.Diagnostics.Debug.WriteLine("Extracted successfully");
            }
            catch (TaskCanceledException e)
            {
                if (zipReadingStream != null) zipReadingStream.Close();
                Directory.Delete(v.GameDirectory, true);
                throw e;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Extraction failed:\n" + e.ToString());
                ErrorScreenShow.exceptionmsg("Extraction failed", e);

                if (zipReadingStream != null) zipReadingStream.Close();
                ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.None;
                throw new TaskCanceledException();
            }
        }
        private async Task RemovePackage(MCVersion v, Package pkg)
        {
            System.Diagnostics.Debug.WriteLine("Removing package: " + pkg.Id.FullName);
            ViewModels.LauncherModel.Default.DeploymentPackageName = pkg.Id.FullName;
            ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.isRemovingPackage;
            if (!pkg.IsDevelopmentMode)
            {
                await DeploymentProgressWrapper(v, new PackageManager().RemovePackageAsync(pkg.Id.FullName, 0));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Package is in development mode");
                await DeploymentProgressWrapper(v, new PackageManager().RemovePackageAsync(pkg.Id.FullName, RemovalOptions.PreserveApplicationData));
            }
            System.Diagnostics.Debug.WriteLine("Removal of package done: " + pkg.Id.FullName);
            ViewModels.LauncherModel.Default.DeploymentPackageName = "";
            ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.None;
        }
        private async Task UnregisterPackage(MCVersion v, string gameDir)
        {
            foreach (var pkg in new PackageManager().FindPackages(MINECRAFT_PACKAGE_FAMILY))
            {
                string location = GetPackagePath(pkg);
                if (location == "" || location == gameDir) await RemovePackage(v, pkg);
            }
        }
        private async Task ReRegisterPackage(MCVersion v, string gameDir)
        {
            try
            {
                foreach (var pkg in new PackageManager().FindPackages(MINECRAFT_PACKAGE_FAMILY))
                {
                    string location = GetPackagePath(pkg);
                    if (location == gameDir)
                    {
                        System.Diagnostics.Debug.WriteLine("Skipping package removal - same path: " + pkg.Id.FullName + " " + location);
                        return;
                    }
                    await RemovePackage(v, pkg);
                }
                System.Diagnostics.Debug.WriteLine("Registering package");
                string manifestPath = Path.Combine(gameDir, "AppxManifest.xml");
                ViewModels.LauncherModel.Default.DeploymentPackageName = GetPackageNameFromMainifest(manifestPath);
                ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.isRegisteringPackage;
                await DeploymentProgressWrapper(v, new PackageManager().RegisterPackageAsync(new Uri(manifestPath), null, DeploymentOptions.DevelopmentMode));
                System.Diagnostics.Debug.WriteLine("App re-register done!");
                ViewModels.LauncherModel.Default.DeploymentPackageName = "";
                ViewModels.LauncherModel.Default.CurrentState = ViewModels.LauncherModel.StateChange.None;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("App re-register failed:\n" + e.ToString());
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ErrorScreenShow.errormsg("appregistererror");
                });
                throw e;
            }

        }
        private async Task DeploymentProgressWrapper(MCVersion version, IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> t)
        {
            TaskCompletionSource<int> src = new TaskCompletionSource<int>();
            t.Progress += (v, p) =>
            {
                System.Diagnostics.Debug.WriteLine("Deployment progress: " + p.state + " " + p.percentage + "%");
                if (Properties.LauncherSettings.Default.ShowAdvancedInstallDetails) ViewModels.LauncherModel.Default.CurrentProgress = Convert.ToInt64(p.percentage);
                if (Properties.LauncherSettings.Default.ShowAdvancedInstallDetails) ViewModels.LauncherModel.Default.TotalProgress = ViewModels.LauncherModel.DeploymentMaximum;
            };
            t.Completed += (v, p) =>
            {
                if (p == AsyncStatus.Error)
                {
                    System.Diagnostics.Debug.WriteLine("Deployment failed: " + v.GetResults().ErrorText);
                    src.SetException(new Exception("Deployment failed: " + v.GetResults().ErrorText));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Deployment done: " + p);
                    src.SetResult(1);
                }
            };
            await src.Task;
        }

        #endregion

        #region Helper Methods

        private string GetPackageNameFromMainifest(string filePath)
        {
            try
            {
                string manifestXml = File.ReadAllText(filePath);
                XDocument XMLDoc = XDocument.Parse(manifestXml);
                var Descendants = XMLDoc.Descendants();
                XElement Identity = Descendants.Where(x => x.Name.LocalName == "Identity").FirstOrDefault();
                string Name = Identity.Attribute("Name").Value;
                string Version = Identity.Attribute("Version").Value;
                string ProcessorArchitecture = Identity.Attribute("ProcessorArchitecture").Value;
                return String.Join("_", Name, Version, ProcessorArchitecture);
            }
            catch
            {
                return "???";
            }
        }
        private string GetPackagePath(Package pkg)
        {
            try
            {
                return pkg.InstalledLocation.Path;
            }
            catch (FileNotFoundException)
            {
                return "";
            }
        }

        #endregion

    }
}
