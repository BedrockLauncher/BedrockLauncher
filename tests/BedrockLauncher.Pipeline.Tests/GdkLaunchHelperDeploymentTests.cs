using BedrockLauncher.Exceptions;
using BedrockLauncher.Handlers;
using Xunit;

namespace BedrockLauncher.Pipeline.Tests
{
    public class GdkLaunchHelperDeploymentTests : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "bl-helper-" + Guid.NewGuid().ToString("N"));
        private readonly string source;
        private readonly string install;

        public GdkLaunchHelperDeploymentTests()
        {
            install = Path.Combine(root, "Content");
            Directory.CreateDirectory(install);

            source = Path.Combine(root, "gamelaunchhelper.dll");
            File.WriteAllBytes(source, new byte[] { 1, 2, 3, 4 });

            File.WriteAllBytes(Path.Combine(install, GdkLaunchHelperDeployment.HelperLibraryName), new byte[] { 9, 9, 9 });
            File.WriteAllBytes(Path.Combine(install, GdkLaunchHelperDeployment.HelperExecutableName), new byte[] { 0 });
            WriteManifest("GameLaunchHelper.exe");
        }

        public void Dispose() => Directory.Delete(root, true);

        private void WriteManifest(string executable)
        {
            File.WriteAllText(Path.Combine(install, "AppxManifest.xml"), $"""
                <Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10">
                  <Identity Name="Microsoft.MinecraftUWP" Publisher="CN=Microsoft Corporation" Version="1.26.4005.0" ProcessorArchitecture="x64" />
                  <Applications><Application Id="Game" Executable="{executable}" EntryPoint="Windows.FullTrustApplication" /></Applications>
                </Package>
                """);
        }

        [Fact]
        public void DeploysOnceKeepsTheOriginalAndIsIdempotent()
        {
            var deployment = new GdkLaunchHelperDeployment(source);
            string target = Path.Combine(install, GdkLaunchHelperDeployment.HelperLibraryName);

            Assert.Equal(GdkLaunchHelperState.Deployed, deployment.EnsureDeployed(install, "x64", "Game"));
            Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(target));
            Assert.Equal(new byte[] { 9, 9, 9 }, File.ReadAllBytes(target + GdkLaunchHelperDeployment.OriginalLibrarySuffix));

            DateTime written = File.GetLastWriteTimeUtc(target);
            Assert.Equal(GdkLaunchHelperState.AlreadyDeployed, deployment.EnsureDeployed(install, "x64", "Game"));
            Assert.Equal(written, File.GetLastWriteTimeUtc(target));
            Assert.Equal(new byte[] { 9, 9, 9 }, File.ReadAllBytes(target + GdkLaunchHelperDeployment.OriginalLibrarySuffix));
        }

        [Fact]
        public void PackageThatDoesNotStartThroughGameLaunchHelperIsRejected()
        {
            WriteManifest("Minecraft.Windows.exe");

            Assert.Throws<GdkBootstrapException>(() => new GdkLaunchHelperDeployment(source).EnsureDeployed(install, "x64", "Game"));
        }

        [Fact]
        public void UnsupportedArchitectureIsRejected()
        {
            Assert.Throws<GdkBootstrapException>(() => new GdkLaunchHelperDeployment(source).EnsureDeployed(install, "arm64", "Game"));
        }

        [Fact]
        public void MissingHelperBinaryIsReportedExplicitly()
        {
            Assert.Throws<GdkBootstrapException>(() => new GdkLaunchHelperDeployment(Path.Combine(root, "missing.dll")).EnsureDeployed(install, "x64", "Game"));
        }

        [Fact]
        public void InstallRecordRoundTripsAndReadsLegacyRecords()
        {
            string versionFolder = Path.Combine(root, "1.26.40.5");
            var identity = Identities.Release("1.26.4005.0");

            GdkInstallRecord.Write(versionFolder, new GdkInstallRecord.Data
            {
                RequiredPackageFullName = identity.FullName,
                InstalledPackageFullName = identity.FullName
            });

            Assert.True(GdkInstallRecord.TryRead(versionFolder, out var recorded));
            Assert.Equal(identity, recorded);

            // Format written by earlier launcher builds.
            File.WriteAllText(Path.Combine(versionFolder, GdkInstallRecord.FileName), """
                { "version": "1.26.40.5", "packageType": "GDK", "packageFullName": "Microsoft.MinecraftUWP_1.26.4005.0_x64__8wekyb3d8bbwe" }
                """);
            Assert.True(GdkInstallRecord.TryRead(versionFolder, out recorded));
            Assert.Equal(identity, recorded);
        }
    }
}
