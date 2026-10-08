using BedrockLauncher.Exceptions;
using BedrockLauncher.Handlers;
using BedrockLauncher.UpdateProcessor.Enums;
using Xunit;

namespace BedrockLauncher.Pipeline.Tests
{
    public class GdkLaunchPipelineTests
    {
        private static GdkLaunchPipeline Pipeline(FakeGdkPlatform platform, List<string> log = null) =>
            new GdkLaunchPipeline(platform, message => log?.Add(message));

        [Fact]
        public async Task ExactInstalledPackageIsReusedWithoutInstalling()
        {
            var required = Identities.Release("1.26.4005.0");
            var platform = new FakeGdkPlatform();
            platform.Installed.Add(FakeGdkPlatform.Package(required));

            InstalledPackageInfo launched = await Pipeline(platform).LaunchAsync(Identities.GdkRequest(required));

            Assert.Equal(required.FullName, launched.FullName);
            Assert.DoesNotContain("Install", platform.Calls);
            Assert.DoesNotContain("Remove", platform.Calls);
            Assert.Contains("Activate:" + required.FullName, platform.Calls);
        }

        [Fact]
        public async Task MissingPackageInstallsExactlyTheRequiredVersion()
        {
            var required = Identities.Release("1.21.12004.0");
            var platform = new FakeGdkPlatform();

            await Pipeline(platform).LaunchAsync(Identities.GdkRequest(required));

            Assert.Equal(new[] { required }, platform.InstallRequests);
        }

        [Fact]
        public async Task NewerInstalledPackageDoesNotReplaceTheRequiredOneAndBlocksLaunch()
        {
            var required = Identities.Release("1.26.4005.0");
            var newer = Identities.Release("1.26.5203.0");
            var platform = new FakeGdkPlatform { ConfirmReplace = false };
            platform.Installed.Add(FakeGdkPlatform.Package(newer));

            var error = await Assert.ThrowsAsync<GdkVersionMismatchException>(
                () => Pipeline(platform).LaunchAsync(Identities.GdkRequest(required)));

            Assert.Equal(required.FullName, error.RequiredPackage);
            Assert.Equal(newer.FullName, error.InstalledPackage);
            Assert.Empty(platform.InstallRequests);
            Assert.DoesNotContain(platform.Calls, call => call.StartsWith("Activate"));
            Assert.Single(platform.Installed, x => x.FullName == newer.FullName);
        }

        [Fact]
        public async Task NewerInstalledPackageIsReplacedByTheRequiredOneOnlyAfterConfirmation()
        {
            var required = Identities.Release("1.26.4005.0");
            var newer = Identities.Release("1.26.5203.0");
            var platform = new FakeGdkPlatform { ConfirmReplace = true };
            platform.Installed.Add(FakeGdkPlatform.Package(newer));

            await Pipeline(platform).LaunchAsync(Identities.GdkRequest(required));

            Assert.Equal(new[] { "Confirm", "Remove", "Install" }, platform.Calls.Where(c => c is "Confirm" or "Remove" or "Install"));
            Assert.Equal(new[] { required }, platform.InstallRequests);
            Assert.Contains("Activate:" + required.FullName, platform.Calls);
        }

        [Fact]
        public async Task OlderInstalledPackageDoesNotSatisfyTheRequirement()
        {
            var required = Identities.Release("1.26.4005.0");
            var older = Identities.Release("1.26.3005.0");
            var platform = new FakeGdkPlatform();
            platform.Installed.Add(FakeGdkPlatform.Package(older));

            await Pipeline(platform).LaunchAsync(Identities.GdkRequest(required));

            Assert.Equal(new[] { required }, platform.InstallRequests);
            Assert.DoesNotContain("Activate:" + older.FullName, platform.Calls);
        }

        [Fact]
        public async Task LaunchNeverPicksTheNewestRegisteredPackage()
        {
            var required = Identities.Release("1.21.12004.0");
            var platform = new FakeGdkPlatform { ConfirmReplace = true };
            platform.Installed.Add(FakeGdkPlatform.Package(Identities.Release("1.26.6000.0")));

            await Pipeline(platform).LaunchAsync(Identities.GdkRequest(required));

            string activation = Assert.Single(platform.Calls, call => call.StartsWith("Activate:"));
            Assert.Equal("Activate:" + required.FullName, activation);
        }

        [Fact]
        public async Task LaunchIsBlockedWhenWindowsInstallsADifferentPackage()
        {
            var required = Identities.Release("1.26.4005.0");
            var platform = new FakeGdkPlatform
            {
                InstallResult = _ => Identities.Release("1.26.5203.0")
            };

            var error = await Assert.ThrowsAsync<GdkVersionMismatchException>(
                () => Pipeline(platform).LaunchAsync(Identities.GdkRequest(required)));

            Assert.Contains("Required GDK: " + required.FullName, error.Message);
            Assert.Contains("Installed GDK: Microsoft.MinecraftUWP_1.26.5203.0_x64__8wekyb3d8bbwe", error.Message);
            Assert.DoesNotContain(platform.Calls, call => call.StartsWith("Activate"));
            Assert.DoesNotContain("Bootstrap", platform.Calls);
        }

        [Fact]
        public async Task WrongArchitectureIsNotAcceptedAsTheRequiredPackage()
        {
            var required = Identities.Release("1.26.4005.0", "x64");
            var platform = new FakeGdkPlatform { ConfirmReplace = false };
            platform.Installed.Add(FakeGdkPlatform.Package(Identities.Release("1.26.4005.0", "arm64")));

            await Assert.ThrowsAsync<GdkVersionMismatchException>(
                () => Pipeline(platform).LaunchAsync(Identities.GdkRequest(required)));

            Assert.Contains("Confirm", platform.Calls);
            Assert.Empty(platform.InstallRequests);
        }

        [Fact]
        public async Task GdkRequiresEntitlementBeforeAnyInstallation()
        {
            var platform = new FakeGdkPlatform { Entitled = false };

            await Assert.ThrowsAsync<GdkEntitlementException>(
                () => Pipeline(platform).LaunchAsync(Identities.GdkRequest(Identities.Release("1.26.4005.0"))));

            Assert.Equal(new[] { "Entitlement" }, platform.Calls);
        }

        [Fact]
        public async Task EntitlementRunsForEveryGdkLaunch()
        {
            var required = Identities.Release("1.26.4005.0");
            var platform = new FakeGdkPlatform();
            platform.Installed.Add(FakeGdkPlatform.Package(required));

            await Pipeline(platform).LaunchAsync(Identities.GdkRequest(required));

            Assert.Equal("Entitlement", platform.Calls.First());
        }

        [Fact]
        public async Task UwpVersionNeverReachesEntitlementOrInstaller()
        {
            var platform = new FakeGdkPlatform();
            // A GDK package being installed on the system must not matter for a UWP version.
            platform.Installed.Add(FakeGdkPlatform.Package(Identities.Release("1.26.4005.0")));

            var request = new GdkLaunchRequest
            {
                MinecraftVersion = "1.21.30.3",
                PackageType = PackageType.UWP,
                VersionType = VersionType.Release
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => Pipeline(platform).LaunchAsync(request));
            await Assert.ThrowsAsync<InvalidOperationException>(() => Pipeline(platform).EnsureReadyAsync(request));

            Assert.Empty(platform.Calls);
        }

        [Fact]
        public async Task GdkVersionWithoutRecordedPackageIsNeverResolvedFromInstalledPackages()
        {
            var platform = new FakeGdkPlatform();
            platform.Installed.Add(FakeGdkPlatform.Package(Identities.Release("1.26.4005.0")));

            await Assert.ThrowsAsync<GdkRequirementUnresolvedException>(
                () => Pipeline(platform).LaunchAsync(Identities.GdkRequest(required: null)));

            Assert.Empty(platform.Calls);
        }

        [Fact]
        public async Task ReleaseVersionCannotRequireAPreviewPackage()
        {
            var platform = new FakeGdkPlatform();

            await Assert.ThrowsAsync<GdkRequirementUnresolvedException>(
                () => Pipeline(platform).LaunchAsync(Identities.GdkRequest(Identities.Preview("1.26.6028.0"), VersionType.Release)));

            Assert.Empty(platform.Calls);
        }

        [Fact]
        public async Task PreviewPackageIsResolvedInThePreviewFamilyOnly()
        {
            var required = Identities.Preview("1.26.6028.0");
            var platform = new FakeGdkPlatform();
            // A release package with the same version number is a different family and must be ignored.
            platform.Installed.Add(FakeGdkPlatform.Package(Identities.Release("1.26.6028.0")));

            await Pipeline(platform).LaunchAsync(Identities.GdkRequest(required, VersionType.Preview));

            Assert.Equal(new[] { required }, platform.InstallRequests);
            Assert.Contains(platform.Installed, x => x.FullName == Identities.Release("1.26.6028.0").FullName);
        }

        [Fact]
        public async Task DowngradeErrorIsReportedExplicitlyWhenTheRequiredPackageIsNotRegistered()
        {
            var required = Identities.Release("1.26.4005.0");
            var platform = new FakeGdkPlatform { InstallFailureHResult = GdkLaunchPipeline.ErrorInstallPackageDowngrade };

            var error = await Assert.ThrowsAsync<GdkVersionMismatchException>(
                () => Pipeline(platform).LaunchAsync(Identities.GdkRequest(required)));

            Assert.Contains("0x80073D06", error.Message);
            Assert.DoesNotContain(platform.Calls, call => call.StartsWith("Activate"));
        }

        [Fact]
        public async Task DowngradeErrorWithTheRequiredPackageAlreadyRegisteredReusesIt()
        {
            var required = Identities.Release("1.26.4005.0");
            var platform = new FakeGdkPlatform();
            platform.Installed.Add(FakeGdkPlatform.Package(Identities.Release("1.26.3005.0")));

            // Older installed -> install requested -> Windows reports the downgrade error, but the exact package is
            // registered by then: "already present" is not confused with "refused by Windows".
            var racing = new RacingPlatform(platform, required);
            InstalledPackageInfo launched = await new GdkLaunchPipeline(racing, _ => { }).LaunchAsync(Identities.GdkRequest(required));

            Assert.Equal(required.FullName, launched.FullName);
        }

        [Fact]
        public async Task BootstrapFailureBlocksTheLaunch()
        {
            var required = Identities.Release("1.26.4005.0");
            var platform = new FakeGdkPlatform { BootstrapFailure = new GdkBootstrapException("helper missing") };
            platform.Installed.Add(FakeGdkPlatform.Package(required));

            await Assert.ThrowsAsync<GdkBootstrapException>(
                () => Pipeline(platform).LaunchAsync(Identities.GdkRequest(required)));

            Assert.DoesNotContain(platform.Calls, call => call.StartsWith("Activate"));
        }

        [Fact]
        public async Task LauncherUwpRegistrationOccupyingTheFamilyIsRemovedBeforeInstalling()
        {
            var required = Identities.Release("1.26.4005.0");
            var platform = new FakeGdkPlatform();
            platform.Installed.Add(FakeGdkPlatform.Package(
                Identities.Release("1.21.3003.0"),
                location: FakeGdkPlatform.VersionsFolder + @"\bf5746e4-6844-4124-8bc9-2fb979863c0e",
                developmentMode: true));

            await Pipeline(platform).LaunchAsync(Identities.GdkRequest(required));

            Assert.Contains("RemoveLauncherRegistration", platform.Calls);
            Assert.DoesNotContain("Confirm", platform.Calls);
            Assert.Equal(new[] { required }, platform.InstallRequests);
        }

        [Fact]
        public async Task LooseRegistrationOfTheRequiredPackageIsReplacedByAWindowsInstall()
        {
            // A GDK build registered in development mode from a launcher folder does not run (Gaming Services does
            // not know it), so even the exact identity there is removed and installed through Windows.
            var required = Identities.Release("1.26.3005.0");
            var platform = new FakeGdkPlatform();
            platform.Installed.Add(FakeGdkPlatform.Package(
                required,
                location: FakeGdkPlatform.VersionsFolder + @"\1.26.30.5",
                developmentMode: true));

            InstalledPackageInfo launched = await Pipeline(platform).LaunchAsync(Identities.GdkRequest(required));

            Assert.Contains("RemoveLauncherRegistration", platform.Calls);
            Assert.Equal(new[] { required }, platform.InstallRequests);
            Assert.False(launched.IsDevelopmentMode);
        }

        [Fact]
        public async Task SaveDataIsLinkedAfterValidationAndBeforeActivation()
        {
            var required = Identities.Release("1.26.4005.0");
            var platform = new FakeGdkPlatform();
            platform.Installed.Add(FakeGdkPlatform.Package(required));
            var request = Identities.GdkRequest(required);
            request.InstallationDataPath = @"C:\Launcher\installations\profile\install\packageData";

            await Pipeline(platform).LaunchAsync(request);

            int saveData = platform.Calls.IndexOf("SaveData:" + request.InstallationDataPath);
            Assert.True(saveData > platform.Calls.IndexOf("Bootstrap"));
            Assert.True(saveData < platform.Calls.FindIndex(call => call.StartsWith("Activate:")));
        }

        [Fact]
        public async Task SaveDataIsNotTouchedWhenTheLaunchIsBlocked()
        {
            var platform = new FakeGdkPlatform { ConfirmReplace = false };
            platform.Installed.Add(FakeGdkPlatform.Package(Identities.Release("1.26.5203.0")));
            var request = Identities.GdkRequest(Identities.Release("1.26.4005.0"));
            request.InstallationDataPath = @"C:\Launcher\installations\profile\install\packageData";

            await Assert.ThrowsAsync<GdkVersionMismatchException>(() => Pipeline(platform).LaunchAsync(request));

            Assert.DoesNotContain(platform.Calls, call => call.StartsWith("SaveData:"));
        }

        [Fact]
        public async Task LogShowsRequiredAndInstalledPackages()
        {
            var required = Identities.Release("1.26.4005.0");
            var platform = new FakeGdkPlatform();
            platform.Installed.Add(FakeGdkPlatform.Package(required));
            var log = new List<string>();

            await Pipeline(platform, log).LaunchAsync(Identities.GdkRequest(required));

            Assert.Contains("Package type: GDK", log);
            Assert.Contains("Required GDK: " + required.FullName, log);
            Assert.Contains(log, line => line.StartsWith("Installed GDK: " + required.FullName));
            Assert.Contains("Entitlement: OK", log);
            Assert.Contains("Package validation: OK", log);
            Assert.Contains("Bootstrap: OK", log);
            Assert.Contains("GDK validation: OK", log);
        }

        /// <summary>Simulates the package becoming registered while Windows reports a downgrade error.</summary>
        private sealed class RacingPlatform : IGdkPlatform
        {
            private readonly FakeGdkPlatform inner;
            private readonly Handlers.InstalledPackageInfo exact;

            public RacingPlatform(FakeGdkPlatform inner, UpdateProcessor.Classes.GdkPackageIdentity required)
            {
                this.inner = inner;
                exact = FakeGdkPlatform.Package(required);
            }

            public Task VerifyEntitlementAsync(GdkLaunchRequest request) => inner.VerifyEntitlementAsync(request);
            public IReadOnlyList<InstalledPackageInfo> GetInstalledPackages(string family) => inner.GetInstalledPackages(family);
            public bool IsLauncherOwnedLocation(string location) => inner.IsLauncherOwnedLocation(location);

            public Task InstallExactAsync(GdkLaunchRequest request)
            {
                inner.Installed.Clear();
                inner.Installed.Add(exact);
                throw new GdkDeploymentRejectedException("downgrade", GdkLaunchPipeline.ErrorInstallPackageDowngrade, new Exception());
            }

            public Task<bool> ConfirmReplaceAsync(GdkLaunchRequest request, GdkInstallEvaluation evaluation) => inner.ConfirmReplaceAsync(request, evaluation);
            public Task RemovePackageAsync(InstalledPackageInfo package, bool launcherRegistration) => inner.RemovePackageAsync(package, launcherRegistration);
            public void PrepareBootstrap(GdkLaunchRequest request, InstalledPackageInfo package) => inner.PrepareBootstrap(request, package);
            public void RecordInstall(GdkLaunchRequest request, InstalledPackageInfo package) => inner.RecordInstall(request, package);
            public void PrepareSaveData(GdkLaunchRequest request) => inner.PrepareSaveData(request);
            public Task<bool> ActivateAsync(GdkLaunchRequest request, InstalledPackageInfo package) => inner.ActivateAsync(request, package);
        }
    }
}
