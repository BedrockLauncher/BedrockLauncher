using BedrockLauncher.Pages.Common;
using BedrockLauncher.Enums;
using BedrockLauncher.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Management.Core;

namespace BedrockLauncher.Handlers
{
    public static class BackupHandler
    {
        public static void BackupOriginalSaveData()
        {
            UpdateProgressBar(show: true, state: LauncherStateChange.isBackingUp);

            try
            {
                var data = ApplicationDataManager.CreateForPackageFamily(Constants.MINECRAFT_PACKAGE_FAMILY);
                string dataPath;

                try { dataPath = Path.Combine(data.LocalFolder.Path, "games", "com.mojang"); }
                catch { dataPath = string.Empty; }

                if (dataPath != string.Empty)
                {
                    var dirData = GenerateStrings();
                    if (!Directory.Exists(dirData.Item2)) Directory.CreateDirectory(dirData.Item2);
                    System.Diagnostics.Debug.WriteLine("Moving backup Minecraft data to: " + dirData.Item2);

                    RestoreCopy(dataPath, dirData.Item2);


                    Application.Current.Dispatcher.Invoke(() => MainViewModel.Default.Config.Installation_Create(dirData.Item1, null, dirData.Item2));
                }
            }
            catch (Exception ex) { ErrorScreenShow.exceptionmsg(ex); }

            UpdateProgressBar(show: false, state: LauncherStateChange.None);

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
                    directoryPath = Path.Combine(MainViewModel.Default.FilePaths.GetInstallationsFolderPath(MainViewModel.Default.Config.CurrentProfileUUID, recoveryDir));
                    if (!Directory.Exists(directoryPath)) break;
                    i++;
                }

                if (i >= 100) throw new Exception("Too many backups made");

                return new Tuple<string, string>(recoveryName, directoryPath);

            }

            void RestoreCopy(string from, string to)
            {
                int Total = Directory.GetFiles(from, "*", SearchOption.AllDirectories).Length;

                UpdateProgressBar(totalProgress: Total, progress: 0);

                RestoreCopy_Step(from, to);
            }

            void RestoreCopy_Step(string from, string to)
            {
                foreach (var f in Directory.EnumerateFiles(from))
                {
                    string ft = Path.Combine(to, Path.GetFileName(f));
                    File.Copy(f, ft);
                    MainViewModel.Default.ProgressBarState.ProgressBar_CurrentProgress += 1;
                }
                foreach (var f in Directory.EnumerateDirectories(from))
                {
                    string tp = Path.Combine(to, Path.GetFileName(f));
                    Directory.CreateDirectory(tp);
                    RestoreCopy_Step(f, tp);
                }
            }
        }
        private static void UpdateProgressBar(string deploymentPackageName = null, LauncherStateChange? state = null, long? progress = null, long? totalProgress = null, bool? show = null, bool? isGameRunning = null)
        {
            if (deploymentPackageName != null) MainViewModel.Default.ProgressBarState.DeploymentPackageName = deploymentPackageName;
            if (state != null) MainViewModel.Default.ProgressBarState.ProgressBar_CurrentState = state.Value;
            if (progress != null) MainViewModel.Default.ProgressBarState.ProgressBar_CurrentProgress = progress.Value;
            if (totalProgress != null) MainViewModel.Default.ProgressBarState.ProgressBar_TotalProgress = totalProgress.Value;
            if (show != null) MainViewModel.Default.ProgressBarState.ProgressBar_Show = show.Value;
            if (isGameRunning != null) MainViewModel.Default.ProgressBarState.IsGameRunning = isGameRunning.Value;
        }

    }
}
