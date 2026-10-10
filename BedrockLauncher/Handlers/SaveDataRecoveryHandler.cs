using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BedrockLauncher.Classes;
using BedrockLauncher.Enums;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Handlers
{
    /// <summary>
    /// "Backup Save Data" (Settings → General): copies Minecraft save data that does not belong to a launcher
    /// installation into a new "Recovery Data" installation, so those worlds can be played from the launcher again.
    ///
    /// Sources, for the selected channel (Release or Preview):
    /// - UWP: the package's LocalState\games\com.mojang, when it is a real folder (not a launcher link);
    /// - GDK: %APPDATA%\Minecraft Bedrock[ Preview], when it is a real folder (not a launcher link);
    /// - GDK: the "...default" folders the launcher moved existing GDK data to when it first linked that folder;
    /// - UWP: the data the launcher moved out of the package folder before Windows removed the package.
    /// </summary>
    public static class SaveDataRecoveryHandler
    {
        private sealed class Source
        {
            public string Path { get; set; }
            public PackageType PackageType { get; set; }
        }

        public static async Task BackupAsync(VersionType type)
        {
            List<Source> sources = FindSources(type);
            if (sources.Count == 0)
            {
                await ErrorScreenShow.exceptionmsg(
                    "Nothing to back up", //TODO: Localize String
                    new Exception("No Minecraft save data was found outside the launcher installations."));
                return;
            }

            if (Process.GetProcessesByName(Constants.MINECRAFT_PROCESS_NAME).Length > 0)
            {
                await ErrorScreenShow.exceptionmsg(
                    "Close Minecraft first", //TODO: Localize String
                    new Exception("Minecraft is running and keeps its files open. Close it and try again."));
                return;
            }

            var progress = MainDataModel.Default.ProgressBarState;
            progress.SetProgressBarVisibility(true);
            progress.SetProgressBarState(LauncherState.isBackingUp);

            try
            {
                foreach (Source source in sources)
                    await RecoverAsync(source, type);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Save data backup failed: {ex}");
                await MainDataModel.BackwardsCommunicationHost.exceptionmsg(ex);
            }
            finally
            {
                progress.ResetProgressBarProgress();
                progress.SetProgressBarState(LauncherState.None);
                progress.SetProgressBarVisibility(false);
            }
        }

        private static List<Source> FindSources(VersionType type)
        {
            var sources = new List<Source>();

            string uwp = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages", Constants.GetPackageFamily(type), "LocalState", "games", "com.mojang");
            if (HasRealData(uwp))
                sources.Add(new Source { Path = uwp, PackageType = PackageType.UWP });

            string gdk = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                MinecraftPackageFamilies.GetGdkDataFolderName(type));
            if (HasRealData(gdk))
                sources.Add(new Source { Path = gdk, PackageType = PackageType.GDK });

            // UWP save data the launcher moved out of the package folder before Windows removed the package.
            string removedUwp = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                PackageHandler.SaveDataBackupFolderName);
            if (Directory.Exists(removedUwp))
            {
                foreach (string backup in Directory.EnumerateDirectories(removedUwp, Constants.GetPackageFamily(type) + "_*"))
                {
                    if (HasRealData(backup))
                        sources.Add(new Source { Path = backup, PackageType = PackageType.UWP });
                }
            }

            string parent = Path.GetDirectoryName(gdk);
            if (Directory.Exists(parent))
            {
                foreach (string backup in Directory.EnumerateDirectories(parent, Path.GetFileName(gdk) + GdkSaveDataRedirector.BackupSuffix + "*"))
                {
                    if (HasRealData(backup))
                        sources.Add(new Source { Path = backup, PackageType = PackageType.GDK });
                }
            }

            return sources;
        }

        private static bool HasRealData(string path)
        {
            var info = new DirectoryInfo(path);
            return info.Exists &&
                   !info.Attributes.HasFlag(FileAttributes.ReparsePoint) &&
                   info.EnumerateFileSystemInfos().Any();
        }

        private static async Task RecoverAsync(Source source, VersionType type)
        {
            string baseName = type == VersionType.Preview ? "Recovery Data (Preview)" : "Recovery Data";
            baseName += source.PackageType == PackageType.GDK ? " GDK" : " UWP";

            string profileUUID = Properties.LauncherSettings.Default.CurrentProfileUUID;
            string name = baseName;
            for (int i = 2; Directory.Exists(MainDataModel.Default.FilePaths.GetInstallationPath(profileUUID, name)); i++)
                name = $"{baseName} ({i})";

            string destination = MainDataModel.Default.FilePaths.GetInstallationPackageDataPath(profileUUID, name);
            Trace.WriteLine($"Backing up {source.Path} to {destination}");

            await Task.Run(() => CopyDirectory(source.Path, destination));

            MCVersion version = PickVersion(type, source.PackageType);
            await Application.Current.Dispatcher.InvokeAsync(() =>
                MainDataModel.Default.Config.Installation_Create(name, version, name));
        }

        /// <summary>
        /// GDK data is played with the latest version of the channel (GDK). UWP data keeps the UWP folder layout, so it
        /// is paired with the newest UWP version of the channel when there is one.
        /// </summary>
        private static MCVersion PickVersion(VersionType type, PackageType packageType)
        {
            if (packageType == PackageType.UWP)
            {
                MCVersion newestUwp = MainDataModel.Default.Versions
                    .Where(v => v.PackageType == PackageType.UWP && v.Type == type && Version.TryParse(v.Name, out _))
                    .OrderByDescending(v => Version.Parse(v.Name))
                    .FirstOrDefault();

                if (newestUwp != null)
                    return newestUwp;
            }

            return type == VersionType.Preview
                ? new MCVersion(Constants.LATEST_PREVIEW_UUID, Constants.LATEST_PREVIEW_UUID, "", VersionType.Preview, Constants.CurrentArchitecture)
                : new MCVersion(Constants.LATEST_RELEASE_UUID, Constants.LATEST_RELEASE_UUID, "", VersionType.Release, Constants.CurrentArchitecture);
        }

        private static void CopyDirectory(string source, string destination)
        {
            var progress = MainDataModel.Default.ProgressBarState;
            List<FileInfo> files = new List<FileInfo>();
            var pending = new Stack<DirectoryInfo>();
            pending.Push(new DirectoryInfo(source));

            // Links inside the data are not followed: only real files are copied.
            while (pending.Count > 0)
            {
                DirectoryInfo directory = pending.Pop();
                foreach (FileSystemInfo entry in directory.EnumerateFileSystemInfos())
                {
                    if (entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        continue;

                    if (entry is DirectoryInfo subDirectory)
                        pending.Push(subDirectory);
                    else if (entry is FileInfo file)
                        files.Add(file);
                }
            }

            Directory.CreateDirectory(destination);
            for (int i = 0; i < files.Count; i++)
            {
                string target = Path.Combine(destination, Path.GetRelativePath(source, files[i].FullName));
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                files[i].CopyTo(target, overwrite: true);
                progress.SetProgressBarProgress(i + 1, files.Count);
            }
        }
    }
}
