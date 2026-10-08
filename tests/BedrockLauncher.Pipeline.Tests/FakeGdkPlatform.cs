using BedrockLauncher.Exceptions;
using BedrockLauncher.Handlers;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Enums;

namespace BedrockLauncher.Pipeline.Tests
{
    /// <summary>In-memory stand-in for Windows: a per-user package registry with one package per family.</summary>
    internal sealed class FakeGdkPlatform : IGdkPlatform
    {
        public const string VersionsFolder = @"C:\Launcher\versions";

        public List<InstalledPackageInfo> Installed { get; } = new List<InstalledPackageInfo>();
        public List<string> Calls { get; } = new List<string>();
        public List<GdkPackageIdentity> InstallRequests { get; } = new List<GdkPackageIdentity>();

        public bool Entitled { get; set; } = true;
        public bool ConfirmReplace { get; set; }
        public bool ActivationSucceeds { get; set; } = true;
        public Exception BootstrapFailure { get; set; }

        /// <summary>What Windows ends up registering when asked to install a package (default: exactly the request).</summary>
        public Func<GdkPackageIdentity, GdkPackageIdentity> InstallResult { get; set; } = identity => identity;

        /// <summary>Optional HRESULT failure Windows reports for an install request.</summary>
        public int? InstallFailureHResult { get; set; }

        public Task VerifyEntitlementAsync(GdkLaunchRequest request)
        {
            Calls.Add("Entitlement");
            if (!Entitled)
                throw new GdkEntitlementException("not entitled");
            return Task.CompletedTask;
        }

        public IReadOnlyList<InstalledPackageInfo> GetInstalledPackages(string packageFamilyName)
        {
            Calls.Add("Query");
            return Installed
                .Where(x => string.Equals(x.FamilyName, packageFamilyName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public bool IsLauncherOwnedLocation(string location) =>
            location != null && location.StartsWith(VersionsFolder + "\\", StringComparison.OrdinalIgnoreCase);

        public Task InstallExactAsync(GdkLaunchRequest request)
        {
            Calls.Add("Install");
            InstallRequests.Add(request.RequiredPackage);

            if (InstallFailureHResult.HasValue)
                throw new GdkDeploymentRejectedException("rejected", InstallFailureHResult.Value, new Exception("rejected"));

            GdkPackageIdentity result = InstallResult(request.RequiredPackage);
            Installed.RemoveAll(x => string.Equals(x.FamilyName, result.FamilyName, StringComparison.OrdinalIgnoreCase));
            Installed.Add(Package(result));
            return Task.CompletedTask;
        }

        public Task<bool> ConfirmReplaceAsync(GdkLaunchRequest request, GdkInstallEvaluation evaluation)
        {
            Calls.Add("Confirm");
            return Task.FromResult(ConfirmReplace);
        }

        public Task RemovePackageAsync(InstalledPackageInfo package, bool launcherRegistration)
        {
            Calls.Add(launcherRegistration ? "RemoveLauncherRegistration" : "Remove");
            Installed.Remove(package);
            return Task.CompletedTask;
        }

        public void PrepareBootstrap(GdkLaunchRequest request, InstalledPackageInfo package)
        {
            Calls.Add("Bootstrap");
            if (BootstrapFailure != null)
                throw BootstrapFailure;
        }

        public void RecordInstall(GdkLaunchRequest request, InstalledPackageInfo package)
        {
            Calls.Add("Record");
        }

        public Task<bool> ActivateAsync(GdkLaunchRequest request, InstalledPackageInfo package)
        {
            Calls.Add("Activate:" + package.FullName);
            return Task.FromResult(ActivationSucceeds);
        }

        public static InstalledPackageInfo Package(GdkPackageIdentity identity, string location = @"C:\XboxGames\Minecraft for Windows\Content", bool developmentMode = false)
        {
            return new InstalledPackageInfo
            {
                FullName = identity.FullName,
                Name = identity.Name,
                Version = identity.Version,
                Architecture = identity.Architecture,
                PublisherId = identity.PublisherId,
                InstallLocation = location,
                IsDevelopmentMode = developmentMode
            };
        }
    }

    internal static class Identities
    {
        public static GdkPackageIdentity Release(string version, string architecture = "x64") =>
            new GdkPackageIdentity(MinecraftPackageFamilies.ReleaseIdentityName, Version.Parse(version), architecture, MinecraftPackageFamilies.PublisherId);

        public static GdkPackageIdentity Preview(string version, string architecture = "x64") =>
            new GdkPackageIdentity(MinecraftPackageFamilies.PreviewIdentityName, Version.Parse(version), architecture, MinecraftPackageFamilies.PublisherId);

        public static GdkLaunchRequest GdkRequest(GdkPackageIdentity required, VersionType type = VersionType.Release) =>
            new GdkLaunchRequest
            {
                MinecraftVersion = "test",
                VersionUuid = Guid.NewGuid().ToString(),
                VersionType = type,
                PackageType = PackageType.GDK,
                RequiredPackage = required
            };
    }
}
