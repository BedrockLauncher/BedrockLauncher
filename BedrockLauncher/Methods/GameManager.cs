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
using BedrockLauncher.Methods;
using BedrockLauncher.Classes;
using BedrockLauncher.Pages.GameScreen;
using BedrockLauncher.Pages.NoContentScreen;
using BedrockLauncher.Pages.PlayScreen;
using BedrockLauncher.Pages.ServersScreen;
using BedrockLauncher.Pages.SettingsScreen;
using BedrockLauncher.Pages.FirstLaunch;
using BedrockLauncher.Pages.ErrorScreen;
using BedrockLauncher.Pages.InstallationsScreen;
using BedrockLauncher.Pages.NewsScreen;
using BedrockLauncher.Pages.ProfileManagementScreen;
using System.Windows.Media.Animation;
using ServerTab;
using BedrockLauncher.Core;

using Version = BedrockLauncher.Classes.Version;

namespace BedrockLauncher.Methods
{
    public class GameManager: ICommonVersionCommands
    {
        #region Status

        public bool IsDownloading = false;
        public volatile bool HasLaunchTask = false;

        #endregion

        #region Threading Tasks

        private readonly VersionDownloader _anonVersionDownloader = new VersionDownloader();
        private readonly VersionDownloader _userVersionDownloader = new VersionDownloader();
        private readonly Task _userVersionDownloaderLoginTask;
        private volatile int _userVersionDownloaderLoginTaskStarted;

        #endregion

        #region Constants

        private static readonly string MINECRAFT_PACKAGE_FAMILY = "Microsoft.MinecraftUWP_8wekyb3d8bbwe";

        #endregion

        #region ICommands
        public ICommand LaunchCommand => new RelayCommand((v) => InvokeLaunch((Version)v));
        public ICommand RemoveCommand => new RelayCommand((v) => InvokeRemove((Version)v));
        public ICommand DownloadCommand => new RelayCommand((v) => InvokeDownload((Version)v));

        #endregion

        #region Events

        public class GameStateArgs : EventArgs
        {
            public static GameStateArgs Empty => new GameStateArgs();

        }

        public event EventHandler GameStateChanged;
        protected virtual void OnGameStateChanged(GameStateArgs e)
        {
            EventHandler handler = GameStateChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

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

        public void Play(Installation i)
        {
            {
                var v = i.Version;
                switch (v.DisplayInstallStatus.ToString())
                {
                    case "Not installed":
                        InvokeDownload(v);
                        break;
                    case "Installed":
                        InvokeLaunch(v);
                        break;
                }
            }
        }

        #endregion

        #region Invoke Methods

        private void InvokeLaunch(Version v)
        {
            if (HasLaunchTask)
            {
                OnGameStateChanged(GameStateArgs.Empty);
                return;
            }
            else
            {
                HasLaunchTask = true;
                OnGameStateChanged(GameStateArgs.Empty);
            }

            Task.Run(async () =>
            {
                v.StateChangeInfo = new VersionStateChangeInfo();
                v.StateChangeInfo.IsLaunching = true;
                string gameDir = Path.GetFullPath(v.GameDirectory);
                try
                {
                    await ReRegisterPackage(gameDir);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("App re-register failed:\n" + e.ToString());
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ErrorScreenShow.errormsg("appregistererror");
                    });
                    HasLaunchTask = false;
                    OnGameStateChanged(GameStateArgs.Empty);
                    v.StateChangeInfo = null;
                    return;
                }
                try
                {
                    var pkg = await AppDiagnosticInfo.RequestInfoForPackageAsync(MINECRAFT_PACKAGE_FAMILY);
                    if (pkg.Count > 0)
                        await pkg[0].LaunchAsync();
                    Debug.WriteLine("App launch finished!");
                    HasLaunchTask = false;
                    OnGameStateChanged(GameStateArgs.Empty);
                    v.StateChangeInfo = null;
                    // close launcher if needed and hide progressbar
                    Application.Current.Dispatcher.Invoke(() => { ConfigManager.MainThread.ProgressBarGrid.Visibility = Visibility.Hidden; });
                    if (Properties.Settings.Default.KeepLauncherOpenCheckBox == false)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Application.Current.MainWindow.Close();
                        });
                    }

                }
                catch (Exception e)
                {
                    Debug.WriteLine("App launch failed:\n" + e.ToString());
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConfigManager.MainThread.ProgressBarGrid.Visibility = Visibility.Hidden;
                        ErrorScreenShow.errormsg("applauncherror");
                    });
                    HasLaunchTask = false;
                    OnGameStateChanged(GameStateArgs.Empty);
                    v.StateChangeInfo = null;
                    return;
                }
            });
        }
        private void InvokeDownload(Version v)
        {
            CancellationTokenSource cancelSource = new CancellationTokenSource();
            v.StateChangeInfo = new VersionStateChangeInfo();
            v.StateChangeInfo.IsInitializing = true;
            v.StateChangeInfo.CancelCommand = new RelayCommand((o) => cancelSource.Cancel());

            Debug.WriteLine("Download start");
            Task.Run(async () =>
            {
                string dlPath = "Minecraft-" + v.Name + ".Appx";
                VersionDownloader downloader = _anonVersionDownloader;
                if (v.IsBeta)
                {
                    downloader = _userVersionDownloader;
                    if (Interlocked.CompareExchange(ref _userVersionDownloaderLoginTaskStarted, 1, 0) == 0)
                    {
                        _userVersionDownloaderLoginTask.Start();
                    }
                    Debug.WriteLine("Waiting for authentication");
                    try
                    {
                        await _userVersionDownloaderLoginTask;
                        Debug.WriteLine("Authentication complete");
                    }
                    catch (Exception e)
                    {
                        v.StateChangeInfo = null;
                        Debug.WriteLine("Authentication failed:\n" + e.ToString());
                        MessageBox.Show("Failed to authenticate. Please make sure your account is subscribed to the beta programme.\n\n" + e.ToString(), "Authentication failed");
                        return;
                    }
                }
                try
                {
                    await downloader.Download(v.UUID, "1", dlPath, (current, total) =>
                    {
                        if (v.StateChangeInfo.IsInitializing)
                        {
                            IsDownloading = true;
                            OnGameStateChanged(GameStateArgs.Empty);
                            Debug.WriteLine("Actual download started");
                            v.StateChangeInfo.IsInitializing = false;
                            if (total.HasValue)
                                v.StateChangeInfo.TotalSize = total.Value;
                        }
                        v.StateChangeInfo.DownloadedBytes = current;
                    }, cancelSource.Token);
                    Debug.WriteLine("Download complete");
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Download failed:\n" + e.ToString());
                    if (!(e is TaskCanceledException))
                        MessageBox.Show("Download failed:\n" + e.ToString());
                    v.StateChangeInfo = null;
                    IsDownloading = false;
                    return;
                }
                try
                {
                    Debug.WriteLine("Extraction started");
                    v.StateChangeInfo.IsExtracting = true;
                    string dirPath = v.GameDirectory;
                    if (Directory.Exists(dirPath))
                        Directory.Delete(dirPath, true);
                    ZipFile.ExtractToDirectory(dlPath, dirPath);
                    v.StateChangeInfo = null;
                    File.Delete(Path.Combine(dirPath, "AppxSignature.p7x"));
                    File.Delete(dlPath);
                    Debug.WriteLine("Extracted successfully");
                    InvokeLaunch(v);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Extraction failed:\n" + e.ToString());
                    MessageBox.Show("Extraction failed:\n" + e.ToString());
                    v.StateChangeInfo = null;
                    IsDownloading = false;
                    return;
                }
                v.StateChangeInfo = null;
                v.UpdateInstallStatus();
                OnGameStateChanged(GameStateArgs.Empty);
            });
        }
        private void InvokeRemove(Version v)
        {
            Task.Run(async () =>
            {
                v.StateChangeInfo = new VersionStateChangeInfo();
                v.StateChangeInfo.IsUninstalling = true;
                await UnregisterPackage(Path.GetFullPath(v.GameDirectory));
                Directory.Delete(v.GameDirectory, true);
                v.StateChangeInfo = null;
                v.UpdateInstallStatus();
            });
        }

        #endregion

        #region Task Methods

        private async Task RemovePackage(Package pkg)
        {
            Debug.WriteLine("Removing package: " + pkg.Id.FullName);
            if (!pkg.IsDevelopmentMode)
            {
                // somehow this cause errors for some people, idk why, disabled
                //BackupMinecraftDataForRemoval();
                await DeploymentProgressWrapper(new PackageManager().RemovePackageAsync(pkg.Id.FullName, 0));
            }
            else
            {
                Debug.WriteLine("Package is in development mode");
                await DeploymentProgressWrapper(new PackageManager().RemovePackageAsync(pkg.Id.FullName, RemovalOptions.PreserveApplicationData));
            }
            Debug.WriteLine("Removal of package done: " + pkg.Id.FullName);
        }
        private async Task UnregisterPackage(string gameDir)
        {
            foreach (var pkg in new PackageManager().FindPackages(MINECRAFT_PACKAGE_FAMILY))
            {
                string location = GetPackagePath(pkg);
                if (location == "" || location == gameDir)
                {
                    await RemovePackage(pkg);
                }
            }
        }
        private async Task ReRegisterPackage(string gameDir)
        {
            foreach (var pkg in new PackageManager().FindPackages(MINECRAFT_PACKAGE_FAMILY))
            {
                string location = GetPackagePath(pkg);
                if (location == gameDir)
                {
                    Debug.WriteLine("Skipping package removal - same path: " + pkg.Id.FullName + " " + location);
                    return;
                }
                await RemovePackage(pkg);
            }
            Debug.WriteLine("Registering package");

            string manifestPath = Path.Combine(gameDir, "AppxManifest.xml");
            await DeploymentProgressWrapper(new PackageManager().RegisterPackageAsync(new Uri(manifestPath), null, DeploymentOptions.DevelopmentMode));
            Debug.WriteLine("App re-register done!");
            RestoreMinecraftDataFromReinstall();
        }
        private async Task DeploymentProgressWrapper(IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> t)
        {
            TaskCompletionSource<int> src = new TaskCompletionSource<int>();
            t.Progress += (v, p) =>
            {
                Debug.WriteLine("Deployment progress: " + p.state + " " + p.percentage + "%");
            };
            t.Completed += (v, p) =>
            {
                if (p == AsyncStatus.Error)
                {
                    Debug.WriteLine("Deployment failed: " + v.GetResults().ErrorText);
                    src.SetException(new Exception("Deployment failed: " + v.GetResults().ErrorText));
                }
                else
                {
                    Debug.WriteLine("Deployment done: " + p);
                    src.SetResult(1);
                }
            };
            await src.Task;
        }

        #endregion

        #region Backup/Restore Methods

        private string GetBackupMinecraftDataDir()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string tmpDir = Path.Combine(localAppData, "TmpMinecraftLocalState");
            return tmpDir;
        }
        private void BackupMinecraftDataForRemoval()
        {
            var data = ApplicationDataManager.CreateForPackageFamily(MINECRAFT_PACKAGE_FAMILY);
            string tmpDir = GetBackupMinecraftDataDir();
            if (Directory.Exists(tmpDir))
            {
                Debug.WriteLine("BackupMinecraftDataForRemoval error: " + tmpDir + " already exists");
                Process.Start("explorer.exe", tmpDir);
                MessageBox.Show("The temporary directory for backing up MC data already exists. This probably means that we failed last time backing up the data. Please back the directory up manually.");
                throw new Exception("Temporary dir exists");
            }
            Debug.WriteLine("Moving Minecraft data to: " + tmpDir);
            Directory.Move(data.LocalFolder.Path, tmpDir);
        }
        private void RestoreMove(string from, string to)
        {
            foreach (var f in Directory.EnumerateFiles(from))
            {
                string ft = Path.Combine(to, Path.GetFileName(f));
                if (File.Exists(ft))
                {
                    if (MessageBox.Show("The file " + ft + " already exists in the destination.\nDo you want to replace it? The old file will be lost otherwise.", "Restoring data directory from previous installation", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        continue;
                    File.Delete(ft);
                }
                File.Move(f, ft);
            }
            foreach (var f in Directory.EnumerateDirectories(from))
            {
                string tp = Path.Combine(to, Path.GetFileName(f));
                if (!Directory.Exists(tp))
                {
                    if (File.Exists(tp) && MessageBox.Show("The file " + tp + " is not a directory. Do you want to remove it? The data from the old directory will be lost otherwise.", "Restoring data directory from previous installation", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        continue;
                    Directory.CreateDirectory(tp);
                }
                RestoreMove(f, tp);
            }
        }
        private void RestoreMinecraftDataFromReinstall()
        {
            string tmpDir = GetBackupMinecraftDataDir();
            if (!Directory.Exists(tmpDir))
                return;
            var data = ApplicationDataManager.CreateForPackageFamily(MINECRAFT_PACKAGE_FAMILY);
            Debug.WriteLine("Moving backup Minecraft data to: " + data.LocalFolder.Path);
            RestoreMove(tmpDir, data.LocalFolder.Path);
            Directory.Delete(tmpDir, true);
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
