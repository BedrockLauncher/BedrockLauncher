using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Databases;
using BedrockLauncher.UpdateProcessor.Enums;
using Xunit;

namespace BedrockLauncher.Pipeline.Tests
{
    public class PackageDetectionTests
    {
        [Fact]
        public void UwpReleaseFolderIsDetectedAsUwpRelease()
        {
            var result = MinecraftPackageClassifier.Classify("Microsoft.MinecraftUWP", gameConfigIdentityName: null);

            Assert.Equal(PackageClassificationResult.Uwp, result.Result);
            Assert.Equal(PackageType.UWP, result.PackageType);
            Assert.Equal(VersionType.Release, result.VersionType);
        }

        [Fact]
        public void UwpPreviewFolderIsDetectedAsUwpPreview()
        {
            var result = MinecraftPackageClassifier.Classify("Microsoft.MinecraftWindowsBeta", gameConfigIdentityName: null);

            Assert.Equal(PackageType.UWP, result.PackageType);
            Assert.Equal(VersionType.Preview, result.VersionType);
        }

        [Fact]
        public void GdkFolderIsDetectedFromMicrosoftGameConfig()
        {
            // Real GDK layout: appxmanifest.xml and MicrosoftGame.Config both use the upper-case identity.
            var result = MinecraftPackageClassifier.Classify("MICROSOFT.MINECRAFTUWP", "MICROSOFT.MINECRAFTUWP");

            Assert.Equal(PackageClassificationResult.Gdk, result.Result);
            Assert.Equal(PackageType.GDK, result.PackageType);
            Assert.Equal(VersionType.Release, result.VersionType);
        }

        [Fact]
        public void ReleaseAndPreviewAreNeverConfused()
        {
            Assert.Equal(VersionType.Release, MinecraftPackageClassifier.Classify("Microsoft.MinecraftUWP", null).VersionType);
            Assert.Equal(VersionType.Preview, MinecraftPackageClassifier.Classify("Microsoft.MinecraftWindowsBeta", null).VersionType);

            // Names that merely share a prefix are not Minecraft packages.
            Assert.False(MinecraftPackageClassifier.Classify("Microsoft.MinecraftUWPBeta", null).IsResolved);
            Assert.False(MinecraftPackageClassifier.Classify("Microsoft.Minecraft", null).IsResolved);

            // A GDK folder whose manifest and game config disagree on the family is not guessed.
            Assert.False(MinecraftPackageClassifier.Classify("Microsoft.MinecraftUWP", "Microsoft.MinecraftWindowsBeta").IsResolved);
        }

        [Fact]
        public void ClassificationDependsOnlyOnTheFolderFiles()
        {
            // The classifier has no access to installed packages; a UWP manifest stays UWP whatever Windows has.
            var result = MinecraftPackageClassifier.Classify("Microsoft.MinecraftUWP", gameConfigIdentityName: null);

            Assert.Equal(PackageType.UWP, result.PackageType);
        }

        [Fact]
        public void FolderWithoutPackageIdentityIsUnresolvedInsteadOfGuessed()
        {
            var result = MinecraftPackageClassifier.Classify(null, null);

            Assert.False(result.IsResolved);
        }

        [Theory]
        [InlineData("http://assets1.xboxlive.com/1/x/y/1.21.12004.0.z/MICROSOFT.MINECRAFTUWP_1.21.12004.0_x64__8wekyb3d8bbwe.msixvc", "MICROSOFT.MINECRAFTUWP", "1.21.12004.0")]
        [InlineData("http://assets2.xboxlive.com/2/x/y/1.26.6028.0.z/Microsoft.MinecraftWindowsBeta_1.26.6028.0_x64__8wekyb3d8bbwe.msixvc", "Microsoft.MinecraftWindowsBeta", "1.26.6028.0")]
        public void GdkIdentityIsParsedExactlyFromTheResourceName(string url, string name, string version)
        {
            Assert.True(GdkPackageIdentity.TryParseFromUrl(url, out GdkPackageIdentity identity));
            Assert.Equal(name, identity.Name);
            Assert.Equal(Version.Parse(version), identity.Version);
            Assert.Equal("x64", identity.Architecture);
            Assert.Equal("8wekyb3d8bbwe", identity.PublisherId);
        }

        [Theory]
        [InlineData("http://example.com/Microsoft.MinecraftUWP_1.21.12004.0_x64__8wekyb3d8bbwe.appx")]
        [InlineData("http://example.com/Microsoft.MinecraftUWP_1.21.120_x64__8wekyb3d8bbwe.msixvc")]
        [InlineData("http://example.com/Microsoft.MinecraftUWP_1.21.12004.0_neutral_split.scale-100_8wekyb3d8bbwe.msixvc")]
        [InlineData("not a url")]
        public void AmbiguousResourcesAreNotParsed(string url)
        {
            Assert.False(GdkPackageIdentity.TryParseFromUrl(url, out _));
        }

        [Fact]
        public void GdkLinksEntriesCarryTheirExactPackageIdentity()
        {
            var db = new GdkLinksDb();
            db.Parse("""
                {
                  "release": {
                    "1.21.120.4": [
                      "http://assets1.xboxlive.com/1/a/b/1.21.12004.0.c/MICROSOFT.MINECRAFTUWP_1.21.12004.0_x64__8wekyb3d8bbwe.msixvc",
                      "http://assets2.xboxlive.com/1/a/b/1.21.12004.0.c/MICROSOFT.MINECRAFTUWP_1.21.12004.0_x64__8wekyb3d8bbwe.msixvc"
                    ]
                  },
                  "preview": {
                    "1.26.60.28": [ "http://assets1.xboxlive.com/2/a/b/1.26.6028.0.c/Microsoft.MinecraftWindowsBeta_1.26.6028.0_x64__8wekyb3d8bbwe.msixvc" ]
                  }
                }
                """);

            Assert.Empty(db.RejectedEntries);
            VersionInfoJson release = Assert.Single(db.Versions, v => v.type == VersionType.Release);
            VersionInfoJson preview = Assert.Single(db.Versions, v => v.type == VersionType.Preview);

            Assert.Equal(PackageType.GDK, release.packageType);
            Assert.Equal(Identities.Release("1.21.12004.0"), release.GetRequiredGdkPackage());
            Assert.Equal(Identities.Preview("1.26.6028.0"), preview.GetRequiredGdkPackage());
            Assert.Equal(2, release.downloadUrls.Length);
        }

        [Fact]
        public void GdkLinksEntryInTheWrongFamilyOrWithAMismatchedVersionIsRejected()
        {
            var db = new GdkLinksDb();
            db.Parse("""
                {
                  "release": {
                    "1.26.60.28": [ "http://a/Microsoft.MinecraftWindowsBeta_1.26.6028.0_x64__8wekyb3d8bbwe.msixvc" ],
                    "1.26.40.5": [ "http://a/Microsoft.MinecraftUWP_1.26.5203.0_x64__8wekyb3d8bbwe.msixvc" ],
                    "1.26.41.1": [
                      "http://a/Microsoft.MinecraftUWP_1.26.4101.0_x64__8wekyb3d8bbwe.msixvc",
                      "http://a/Microsoft.MinecraftUWP_1.26.4102.0_x64__8wekyb3d8bbwe.msixvc"
                    ]
                  }
                }
                """);

            Assert.Empty(db.Versions);
            Assert.Equal(3, db.RejectedEntries.Count);
        }

        [Fact]
        public void GdkPackageVersionEncodesTheMinecraftVersion()
        {
            Assert.True(GdkLinksDb.TryGetPackageVersion("1.21.120.4", out Version a));
            Assert.Equal(new Version(1, 21, 12004, 0), a);
            Assert.True(GdkLinksDb.TryGetPackageVersion("1.26.40.5", out Version b));
            Assert.Equal(new Version(1, 26, 4005, 0), b);
            Assert.False(GdkLinksDb.TryGetPackageVersion("1.26.40", out _));
        }
    }
}
