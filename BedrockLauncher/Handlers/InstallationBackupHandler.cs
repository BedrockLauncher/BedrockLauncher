using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using BedrockLauncher.Enums;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Handlers
{
    /// <summary>
    /// Backup / restore of an installation's data folder (worlds, resource and behavior packs, skins, settings and key
    /// bindings) as a .zip. Works for UWP and GDK installations alike: both keep their game data in the installation's
    /// packageData folder, which the game data folders are linked to.
    /// </summary>
    public static class InstallationBackupHandler
    {
        private const string BackupsFolderName = "backups";
        private const string RestoreSuffix = ".before-restore-";

        public static async Task BackupAsync(BLInstallation installation)
        {
            if (installation == null) return;

            string source = GetDataPath(installation);
            if (string.IsNullOrEmpty(source) || !Directory.Exists(source) || !Directory.EnumerateFileSystemEntries(source).Any())
            {
                await ShowMessage("Nothing to back up", $"\"{installation.DisplayName_Full}\" has no saved data yet.");
                return;
            }

            if (IsGameRunning())
            {
                await ShowMessage("Close Minecraft first", "Minecraft is running and keeps its files open. Close it and try again.");
                return;
            }

            string backupsFolder = Path.Combine(MainDataModel.Default.FilePaths.CurrentLocation, BackupsFolderName);
            Directory.CreateDirectory(backupsFolder);

            using var dialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "Backup (*.zip)|*.zip",
                InitialDirectory = backupsFolder,
                FileName = $"{SafeFileName(installation.DisplayName_Full)}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip",
                OverwritePrompt = true
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            string destination = dialog.FileName;
            string temporary = destination + ".tmp";

            await RunWithProgress(async () =>
            {
                try
                {
                    await Task.Run(() => CreateZip(source, temporary));
                    File.Move(temporary, destination, overwrite: true);
                    Trace.WriteLine($"Backup of '{installation.DisplayName_Full}' saved to {destination}");
                }
                finally
                {
                    if (File.Exists(temporary))
                        File.Delete(temporary);
                }
            });

            Process.Start("explorer.exe", $"/select,\"{destination}\"");
        }

        public static async Task RestoreAsync(BLInstallation installation)
        {
            if (installation == null) return;

            string target = GetDataPath(installation);
            if (string.IsNullOrEmpty(target)) return;

            if (IsGameRunning())
            {
                await ShowMessage("Close Minecraft first", "Minecraft is running and keeps its files open. Close it and try again.");
                return;
            }

            string backupsFolder = Path.Combine(MainDataModel.Default.FilePaths.CurrentLocation, BackupsFolderName);
            using var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "Backup (*.zip)|*.zip",
                InitialDirectory = Directory.Exists(backupsFolder) ? backupsFolder : string.Empty
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            string archive = dialog.FileName;
            string previous = target + RestoreSuffix + DateTime.Now.ToString("yyyyMMdd-HHmmss");

            var answer = await DialogPrompt.ShowDialog_YesNo(
                "Restore backup", //TODO: Localize String
                $"Replace the data of \"{installation.DisplayName_Full}\" with {Path.GetFileName(archive)}? " +
                $"The current data is kept in {Path.GetFileName(previous)} next to it.");

            if (answer != System.Windows.Forms.DialogResult.Yes)
                return;

            await RunWithProgress(async () =>
            {
                bool movedAside = false;
                try
                {
                    if (Directory.Exists(target))
                    {
                        Directory.Move(target, previous);
                        movedAside = true;
                    }

                    await Task.Run(() => ExtractZip(archive, target));
                    Trace.WriteLine($"Restored '{installation.DisplayName_Full}' from {archive}; previous data kept in {previous}");
                }
                catch
                {
                    // Put the previous data back so a failed restore never loses anything.
                    if (Directory.Exists(target))
                        Directory.Delete(target, true);
                    if (movedAside)
                        Directory.Move(previous, target);
                    throw;
                }
            });
        }

        private static string GetDataPath(BLInstallation installation) =>
            MainDataModel.Default.FilePaths.GetInstallationPackageDataPath(
                Properties.LauncherSettings.Default.CurrentProfileUUID,
                installation.DirectoryName_Full);

        private static void CreateZip(string source, string destination)
        {
            string[] files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
            using ZipArchive zip = ZipFile.Open(destination, ZipArchiveMode.Create);

            for (int i = 0; i < files.Length; i++)
            {
                string entryName = Path.GetRelativePath(source, files[i]).Replace(Path.DirectorySeparatorChar, '/');
                zip.CreateEntryFromFile(files[i], entryName, CompressionLevel.Optimal);
                MainDataModel.Default.ProgressBarState.SetProgressBarProgress(i + 1, files.Length);
            }
        }

        private static void ExtractZip(string archive, string target)
        {
            string root = Path.GetFullPath(target) + Path.DirectorySeparatorChar;
            using ZipArchive zip = ZipFile.OpenRead(archive);
            int done = 0;

            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                string destination = Path.GetFullPath(Path.Combine(target, entry.FullName));
                if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"The backup contains an entry outside the installation folder: {entry.FullName}");

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destination);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    entry.ExtractToFile(destination, overwrite: true);
                }

                MainDataModel.Default.ProgressBarState.SetProgressBarProgress(++done, zip.Entries.Count);
            }

            Directory.CreateDirectory(target);
        }

        private static async Task RunWithProgress(Func<Task> work)
        {
            var progress = MainDataModel.Default.ProgressBarState;
            progress.SetProgressBarVisibility(true);
            progress.SetProgressBarState(LauncherState.isBackingUp);

            try
            {
                await work();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Installation backup/restore failed: {ex}");
                await MainDataModel.BackwardsCommunicationHost.exceptionmsg(ex);
            }
            finally
            {
                progress.ResetProgressBarProgress();
                progress.SetProgressBarState(LauncherState.None);
                progress.SetProgressBarVisibility(false);
            }
        }

        private static bool IsGameRunning() => Process.GetProcessesByName(Constants.MINECRAFT_PROCESS_NAME).Length > 0;

        private static Task ShowMessage(string title, string text) =>
            ErrorScreenShow.exceptionmsg(title, new Exception(text)); //TODO: Localize String

        private static string SafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
