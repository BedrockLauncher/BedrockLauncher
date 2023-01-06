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
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.UpdateProcessor.Enums;

namespace BedrockLauncher.Handlers
{
    public static class BackupHandler
    {
        public static async Task BackupOriginalSaveData()
        {
            await Task.Run(async () =>
            {
                await BackupData(VersionType.Release);
                await BackupData(VersionType.Preview);
            });
        }

        public static async Task BackupData(VersionType type)
        {
            await Task.Run(() =>
            {
                MainViewModel.Default.ProgressBarState.SetProgressBarVisibility(true);
                MainViewModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isBackingUp);

                try
                {
                    var data = ApplicationDataManager.CreateForPackageFamily(Constants.GetPackageFamily(type));
                    string dataPath;

                    try { dataPath = Path.Combine(data.LocalFolder.Path, "games", "com.mojang"); }
                    catch { dataPath = string.Empty; }

                    if (dataPath != string.Empty)
                    {
                        var dirData = GenerateStrings();
                        if (!Directory.Exists(dirData.Item2)) Directory.CreateDirectory(dirData.Item2);
                        System.Diagnostics.Trace.WriteLine("Moving backup Minecraft data to: " + dirData.Item2);

                        RestoreCopy(dataPath, dirData.Item2);


                        Application.Current.Dispatcher.Invoke(() => MainViewModel.Default.Config.Installation_Create(dirData.Item1, null, dirData.Item2));
                    }
                }
                catch (Exception ex) { ErrorScreenShow.exceptionmsg(ex); }

                MainViewModel.Default.ProgressBarState.ResetProgressBarProgress();
                MainViewModel.Default.ProgressBarState.SetProgressBarState(LauncherState.None);
                MainViewModel.Default.ProgressBarState.SetProgressBarVisibility(false);

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
                    int Current = 0;

                    MainViewModel.Default.ProgressBarState.SetProgressBarProgress(totalProgress: Total, currentProgress: Current);

                    RestoreCopy_Step(from, to, Current, Total);
                }

                void RestoreCopy_Step(string from, string to, int Total, int Current)
                {
                    foreach (var f in Directory.EnumerateFiles(from))
                    {
                        string ft = Path.Combine(to, Path.GetFileName(f));
                        File.Copy(f, ft);
                        Current += 1;
                        MainViewModel.Default.ProgressBarState.SetProgressBarProgress(totalProgress: Total, currentProgress: Current);
                    }
                    foreach (var f in Directory.EnumerateDirectories(from))
                    {
                        string tp = Path.Combine(to, Path.GetFileName(f));
                        Directory.CreateDirectory(tp);
                        RestoreCopy_Step(f, tp, Current, Total);
                    }
                }
            });

        }


    }
}
