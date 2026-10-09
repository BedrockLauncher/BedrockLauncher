using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BedrockLauncher.Exceptions;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Enums;

namespace BedrockLauncher.Handlers
{
    /// <summary>What the GDK pipeline needs to know about the version being played. Built from the persisted version only.</summary>
    internal sealed class GdkLaunchRequest
    {
        public string MinecraftVersion { get; set; }
        public string VersionUuid { get; set; }
        public VersionType VersionType { get; set; }
        public PackageType PackageType { get; set; }
        public GdkPackageIdentity RequiredPackage { get; set; }
        public bool LaunchEditor { get; set; }

        /// <summary>The installation's data folder the game data folder is linked to; null when not launching an installation.</summary>
        public string InstallationDataPath { get; set; }
    }

    /// <summary>
    /// Windows-facing operations of the GDK pipeline. Implemented by PackageHandler; the pipeline itself never
    /// touches Windows, the network or the UI directly, which keeps the decisions below deterministic and testable.
    /// </summary>
    internal interface IGdkPlatform
    {
        IReadOnlyList<InstalledPackageInfo> GetInstalledPackages(string packageFamilyName);

        bool IsLauncherOwnedLocation(string location);

        /// <summary>Downloads exactly <see cref="GdkLaunchRequest.RequiredPackage"/> and asks Windows to install it.</summary>
        Task InstallExactAsync(GdkLaunchRequest request);

        /// <summary>Asks the user whether the registered package may be removed so the required one can be installed.</summary>
        Task<bool> ConfirmReplaceAsync(GdkLaunchRequest request, GdkInstallEvaluation evaluation);

        Task RemovePackageAsync(InstalledPackageInfo package, bool launcherRegistration);

        /// <summary>Puts the launch helper (BedrockLauncher-dll) in place. Throws <see cref="GdkBootstrapException"/>.</summary>
        void PrepareBootstrap(GdkLaunchRequest request, InstalledPackageInfo package);

        void RecordInstall(GdkLaunchRequest request, InstalledPackageInfo package);

        /// <summary>Links the GDK data folder to <see cref="GdkLaunchRequest.InstallationDataPath"/>. Throws SaveRedirectionFailedException.</summary>
        void PrepareSaveData(GdkLaunchRequest request);

        Task<bool> ActivateAsync(GdkLaunchRequest request, InstalledPackageInfo package);
    }

    /// <summary>
    /// GDK pipeline: required package -> exact install (only if needed) -> validation -> bootstrap
    /// preparation -> final validation -> launch.
    ///
    /// The required package always comes from the persisted version. What is installed on Windows is only compared
    /// with it, never used to choose it: an installed newer or older build is not accepted in its place, and nothing
    /// here ever looks for the "latest" package.
    /// </summary>
    internal sealed class GdkLaunchPipeline
    {
        // ERROR_INSTALL_PACKAGE_DOWNGRADE (winerror.h, FACILITY_WIN32): a higher version of the package is registered.
        internal const int ErrorInstallPackageDowngrade = unchecked((int)0x80073D06);

        private readonly IGdkPlatform platform;
        private readonly Action<string> log;

        public GdkLaunchPipeline(IGdkPlatform platform, Action<string> log)
        {
            this.platform = platform ?? throw new ArgumentNullException(nameof(platform));
            this.log = log ?? (_ => { });
        }

        public async Task<InstalledPackageInfo> LaunchAsync(GdkLaunchRequest request)
        {
            InstalledPackageInfo package = await EnsureReadyAsync(request);

            if (!string.IsNullOrWhiteSpace(request.InstallationDataPath))
            {
                platform.PrepareSaveData(request);
                log("Save data: OK");
            }

            if (!await platform.ActivateAsync(request, package))
            {
                log("Launch: FAILED (Windows did not activate the package)");
                throw new AppLaunchFailedException(
                    $"Could not launch Minecraft {request.MinecraftVersion} ({package.FullName}).",
                    new InvalidOperationException("Windows did not activate the installed package."));
            }

            log("Launch: requested");
            return package;
        }

        /// <summary>Everything up to (not including) activation. Used by Install/Repair and by Launch.</summary>
        public async Task<InstalledPackageInfo> EnsureReadyAsync(GdkLaunchRequest request)
        {
            GdkPackageIdentity required = ValidateRequest(request);

            log($"Minecraft version: {request.MinecraftVersion}");
            log($"Package type: {request.PackageType}");
            log($"Required GDK: {required.FullName}");
            log($"Package family: {required.FamilyName}");
            log($"Architecture: {required.Architecture}");

            GdkInstallEvaluation evaluation = Evaluate(required);
            log($"Installed GDK: {Describe(evaluation)}");

            switch (evaluation.Status)
            {
                case GdkInstallStatus.Exact:
                    log("Resolution: the required package is already registered; reusing it.");
                    break;

                case GdkInstallStatus.Missing:
                    log("Resolution: the required package is not registered; installing exactly it.");
                    await InstallExactAsync(request);
                    break;

                case GdkInstallStatus.OlderInstalled:
                    log($"Resolution: {evaluation.Installed.FullName} is older than required; installing exactly {required.FullName}.");
                    await InstallExactAsync(request);
                    break;

                case GdkInstallStatus.OccupiedByLauncherRegistration:
                    log($"Resolution: the family is occupied by the launcher registration {evaluation.Installed.FullName}; removing it.");
                    await platform.RemovePackageAsync(evaluation.Installed, launcherRegistration: true);
                    await InstallExactAsync(request);
                    break;

                case GdkInstallStatus.NewerInstalled:
                case GdkInstallStatus.WrongArchitecture:
                    if (!await platform.ConfirmReplaceAsync(request, evaluation))
                    {
                        log("Resolution: replacement declined; launch blocked.");
                        throw new GdkVersionMismatchException(
                            required.FullName,
                            evaluation.Installed.FullName,
                            "Windows keeps one package per family and does not install an older version over a newer one. " +
                            "The installed package was left untouched and the launch was blocked.");
                    }

                    log($"Resolution: replacing {evaluation.Installed.FullName} with {required.FullName} (confirmed).");
                    await platform.RemovePackageAsync(evaluation.Installed, launcherRegistration: false);
                    await InstallExactAsync(request);
                    break;

                default:
                    throw new InvalidOperationException($"Unhandled GDK install status {evaluation.Status}.");
            }

            InstalledPackageInfo installed = RequireExact(required, "Package validation");
            log($"Package identity: {installed.FullName}");
            log("Package validation: OK");

            platform.PrepareBootstrap(request, installed);
            log("Bootstrap: OK");

            platform.RecordInstall(request, installed);

            // Validate again right before handing over: the Store can update the package at any time.
            installed = RequireExact(required, "GDK validation");
            log("GDK validation: OK");

            return installed;
        }

        private GdkPackageIdentity ValidateRequest(GdkLaunchRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.PackageType != PackageType.GDK)
            {
                throw new InvalidOperationException(
                    $"Minecraft {request.MinecraftVersion} is a {request.PackageType} version and cannot use the GDK pipeline.");
            }

            GdkPackageIdentity required = request.RequiredPackage;
            if (required == null)
            {
                throw new GdkRequirementUnresolvedException(
                    $"Minecraft {request.MinecraftVersion} has no recorded GDK package, so the launcher cannot tell which " +
                    "package it requires. Refresh the version list while online.");
            }

            if (!MinecraftPackageFamilies.BelongsTo(required, request.VersionType))
            {
                throw new GdkRequirementUnresolvedException(
                    $"Minecraft {request.MinecraftVersion} ({request.VersionType}) is recorded with {required.FullName}, " +
                    $"which is not the {MinecraftPackageFamilies.GetFamilyName(request.VersionType)} family.");
            }

            return required;
        }

        private async Task InstallExactAsync(GdkLaunchRequest request)
        {
            GdkPackageIdentity required = request.RequiredPackage;

            try
            {
                await platform.InstallExactAsync(request);
            }
            catch (GdkDeploymentRejectedException e) when (e.DeploymentHResult == ErrorInstallPackageDowngrade)
            {
                GdkInstallEvaluation now = Evaluate(required);

                if (now.Status == GdkInstallStatus.Exact)
                {
                    log("Windows reported ERROR_INSTALL_PACKAGE_DOWNGRADE (0x80073D06) but the required package is registered; it is reused.");
                    return;
                }

                log($"Windows refused the installation with ERROR_INSTALL_PACKAGE_DOWNGRADE (0x80073D06); installed: {Describe(now)}");
                throw new GdkVersionMismatchException(
                    required.FullName,
                    now.Installed?.FullName,
                    "Windows refused the installation (HRESULT 0x80073D06, ERROR_INSTALL_PACKAGE_DOWNGRADE): a newer version " +
                    "of the package is registered and Windows does not install an older version over it.");
            }
        }

        private InstalledPackageInfo RequireExact(GdkPackageIdentity required, string step)
        {
            GdkInstallEvaluation evaluation = Evaluate(required);
            if (evaluation.Status == GdkInstallStatus.Exact)
                return evaluation.Installed;

            log($"{step}: FAILED — required {required.FullName}, installed {Describe(evaluation)}");
            throw new GdkVersionMismatchException(
                required.FullName,
                evaluation.Installed?.FullName,
                $"{step} failed: Windows does not report the required package as registered. The launch was blocked.");
        }

        private GdkInstallEvaluation Evaluate(GdkPackageIdentity required)
        {
            return PackageRegistrationMatcher.EvaluateGdk(
                required,
                platform.GetInstalledPackages(required.FamilyName),
                platform.IsLauncherOwnedLocation);
        }

        private static string Describe(GdkInstallEvaluation evaluation) =>
            evaluation.Installed == null ? "none" : $"{evaluation.Installed.FullName} [{evaluation.Status}]";
    }
}
