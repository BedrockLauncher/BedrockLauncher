using BedrockLauncher.Handlers;
using Xunit;

namespace BedrockLauncher.Pipeline.Tests
{
    public class PackageMatchingTests
    {
        [Fact]
        public void FamilyMatchingIsCaseInsensitive()
        {
            Assert.True(PackageRegistrationMatcher.SameFamily("Microsoft.MinecraftUWP_8wekyb3d8bbwe", "MICROSOFT.MINECRAFTUWP_8WEKYB3D8BBWE"));
        }

        [Theory]
        [InlineData("Microsoft.MinecraftUWP_8wekyb3d8bbwe", "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe")]
        [InlineData("Microsoft.MinecraftUWP_8wekyb3d8bbwe", "Microsoft.MinecraftUWPBeta_8wekyb3d8bbwe")]
        [InlineData("Microsoft.MinecraftUWP_8wekyb3d8bbwe", "Microsoft.Minecraft_8wekyb3d8bbwe")]
        [InlineData("Microsoft.MinecraftUWP_8wekyb3d8bbwe", "Microsoft.MinecraftUWP_otherpublisher")]
        [InlineData("Microsoft.MinecraftUWP_8wekyb3d8bbwe", "")]
        [InlineData("Microsoft.MinecraftUWP", "Microsoft.MinecraftUWP")]
        public void FamiliesThatOnlyShareAPrefixNeverMatch(string expected, string installed)
        {
            Assert.False(PackageRegistrationMatcher.SameFamily(expected, installed));
        }

        [Fact]
        public void VersionMatchingIsExact()
        {
            Assert.True(PackageRegistrationMatcher.SameVersion("1.21.114.1", "1.21.114.1"));
            Assert.False(PackageRegistrationMatcher.SameVersion("1.21.114.1", "1.26.5004.0"));
            Assert.False(PackageRegistrationMatcher.SameVersion("1.26.40", "1.26.4005.0"));
            Assert.False(PackageRegistrationMatcher.SameVersion("1.26.4005.0", "1.26.4005.1"));
        }

        [Fact]
        public void LooseRegistrationMatchesOnlyItsOwnFolder()
        {
            Assert.False(PackageRegistrationMatcher.MatchesRegistration(
                "Microsoft.MinecraftUWP_8wekyb3d8bbwe", "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                sameInstallDirectory: false, signedPackageRegistration: false,
                expectedVersion: "1.21.114.1", installedVersion: "1.21.114.1"));

            Assert.True(PackageRegistrationMatcher.MatchesRegistration(
                "Microsoft.MinecraftUWP_8wekyb3d8bbwe", "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                sameInstallDirectory: true, signedPackageRegistration: false,
                expectedVersion: "1.21.114.1", installedVersion: "1.21.114.1"));
        }

        [Fact]
        public void SignedRegistrationMatchesOnlyTheExactVersion()
        {
            Assert.True(PackageRegistrationMatcher.MatchesRegistration(
                "Microsoft.MinecraftUWP_8wekyb3d8bbwe", "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                sameInstallDirectory: false, signedPackageRegistration: true,
                expectedVersion: "1.21.114.1", installedVersion: "1.21.114.1"));

            Assert.False(PackageRegistrationMatcher.MatchesRegistration(
                "Microsoft.MinecraftUWP_8wekyb3d8bbwe", "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                sameInstallDirectory: false, signedPackageRegistration: true,
                expectedVersion: "1.21.114.1", installedVersion: "1.26.5004.0"));
        }

        [Fact]
        public void ExactPackageRequiresNamePublisherVersionAndArchitecture()
        {
            var required = Identities.Release("1.26.4005.0");

            Assert.True(PackageRegistrationMatcher.IsExactPackage(required, FakeGdkPlatform.Package(required)));
            Assert.True(PackageRegistrationMatcher.IsExactPackage(required,
                FakeGdkPlatform.Package(new UpdateProcessor.Classes.GdkPackageIdentity("MICROSOFT.MINECRAFTUWP", required.Version, "X64", "8WEKYB3D8BBWE"))));

            Assert.False(PackageRegistrationMatcher.IsExactPackage(required, FakeGdkPlatform.Package(Identities.Release("1.26.4005.1"))));
            Assert.False(PackageRegistrationMatcher.IsExactPackage(required, FakeGdkPlatform.Package(Identities.Release("1.26.4005.0", "arm64"))));
            Assert.False(PackageRegistrationMatcher.IsExactPackage(required, FakeGdkPlatform.Package(Identities.Preview("1.26.4005.0"))));
        }

        [Fact]
        public void EvaluationIgnoresOtherFamiliesAndOrdersOnlyAfterIdentity()
        {
            var required = Identities.Release("1.26.4005.0");
            var installed = new[]
            {
                FakeGdkPlatform.Package(Identities.Preview("1.26.9999.0")),
            };

            Assert.Equal(GdkInstallStatus.Missing,
                PackageRegistrationMatcher.EvaluateGdk(required, installed, _ => false).Status);

            Assert.Equal(GdkInstallStatus.NewerInstalled,
                PackageRegistrationMatcher.EvaluateGdk(required, new[] { FakeGdkPlatform.Package(Identities.Release("1.26.5203.0")) }, _ => false).Status);

            Assert.Equal(GdkInstallStatus.OlderInstalled,
                PackageRegistrationMatcher.EvaluateGdk(required, new[] { FakeGdkPlatform.Package(Identities.Release("1.21.12004.0")) }, _ => false).Status);

            Assert.Equal(GdkInstallStatus.Exact,
                PackageRegistrationMatcher.EvaluateGdk(required, new[] { FakeGdkPlatform.Package(required) }, _ => false).Status);
        }
    }
}
