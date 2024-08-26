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
using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.Classes;

namespace BedrockLauncher.Handlers
{
    public static class BackupHandler
    {
        public static async Task BackupAllSaveData()
        {
            await Task.Run(async () =>
            {
                await BackupData(VersionType.Release);
                await BackupData(VersionType.Preview);
            });
        }
        public static async Task BackupReleaseSaveData()
        {
            await Task.Run(async () =>
            {
                await BackupData(VersionType.Release);
            });
        }

        public static async Task BackupPreviewSaveData()
        {
            await Task.Run(async () =>
            {
                await BackupData(VersionType.Preview);
            });
        }

        public static async Task BackupData(VersionType type)
        {
            await Task.Run(async () =>
            {
                MainDataModel.Default.ProgressBarState.SetProgressBarVisibility(true);
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.isBackingUp);

                try
                {
                    var data = ApplicationDataManager.CreateForPackageFamily(Constants.GetPackageFamily(type));
                    string from;

                    try { from = Path.Combine(data.LocalFolder.Path, "games", "com.mojang"); }
                    catch { from = string.Empty; }

                    if (from != string.Empty)
                    {
                        MCVersion instanceVersion = GenerateVersion(type);                 
                        var (instanceName, to) = GenerateStrings(type);
                        string toDirectoryName = Path.GetFileName(Path.GetDirectoryName(to));


                        #region Transfer

                        if (!Directory.Exists(to)) Directory.CreateDirectory(to);
                        System.Diagnostics.Trace.WriteLine("Moving backup Minecraft data to: " + to);

                        var Files = Directory.GetFiles(from, "*", SearchOption.AllDirectories);
                        var Directories = Directory.GetDirectories(from, "*", SearchOption.AllDirectories);

                        long Total = Files.Length;
                        long Current = 0;

                        MainDataModel.Default.ProgressBarState.SetProgressBarProgress(currentProgress: Current, totalProgress: Total);

                        foreach (string dirPath in Directories) Directory.CreateDirectory(dirPath.Replace(from, to));

                        foreach (var file in Files)
                        {
                            try
                            {
                                File.Copy(file, file.Replace(from, to), true);
                            }
                            catch (Exception ex)
                            {
                                await MainDataModel.BackwardsCommunicationHost.exceptionmsg(ex);
                            }

                            Current++;
                            MainDataModel.Default.ProgressBarState.SetProgressBarProgress(currentProgress: Current, totalProgress: Total);
                        }
                        #endregion

                        Application.Current.Dispatcher.Invoke(() => MainDataModel.Default.Config.Installation_Create(instanceName, instanceVersion, toDirectoryName));
                    }
                }
                catch (Exception ex) { _ = MainDataModel.BackwardsCommunicationHost.exceptionmsg(ex); }

                MainDataModel.Default.ProgressBarState.ResetProgressBarProgress();
                MainDataModel.Default.ProgressBarState.SetProgressBarState(LauncherState.None);
                MainDataModel.Default.ProgressBarState.SetProgressBarVisibility(false);

                Tuple<string, string> GenerateStrings(VersionType type)
                {
                    string name = string.Empty;
                    string dir = string.Empty;

                    switch (type)
                    {
                        case VersionType.Preview:
                            name = "Recovery Data (Preview)";
                            dir = "Recovery Data (Preview)";
                            break;
                        case VersionType.Release:
                            name = "Recovery Data";
                            dir = "Recovery Data";
                            break;
                        default:
                            break;
                    }


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
                        directoryPath = Path.Combine(MainDataModel.Default.FilePaths.GetInstallationPackageDataPath(Properties.LauncherSettings.Default.CurrentProfileUUID, recoveryDir));
                        if (!Directory.Exists(directoryPath)) break;
                        i++;
                    }

                    if (i >= 100) throw new Exception("Too many backups made");

                    return new Tuple<string, string>(recoveryName, directoryPath);

                }

                MCVersion GenerateVersion(VersionType type)
                {
                    return type == VersionType.Preview ?
                        new MCVersion(Constants.LATEST_PREVIEW_UUID, Constants.LATEST_PREVIEW_UUID, "", VersionType.Preview, Constants.CurrentArchitecture) :
                        new MCVersion(Constants.LATEST_RELEASE_UUID, Constants.LATEST_RELEASE_UUID, "", VersionType.Release, Constants.CurrentArchitecture);
                }
            });

        }


    }
}
