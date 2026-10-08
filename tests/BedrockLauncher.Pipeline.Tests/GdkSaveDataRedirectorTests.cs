using BedrockLauncher.Exceptions;
using BedrockLauncher.Handlers;
using Xunit;

namespace BedrockLauncher.Pipeline.Tests
{
    public class GdkSaveDataRedirectorTests : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "bl-savedata-" + Guid.NewGuid().ToString("N"));
        private readonly string gameData;
        private readonly string installationA;
        private readonly string installationB;

        public GdkSaveDataRedirectorTests()
        {
            gameData = Path.Combine(root, "Roaming", "Minecraft Bedrock");
            installationA = Path.Combine(root, "installations", "profile", "A", "packageData");
            installationB = Path.Combine(root, "installations", "profile", "B", "packageData");
            Directory.CreateDirectory(Path.Combine(root, "Roaming"));
        }

        public void Dispose()
        {
            // Remove the link first so the recursive delete never walks into a target.
            if (Directory.Exists(gameData) && new DirectoryInfo(gameData).Attributes.HasFlag(FileAttributes.ReparsePoint))
                Directory.Delete(gameData, false);
            Directory.Delete(root, true);
        }

        private static GdkSaveDataRedirector Redirector() =>
            new GdkSaveDataRedirector((link, target) =>
            {
                Directory.CreateSymbolicLink(link, target);
                return true;
            });

        private bool IsLinkTo(string target)
        {
            var info = new DirectoryInfo(gameData);
            return info.Attributes.HasFlag(FileAttributes.ReparsePoint) &&
                   string.Equals(Path.GetFullPath(info.LinkTarget), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExistingDataIsBackedUpAndTheFolderIsLinkedToTheInstallation()
        {
            Directory.CreateDirectory(Path.Combine(gameData, "Users", "Shared", "games", "com.mojang", "minecraftWorlds"));
            File.WriteAllText(Path.Combine(gameData, "options.txt"), "real data");

            var (state, backup) = Redirector().Redirect(gameData, installationA);

            Assert.Equal(GdkSaveDataState.Linked, state);
            Assert.True(IsLinkTo(installationA));
            Assert.Equal(gameData + GdkSaveDataRedirector.BackupSuffix, backup);
            Assert.Equal("real data", File.ReadAllText(Path.Combine(backup, "options.txt")));
            Assert.True(Directory.Exists(Path.Combine(backup, "Users", "Shared", "games", "com.mojang", "minecraftWorlds")));
        }

        [Fact]
        public void RedirectIsIdempotent()
        {
            Redirector().Redirect(gameData, installationA);
            File.WriteAllText(Path.Combine(installationA, "world.txt"), "kept");

            var (state, backup) = Redirector().Redirect(gameData, installationA);

            Assert.Equal(GdkSaveDataState.AlreadyLinked, state);
            Assert.Null(backup);
            Assert.Equal("kept", File.ReadAllText(Path.Combine(gameData, "world.txt")));
        }

        [Fact]
        public void SwitchingInstallationsKeepsEachInstallationsData()
        {
            Redirector().Redirect(gameData, installationA);
            File.WriteAllText(Path.Combine(gameData, "a.txt"), "A");

            Redirector().Redirect(gameData, installationB);
            File.WriteAllText(Path.Combine(gameData, "b.txt"), "B");

            Assert.True(IsLinkTo(installationB));
            Assert.Equal("A", File.ReadAllText(Path.Combine(installationA, "a.txt")));
            Assert.False(File.Exists(Path.Combine(installationA, "b.txt")));
            Assert.Equal("B", File.ReadAllText(Path.Combine(installationB, "b.txt")));
        }

        [Fact]
        public void ExistingBackupsAreNeverOverwritten()
        {
            Directory.CreateDirectory(gameData + GdkSaveDataRedirector.BackupSuffix);
            File.WriteAllText(Path.Combine(gameData + GdkSaveDataRedirector.BackupSuffix, "old.txt"), "old");
            Directory.CreateDirectory(gameData);
            File.WriteAllText(Path.Combine(gameData, "new.txt"), "new");

            var (_, backup) = Redirector().Redirect(gameData, installationA);

            Assert.Equal(gameData + GdkSaveDataRedirector.BackupSuffix + "_1", backup);
            Assert.Equal("old", File.ReadAllText(Path.Combine(gameData + GdkSaveDataRedirector.BackupSuffix, "old.txt")));
            Assert.Equal("new", File.ReadAllText(Path.Combine(backup, "new.txt")));
        }

        [Fact]
        public void FailedLinkRestoresTheOriginalData()
        {
            Directory.CreateDirectory(gameData);
            File.WriteAllText(Path.Combine(gameData, "options.txt"), "real data");

            var failing = new GdkSaveDataRedirector((_, _) => false);

            Assert.Throws<SaveRedirectionFailedException>(() => failing.Redirect(gameData, installationA));
            Assert.False(new DirectoryInfo(gameData).Attributes.HasFlag(FileAttributes.ReparsePoint));
            Assert.Equal("real data", File.ReadAllText(Path.Combine(gameData, "options.txt")));
            Assert.False(Directory.Exists(gameData + GdkSaveDataRedirector.BackupSuffix));
        }
    }
}
