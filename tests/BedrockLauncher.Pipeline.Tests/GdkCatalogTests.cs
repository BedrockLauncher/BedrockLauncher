using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Databases;
using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.UpdateProcessor.Handlers;
using Xunit;

namespace BedrockLauncher.Pipeline.Tests
{
    public class GdkCatalogTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "bl-catalog-" + Guid.NewGuid().ToString("N"));

        public GdkCatalogTests() => Directory.CreateDirectory(directory);

        public void Dispose() => Directory.Delete(directory, true);

        private static VersionInfoJson Discovered(string version, GdkPackageIdentity identity, VersionType type = VersionType.Release)
        {
            string uuid = GdkLinksDb.CreateStableUuid($"gdk:{type}:{version}:{identity.Architecture}").ToString();
            return new VersionInfoJson(version, uuid, type, identity.Architecture, PackageType.GDK, identity.FullName,
                new[] { $"http://assets1.xboxlive.com/x/{identity.FullName}.msixvc" });
        }

        private string CatalogPath => Path.Combine(directory, "gdk_versions.json");

        private VersionJsonDb Reload()
        {
            var db = new VersionJsonDb();
            db.ReadJson(CatalogPath);
            return db;
        }

        [Fact]
        public void AssociationSurvivesSaveAndReload()
        {
            var catalog = new VersionJsonDb();
            catalog.MergeGdkEntry(Discovered("1.26.10.0", Identities.Release("1.26.1000.0")));
            catalog.Save(CatalogPath);

            VersionInfoJson reloaded = Assert.Single(Reload().list);

            Assert.Equal(PackageType.GDK, reloaded.packageType);
            Assert.Equal(Identities.Release("1.26.1000.0"), reloaded.GetRequiredGdkPackage());
            Assert.Single(reloaded.downloadUrls);
        }

        [Fact]
        public void UwpAndGdkVersionsAreRepresentedSideBySide()
        {
            var db = new VersionJsonDb();
            db.list.Add(new VersionInfoJson("1.21.30.3", Guid.NewGuid().ToString(), VersionType.Release, "x64", PackageType.UWP));
            db.MergeGdkEntry(Discovered("1.21.120.4", Identities.Release("1.21.12004.0")));
            db.Save(CatalogPath);

            List<VersionInfoJson> reloaded = Reload().list;

            VersionInfoJson uwp = Assert.Single(reloaded, v => v.packageType == PackageType.UWP);
            VersionInfoJson gdk = Assert.Single(reloaded, v => v.packageType == PackageType.GDK);
            Assert.Null(uwp.GetRequiredGdkPackage());
            Assert.Null(uwp.packageIdentity);
            Assert.Equal(Identities.Release("1.21.12004.0"), gdk.GetRequiredGdkPackage());
        }

        [Fact]
        public void RefreshDoesNotChangeAnExistingAssociation()
        {
            var catalog = new VersionJsonDb();
            catalog.MergeGdkEntry(Discovered("1.26.40.5", Identities.Release("1.26.4005.0")));
            catalog.Save(CatalogPath);

            // A later refresh (e.g. a changed GdkLinks entry) claims another package for the same version.
            VersionJsonDb afterRestart = Reload();
            List<GdkCatalogMergeResult> results = VersionManager.MergeGdkDiscoveries(
                afterRestart,
                new[] { Discovered("1.26.40.5", Identities.Release("1.26.5203.0")) });

            Assert.Equal(new[] { GdkCatalogMergeResult.IdentityConflict }, results);
            Assert.Equal(Identities.Release("1.26.4005.0"), Assert.Single(afterRestart.list).GetRequiredGdkPackage());
            Assert.DoesNotContain(Assert.Single(afterRestart.list).downloadUrls, url => url.Contains("5203"));
        }

        [Fact]
        public void RepeatedRefreshIsIdempotent()
        {
            var catalog = new VersionJsonDb();
            var entry = Discovered("1.26.40.5", Identities.Release("1.26.4005.0"));

            Assert.Equal(GdkCatalogMergeResult.Added, catalog.MergeGdkEntry(entry));
            Assert.Equal(GdkCatalogMergeResult.Unchanged, catalog.MergeGdkEntry(entry));
            Assert.Single(catalog.list);
        }

        [Fact]
        public void NewVersionsDoNotModifyExistingMappings()
        {
            var catalog = new VersionJsonDb();
            catalog.MergeGdkEntry(Discovered("1.21.120.4", Identities.Release("1.21.12004.0")));
            catalog.MergeGdkEntry(Discovered("1.26.40.5", Identities.Release("1.26.4005.0")));
            catalog.Save(CatalogPath);

            // A newer GDK build is published and catalogued (download / refresh of another version).
            VersionJsonDb reloaded = Reload();
            VersionManager.MergeGdkDiscoveries(reloaded, new[] { Discovered("1.26.52.3", Identities.Release("1.26.5203.0")) });

            Assert.Equal(3, reloaded.list.Count);
            Assert.Equal(Identities.Release("1.21.12004.0"), reloaded.list.Single(v => v.version == "1.21.120.4").GetRequiredGdkPackage());
            Assert.Equal(Identities.Release("1.26.4005.0"), reloaded.list.Single(v => v.version == "1.26.40.5").GetRequiredGdkPackage());
            Assert.Equal(Identities.Release("1.26.5203.0"), reloaded.list.Single(v => v.version == "1.26.52.3").GetRequiredGdkPackage());
        }

        [Fact]
        public void ResourcesForTheSamePackageAreAddedButForeignResourcesAreNot()
        {
            var identity = Identities.Release("1.26.4005.0");
            var catalog = new VersionJsonDb();
            catalog.MergeGdkEntry(Discovered("1.26.40.5", identity));

            var mirror = new VersionInfoJson("1.26.40.5", catalog.list[0].uuid.ToString(), VersionType.Release, "x64", PackageType.GDK,
                identity.FullName,
                new[]
                {
                    $"http://assets2.xboxlive.com/x/{identity.FullName}.msixvc",
                    "http://assets2.xboxlive.com/x/Microsoft.MinecraftUWP_1.26.5203.0_x64__8wekyb3d8bbwe.msixvc"
                });

            Assert.Equal(GdkCatalogMergeResult.ResourcesAdded, catalog.MergeGdkEntry(mirror));
            Assert.Equal(2, catalog.list[0].downloadUrls.Length);
            Assert.All(catalog.list[0].downloadUrls, url => Assert.Contains(identity.FullName, url));
        }

        [Fact]
        public void LegacyFourAndFiveFieldEntriesStillLoadAsUwp()
        {
            File.WriteAllText(CatalogPath, """
                [
                  ["1.21.114.01", "3738c248-0603-4560-abf4-fdd66e7cd852", 0, "x64"],
                  ["1.21.113.01", "346dba67-b58d-4244-9f4e-d250a5ee52ac", 0, "x64", 0]
                ]
                """);

            List<VersionInfoJson> list = Reload().list;

            Assert.Equal(2, list.Count);
            Assert.All(list, v => Assert.Equal(PackageType.UWP, v.packageType));
            Assert.All(list, v => Assert.Null(v.GetRequiredGdkPackage()));
        }

        [Fact]
        public void GdkEntryWithoutIdentityIsRejectedByTheCatalog()
        {
            var catalog = new VersionJsonDb();
            var unresolved = new VersionInfoJson("1.26.40.5", Guid.NewGuid().ToString(), VersionType.Release, "x64", PackageType.GDK);

            Assert.Equal(GdkCatalogMergeResult.Rejected, catalog.MergeGdkEntry(unresolved));
            Assert.Empty(catalog.list);
        }
    }
}
