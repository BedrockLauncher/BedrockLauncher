using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms.Design;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Data;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Core;
using Windows.Management.Deployment;
using Windows.System;
using BedrockLauncher.Interfaces;
using BedrockLauncher.Classes;
using BedrockLauncher.Classes.SkinPack;
using BedrockLauncher.Pages;
using System.Windows.Media.Animation;
using System.Security.AccessControl;
using System.Security;
using System.Security.Principal;
using System.Security.Policy;
using System.Security.RightsManagement;
using System.Security.Permissions;

using MCVersion = BedrockLauncher.Classes.MCVersion;
using System.Xml.Linq;

namespace BedrockLauncher.Methods
{
    public class GameManager: NotifyPropertyChangedBase, ICommonVersionCommands
    {

        #region Threading Tasks

        private readonly VersionDownloader _anonVersionDownloader = new VersionDownloader();
        private readonly VersionDownloader _userVersionDownloader = new VersionDownloader();
        private readonly Task _userVersionDownloaderLoginTask;
        private volatile int _userVersionDownloaderLoginTaskStarted;

        #endregion

        #region Constants

        public static readonly string MINECRAFT_PACKAGE_FAMILY = "Microsoft.MinecraftUWP_8wekyb3d8bbwe";
        public static readonly string MINECRAFT_EXE_NAME = "Minecraft.Windows";

        #endregion

        #region Game Detection

        public Process GameProcess { get; set; } = null;
        public async void GetGameProcess(MCVersion v)
        {
            await Task.Run(() =>
            {
                string FilePath = Path.GetDirectoryName(v.ExePath);
                string FileName = Path.GetFileNameWithoutExtension(v.ExePath).ToLower();

                Process[] pList = Process.GetProcessesByName(FileName);

                foreach (Process p in pList)
                {
                    string fileName = p.MainModule.FileName;
                    if (fileName.StartsWith(FilePath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        ConfigManager.ViewModel.IsGameRunning = true;
                        GameProcess = p;
                        p.EnableRaisingEvents = true;
                        p.Exited += GameProcessExited;
                        break;
                    }
                }


            });
        }
        private void GameProcessExited(object sender, EventArgs e)
        {
            Process p = sender as Process;
            p.Exited -= GameProcessExited;
            GameProcess = null;
            ConfigManager.ViewModel.IsGameRunning = false;

        }

        #endregion

        #region ICommands
        public ICommand LaunchCommand => new RelayCommand((v) => InvokeLaunch((MCVersion)v));
        public ICommand RemoveCommand => new RelayCommand((v) => InvokeRemove((MCVersion)v));
        public ICommand DownloadCommand => new RelayCommand((v) => InvokeDownload((MCVersion)v));

        #endregion

        #region Init

        public GameManager()
        {
            _userVersionDownloaderLoginTask = new Task(() =>
            {
                _userVersionDownloader.EnableUserAuthorization();
            });
        }

        #endregion

        #region General Methods

        public void Remove(MCVersion v)
        {
            InvokeRemove(v);
        }
        public void KillGame()
        {
            InvokeKillGame();
        }
        public void Play(MCInstallation i)
        {
            if (i == null) return;
            Properties.LauncherSettings.Default.CurrentInstallation = ConfigManager.CurrentInstallations.IndexOf(i);
            Properties.LauncherSettings.Default.Save();

            var v = i.Version;
            switch (v.DisplayInstallStatus.ToString())
            {
                case "Not installed":
                    InvokeDownload(v, true);
                    break;
                case "Installed":
                    InvokeLaunch(v);
                    break;
            }
        }
        public void OpenFolder(MCInstallation i)
        {
            string Directory = Filepaths.GetInstallationsFolderPath(ConfigManager.CurrentProfile, i.DirectoryName);
            if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);
            Process.Start("explorer.exe", Directory);
        }
        public void OpenFolder(MCVersion i)
        {
            string Directory = Path.GetFullPath(i.GameDirectory);
            if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);
            Process.Start("explorer.exe", Directory);
        }
        public void OpenFolder(MCSkinPack i)
        {
            Process.Start("explorer.exe", i.Directory);
        }
        public void BackupGameData()
        {
            InvokeBackup();
        }

        #endregion

        #region Invoke Methods

        private void InvokeLaunch(MCVersion v)
        {
            Task.Run(async () =>
            {
                StartLaunch();
                try
                {
                    await SetInstallationDataPath();
                    string gameDir = Path.GetFullPath(v.GameDirectory);
                    await ReRegisterPackage(v, gameDir);
                    await LaunchGame(v);
                }
                catch
                {

                }
                EndLaunch();
            });

            void StartLaunch()
            {
                ConfigManager.ViewModel.ShowProgressBar = true;
                ConfigManager.ViewModel.StateChangeInfo = new VersionStateChangeInfo();
            }

            void EndLaunch()
            {
                ConfigManager.ViewModel.ShowProgressBar = false;
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.None;
                ConfigManager.ViewModel.StateChangeInfo = null;
            }
        }
        private void InvokeDownload(MCVersion v, bool RunAfterwards = false)
        {
            CancellationTokenSource cancelSource = new CancellationTokenSource();
            Program.Log("Download start");

            Task.Run(async () =>
            {
                StartDownload();
                try
                {
                    string dlPath = "Minecraft-" + v.Name + ".Appx";
                    VersionDownloader downloader = _anonVersionDownloader;
                    downloader = _userVersionDownloader;
                    if (v.IsBeta) await BetaAuthenticate();
                    await DownloadVersion(v, downloader, dlPath, cancelSource);
                    await ExtractPackage(v, dlPath);
                }
                catch { }
                EndDownload();
                if (RunAfterwards) InvokeLaunch(v);
            });

            void StartDownload()
            {
                ConfigManager.ViewModel.ShowProgressBar = true;
                ConfigManager.ViewModel.StateChangeInfo = new VersionStateChangeInfo();
                ConfigManager.ViewModel.StateChangeInfo.CancelCommand = new RelayCommand((o) => cancelSource.Cancel());
            }

            void EndDownload()
            {
                if (!RunAfterwards) ConfigManager.ViewModel.ShowProgressBar = false;
                ConfigManager.ViewModel.StateChangeInfo = null;
                v.UpdateInstallStatus();
            }
        }
        private void InvokeRemove(MCVersion v)
        {
            Task.Run(async () =>
            {
                ConfigManager.ViewModel.ShowProgressBar = true;
                ConfigManager.ViewModel.StateChangeInfo = new VersionStateChangeInfo();
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.isUninstalling;

                try
                {
                    await UnregisterPackage(v, Path.GetFullPath(v.GameDirectory));
                    Directory.Delete(v.GameDirectory, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

                v.UpdateInstallStatus();
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.None;
                ConfigManager.ViewModel.ShowProgressBar = false;
                ConfigManager.ViewModel.StateChangeInfo = null;
                return;
            });
        }
        private async void InvokeBackup()
        {
            await Task.Run(() =>
            {
                StartBackup();
                try
                {
                    var data = ApplicationDataManager.CreateForPackageFamily(MINECRAFT_PACKAGE_FAMILY);
                    string dataPath;

                    try { dataPath = data.LocalFolder.Path; }
                    catch { dataPath = string.Empty; }

                    if (dataPath != string.Empty)
                    {
                        string recoveryPath = Path.Combine(Filepaths.GetInstallationsFolderPath(ConfigManager.CurrentProfile, "Recovery_Data"), "LocalState");
                        if (!Directory.Exists(recoveryPath)) Directory.CreateDirectory(recoveryPath);
                        Program.Log("Moving backup Minecraft data to: " + recoveryPath);
                        RestoreCopy(dataPath, recoveryPath);
                        ConfigManager.CreateInstallation("Recovery_Data", null, "Recovery_Data");
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.ToString()); }
                EndBackup();
            });
            ConfigManager.OnConfigStateChanged(this, Events.ConfigStateArgs.Empty);


            void RestoreCopy(string from, string to)
            {
                int Total = Directory.GetFiles(from, "*", SearchOption.AllDirectories).Length;

                ConfigManager.ViewModel.StateChangeInfo.TotalProgress = Total;
                ConfigManager.ViewModel.StateChangeInfo.CurrentProgress = 0;

                RestoreCopy_Step(from, to);
            }

            void RestoreCopy_Step(string from, string to)
            {
                foreach (var f in Directory.EnumerateFiles(from))
                {
                    string ft = Path.Combine(to, Path.GetFileName(f));
                    if (File.Exists(ft))
                    {
                        if (MessageBox.Show(string.Format(Application.Current.FindResource("GameManager_RecoveringDataIssue_FileNotExistant_Text").ToString(), ft), Application.Current.FindResource("GameManager_RecoveringDataIssue_Title").ToString(), MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                            continue;
                        File.Delete(ft);
                    }
                    File.Copy(f, ft);
                    ConfigManager.ViewModel.StateChangeInfo.CurrentProgress += 1;
                }
                foreach (var f in Directory.EnumerateDirectories(from))
                {
                    string tp = Path.Combine(to, Path.GetFileName(f));
                    if (!Directory.Exists(tp))
                    {
                        if (File.Exists(tp) && MessageBox.Show(string.Format(Application.Current.FindResource("GameManager_RecoveringDataIssue_NotaDirectory_Text").ToString(), tp), Application.Current.FindResource("GameManager_RecoveringDataIssue_Title").ToString(), MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                            continue;
                        Directory.CreateDirectory(tp);
                    }
                    RestoreCopy_Step(f, tp);
                }
            }

            void StartBackup()
            {
                ConfigManager.ViewModel.ShowProgressBar = true;
                ConfigManager.ViewModel.StateChangeInfo = new VersionStateChangeInfo();
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.isBackingUp;
            }

            void EndBackup()
            {
                ConfigManager.ViewModel.ShowProgressBar = false;
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.None;
                ConfigManager.ViewModel.StateChangeInfo = null;
            }
        }
        public async void InvokeKillGame()
        {
            if (ConfigManager.GameManager.GameProcess != null)
            {
                var title = ConfigManager.MainThread.FindResource("Dialog_KillGame_Title") as string;
                var content = ConfigManager.MainThread.FindResource("Dialog_KillGame_Text") as string;

                var result = await DialogPrompt.ShowDialog_YesNo(title, content);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    GameProcess.Kill();
                }
            }
        }

        #endregion

        #region Task Methods

        private async Task DownloadVersion(MCVersion v, VersionDownloader downloader, string dlPath, CancellationTokenSource cancelSource)
        {
            try
            {
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.isInitializing;
                await downloader.Download(v.UUID, "1", dlPath, (current, total) =>
                {
                    if (ConfigManager.ViewModel.StateChangeInfo.CurrentState == VersionStateChangeInfo.StateChange.isInitializing)
                    {
                        StartDownload();
                        Program.Log("Actual download started");
                        if (total.HasValue) ConfigManager.ViewModel.StateChangeInfo.TotalProgress = total.Value;
                    }
                    ConfigManager.ViewModel.StateChangeInfo.CurrentProgress = current;
                }, cancelSource.Token);
                Program.Log("Download complete");
                EndDownload();
            }
            catch (Exception e)
            {
                EndDownload();
                Program.Log("Download failed:\n" + e.ToString());
                if (!(e is TaskCanceledException)) MessageBox.Show("Download failed:\n" + e.ToString());
                throw e;
            }

            void StartDownload()
            {
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.isDownloading;
            }

            void EndDownload()
            {
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.None;
            }
        }
        private async Task BetaAuthenticate()
        {
            if (Interlocked.CompareExchange(ref _userVersionDownloaderLoginTaskStarted, 1, 0) == 0) _userVersionDownloaderLoginTask.Start();
            Program.Log("Waiting for authentication");
            try
            {
                await _userVersionDownloaderLoginTask;
                Program.Log("Authentication complete");
            }
            catch (Exception e)
            {
                Program.Log("Authentication failed:\n" + e.ToString());
                MessageBox.Show("Failed to authenticate. Please make sure your account is subscribed to the beta programme.\n\n" + e.ToString(), "Authentication failed");
                throw e;
            }
        }
        private async Task LaunchGame(MCVersion v)
        {
            try
            {
                StartLaunch();
                var pkg = await AppDiagnosticInfo.RequestInfoForPackageAsync(MINECRAFT_PACKAGE_FAMILY);
                if (pkg.Count > 0) await pkg[0].LaunchAsync();
                Program.Log("App launch finished!");
                if (Properties.LauncherSettings.Default.KeepLauncherOpen) GetGameProcess(v);
                if (Properties.LauncherSettings.Default.KeepLauncherOpen == false)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Application.Current.MainWindow.Close();
                    });
                }
                EndLaunch();
            }
            catch (Exception e)
            {
                EndLaunch();
                Program.Log("App launch failed:\n" + e.ToString());
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ErrorScreenShow.errormsg("applauncherror");
                });
                throw e;
            }

            void StartLaunch()
            {
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.isLaunching;
            }

            void EndLaunch()
            {
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.None;
            }

        }
        private async Task SetInstallationDataPath()
        {
            await Task.Run(() =>
            {
                if (Properties.LauncherSettings.Default.SaveRedirection)
                {
                    string PackageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", MINECRAFT_PACKAGE_FAMILY);
                    string ProfileFolder = Path.GetFullPath(Filepaths.GetInstallationsFolderPath(ConfigManager.CurrentProfile, ConfigManager.CurrentInstallation.DirectoryName));

                    if (Directory.Exists(PackageFolder))
                    {
                        System.IO.Directory.Delete(PackageFolder, true);
                    }

                    SetFolderPermisions(ProfileFolder);
                    Methods.SymLinkHelper.CreateSymbolicLink(PackageFolder, ProfileFolder, SymLinkHelper.SymbolicLinkType.Directory);
                    SetLinkPermisions(PackageFolder);
                }
            });


            void SetFolderPermisions(string ProfileFolder)
            {
                DirectoryInfo dInfo = Directory.CreateDirectory(ProfileFolder);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                SecurityIdentifier authenticated_users_identity = new SecurityIdentifier("S-1-5-11");
                dSecurity.SetOwner(WindowsIdentity.GetCurrent().User);
                dSecurity.AddAccessRule(new FileSystemAccessRule(authenticated_users_identity, FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                dInfo.SetAccessControl(dSecurity);
            }
            void SetLinkPermisions(string PackageFolder)
            {
                DirectoryInfo dInfo = new DirectoryInfo(PackageFolder);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                SecurityIdentifier authenticated_users_identity = new SecurityIdentifier("S-1-5-11");
                dSecurity.AddAccessRule(new FileSystemAccessRule(authenticated_users_identity, FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                dInfo.SetAccessControl(dSecurity);
            }
        }
        private async Task ExtractPackage(MCVersion v, string dlPath)
        {
            Stream zipReadingStream = null;
            try
            {
                Program.Log("Extraction started");
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.isExtracting;
                string dirPath = v.GameDirectory;
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, true);

                zipReadingStream = File.OpenRead(dlPath);
                ZipArchive zip = new ZipArchive(zipReadingStream);
                var progress = new Progress<ZipProgress>();
                progress.ProgressChanged += (s, z) =>
                {
                    if (ConfigManager.ViewModel.StateChangeInfo != null)
                    {
                        ConfigManager.ViewModel.StateChangeInfo.CurrentProgress = z.Processed;
                        ConfigManager.ViewModel.StateChangeInfo.TotalProgress = z.Total;
                    }
                };
                await Task.Run(() => zip.ExtractToDirectory(dirPath, progress));

                zipReadingStream.Close();

                ConfigManager.ViewModel.StateChangeInfo = null;
                File.Delete(Path.Combine(dirPath, "AppxSignature.p7x"));
                File.Delete(dlPath);
                Program.Log("Extracted successfully");
            }
            catch (Exception e)
            {
                Program.Log("Extraction failed:\n" + e.ToString());
                MessageBox.Show("Extraction failed:\n" + e.ToString());
                if (zipReadingStream != null) zipReadingStream.Close();
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.None;
                return;
            }
        }
        private async Task RemovePackage(MCVersion v, Package pkg)
        {
            Program.Log("Removing package: " + pkg.Id.FullName);
            ConfigManager.ViewModel.StateChangeInfo.DeploymentPackageName = pkg.Id.FullName;
            ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.isRemovingPackage;
            if (!pkg.IsDevelopmentMode)
            {
                // somehow this cause errors for some people, idk why, disabled
                //BackupMinecraftDataForRemoval();
                await DeploymentProgressWrapper(v, new PackageManager().RemovePackageAsync(pkg.Id.FullName, 0));
            }
            else
            {
                Program.Log("Package is in development mode");
                await DeploymentProgressWrapper(v, new PackageManager().RemovePackageAsync(pkg.Id.FullName, RemovalOptions.PreserveApplicationData));
            }
            Program.Log("Removal of package done: " + pkg.Id.FullName);
            ConfigManager.ViewModel.StateChangeInfo.DeploymentPackageName = "";
            ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.None;
        }
        private async Task UnregisterPackage(MCVersion v, string gameDir)
        {
            foreach (var pkg in new PackageManager().FindPackages(MINECRAFT_PACKAGE_FAMILY))
            {
                string location = GetPackagePath(pkg);
                if (location == "" || location == gameDir)
                {
                    await RemovePackage(v, pkg);
                }
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
                        Program.Log("Skipping package removal - same path: " + pkg.Id.FullName + " " + location);
                        return;
                    }
                    await RemovePackage(v, pkg);
                }
                Program.Log("Registering package");
                string manifestPath = Path.Combine(gameDir, "AppxManifest.xml");
                ConfigManager.ViewModel.StateChangeInfo.DeploymentPackageName = GetPackageNameFromMainifest(manifestPath);
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.isRegisteringPackage;
                await DeploymentProgressWrapper(v, new PackageManager().RegisterPackageAsync(new Uri(manifestPath), null, DeploymentOptions.DevelopmentMode));
                Program.Log("App re-register done!");
                ConfigManager.ViewModel.StateChangeInfo.DeploymentPackageName = "";
                ConfigManager.ViewModel.StateChangeInfo.CurrentState = VersionStateChangeInfo.StateChange.None;

                string GetPackageNameFromMainifest(string filePath)
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
            }
            catch (Exception e)
            {
                Program.Log("App re-register failed:\n" + e.ToString());
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
                Program.Log("Deployment progress: " + p.state + " " + p.percentage + "%");
                ConfigManager.ViewModel.StateChangeInfo.CurrentProgress = Convert.ToInt64(p.percentage);
            };
            t.Completed += (v, p) =>
            {
                if (p == AsyncStatus.Error)
                {
                    Program.Log("Deployment failed: " + v.GetResults().ErrorText);
                    src.SetException(new Exception("Deployment failed: " + v.GetResults().ErrorText));
                }
                else
                {
                    Program.Log("Deployment done: " + p);
                    src.SetResult(1);
                }
            };
            await src.Task;
        }

        #endregion

        #region Helper Methods

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
