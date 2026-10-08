using System;
using System.IO;
using System.Linq;
using BedrockLauncher.Exceptions;

namespace BedrockLauncher.Handlers
{
    internal enum GdkSaveDataState
    {
        AlreadyLinked,
        Linked
    }

    /// <summary>
    /// GDK counterpart of the UWP save redirection. A GDK build keeps all of its data in
    /// %APPDATA%\Minecraft Bedrock (Preview: %APPDATA%\Minecraft Bedrock Preview) instead of the package's LocalState,
    /// so that folder is turned into a link to the installation's data folder. Existing real data is moved to a
    /// ".default" backup, never deleted, and restored if the link cannot be created.
    /// </summary>
    internal sealed class GdkSaveDataRedirector
    {
        public const string BackupSuffix = ".default";

        private readonly Func<string, string, bool> createDirectoryLink;

        /// <param name="createDirectoryLink">Creates a directory symbolic link (link path, target path); false on failure.</param>
        public GdkSaveDataRedirector(Func<string, string, bool> createDirectoryLink)
        {
            this.createDirectoryLink = createDirectoryLink ?? throw new ArgumentNullException(nameof(createDirectoryLink));
        }

        /// <returns>The state reached and, when existing data had to be moved aside, the backup folder.</returns>
        public (GdkSaveDataState State, string BackupPath) Redirect(string gameDataPath, string installationDataPath)
        {
            if (string.IsNullOrWhiteSpace(gameDataPath)) throw new ArgumentException("The game data path is required.", nameof(gameDataPath));
            if (string.IsNullOrWhiteSpace(installationDataPath)) throw new ArgumentException("The installation data path is required.", nameof(installationDataPath));

            string target = Path.GetFullPath(installationDataPath);
            Directory.CreateDirectory(target);

            string backupPath = null;
            var info = new DirectoryInfo(gameDataPath);

            if (info.Exists)
            {
                if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    if (PointsTo(info, target))
                        return (GdkSaveDataState.AlreadyLinked, null);

                    // A link to another installation: removing the link leaves that installation's data untouched.
                    Directory.Delete(gameDataPath, false);
                }
                else if (!Directory.EnumerateFileSystemEntries(gameDataPath).Any())
                {
                    Directory.Delete(gameDataPath, false);
                }
                else
                {
                    backupPath = GetFreeBackupPath(gameDataPath);
                    Directory.Move(gameDataPath, backupPath);
                }
            }
            else if (File.Exists(gameDataPath))
            {
                throw new SaveRedirectionFailedException(
                    new IOException($"'{gameDataPath}' is a file, not the Minecraft data folder."));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(gameDataPath)));

            if (!createDirectoryLink(gameDataPath, target))
            {
                if (backupPath != null && !Directory.Exists(gameDataPath))
                    Directory.Move(backupPath, gameDataPath);

                throw new SaveRedirectionFailedException(new IOException(
                    $"Could not link '{gameDataPath}' to '{target}'. Enable Developer Mode or run the launcher as administrator."));
            }

            return (GdkSaveDataState.Linked, backupPath);
        }

        private static bool PointsTo(DirectoryInfo link, string target)
        {
            string linkTarget = link.LinkTarget;
            if (string.IsNullOrWhiteSpace(linkTarget))
                return false;

            string resolved = Path.GetFullPath(Path.IsPathRooted(linkTarget)
                ? linkTarget
                : Path.Combine(link.Parent?.FullName ?? string.Empty, linkTarget));

            return string.Equals(
                resolved.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                target.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string GetFreeBackupPath(string gameDataPath)
        {
            string trimmed = gameDataPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string candidate = trimmed + BackupSuffix;

            for (int index = 1; Directory.Exists(candidate) || File.Exists(candidate); index++)
                candidate = $"{trimmed}{BackupSuffix}_{index}";

            return candidate;
        }
    }
}
