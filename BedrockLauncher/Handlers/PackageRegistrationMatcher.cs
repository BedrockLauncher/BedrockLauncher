using System;
using System.Collections.Generic;
using System.Linq;
using BedrockLauncher.UpdateProcessor.Classes;

namespace BedrockLauncher.Handlers
{
    /// <summary>A package Windows reports as registered for the current user (snapshot of Windows.ApplicationModel.Package).</summary>
    internal sealed class InstalledPackageInfo
    {
        public string FullName { get; set; }
        public string Name { get; set; }
        public Version Version { get; set; }
        public string Architecture { get; set; }
        public string PublisherId { get; set; }
        public string InstallLocation { get; set; }
        public bool IsDevelopmentMode { get; set; }

        public string FamilyName => $"{Name}_{PublisherId}";

        public override string ToString() => FullName;
    }

    internal enum GdkInstallStatus
    {
        /// <summary>Nothing of the required family is registered.</summary>
        Missing,
        /// <summary>The exact required package (name, publisher, version, architecture) is registered.</summary>
        Exact,
        /// <summary>The family is occupied by a loose registration the launcher made from its versions folder (a UWP version).</summary>
        OccupiedByLauncherRegistration,
        /// <summary>A lower version of the required package is registered: installing the required one is an upgrade.</summary>
        OlderInstalled,
        /// <summary>A higher version is registered (typically a Store update): Windows refuses to downgrade over it.</summary>
        NewerInstalled,
        /// <summary>Same version, different architecture.</summary>
        WrongArchitecture
    }

    internal sealed class GdkInstallEvaluation
    {
        public GdkInstallStatus Status { get; }
        public InstalledPackageInfo Installed { get; }

        public GdkInstallEvaluation(GdkInstallStatus status, InstalledPackageInfo installed)
        {
            Status = status;
            Installed = installed;
        }

        public override string ToString() =>
            Installed == null ? Status.ToString() : $"{Status} ({Installed.FullName})";
    }

    /// <summary>
    /// Identity-safe package matching. Families are compared on the whole identity name plus publisher id, never by
    /// prefix or substring, so Microsoft.MinecraftUWP can never match Microsoft.MinecraftWindowsBeta.
    /// </summary>
    internal static class PackageRegistrationMatcher
    {
        internal static bool SameFamily(string expectedFamily, string installedFamily)
        {
            return TrySplitFamilyName(expectedFamily, out string expectedName, out string expectedPublisher) &&
                   TrySplitFamilyName(installedFamily, out string installedName, out string installedPublisher) &&
                   string.Equals(expectedName, installedName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(expectedPublisher, installedPublisher, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool SameVersion(string expectedVersion, string installedVersion)
        {
            return Version.TryParse(expectedVersion, out Version expected) &&
                   Version.TryParse(installedVersion, out Version installed) &&
                   expected.Revision >= 0 && installed.Revision >= 0 &&
                   expected == installed;
        }

        /// <summary>
        /// UWP: does a registered package correspond to the selected launcher version? A loose (unsigned) registration
        /// matches only when it points at the version folder; a signed registration matches only on exact version.
        /// </summary>
        internal static bool MatchesRegistration(
            string expectedFamily,
            string installedFamily,
            bool sameInstallDirectory,
            bool signedPackageRegistration,
            string expectedVersion,
            string installedVersion)
        {
            if (!SameFamily(expectedFamily, installedFamily))
                return false;

            return signedPackageRegistration
                ? SameVersion(expectedVersion, installedVersion)
                : sameInstallDirectory;
        }

        /// <summary>Exact identity match: name, publisher id, version and architecture.</summary>
        internal static bool IsExactPackage(GdkPackageIdentity required, InstalledPackageInfo installed)
        {
            return required != null &&
                   installed != null &&
                   SameFamily(required.FamilyName, installed.FamilyName) &&
                   installed.Version == required.Version &&
                   string.Equals(required.Architecture, installed.Architecture, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines what is registered for the family of the required GDK package. Packages of other families
        /// are ignored. Windows keeps at most one package per family per user, so more than one candidate means the
        /// enumeration is inconsistent and the exact one (if any) is preferred; otherwise the first is reported.
        /// The decision is based on identity first and only then on version ordering.
        /// </summary>
        internal static GdkInstallEvaluation EvaluateGdk(
            GdkPackageIdentity required,
            IEnumerable<InstalledPackageInfo> installedPackages,
            Func<string, bool> isLauncherOwnedLocation)
        {
            if (required == null)
                throw new ArgumentNullException(nameof(required));

            List<InstalledPackageInfo> sameFamily = (installedPackages ?? Enumerable.Empty<InstalledPackageInfo>())
                .Where(package => package != null && SameFamily(required.FamilyName, package.FamilyName))
                .ToList();

            if (sameFamily.Count == 0)
                return new GdkInstallEvaluation(GdkInstallStatus.Missing, null);

            InstalledPackageInfo exact = sameFamily.FirstOrDefault(package => IsExactPackage(required, package));
            if (exact != null)
                return new GdkInstallEvaluation(GdkInstallStatus.Exact, exact);

            InstalledPackageInfo installed = sameFamily[0];

            if (installed.IsDevelopmentMode &&
                isLauncherOwnedLocation != null &&
                isLauncherOwnedLocation(installed.InstallLocation))
            {
                return new GdkInstallEvaluation(GdkInstallStatus.OccupiedByLauncherRegistration, installed);
            }

            if (installed.Version == required.Version)
                return new GdkInstallEvaluation(GdkInstallStatus.WrongArchitecture, installed);

            return installed.Version > required.Version
                ? new GdkInstallEvaluation(GdkInstallStatus.NewerInstalled, installed)
                : new GdkInstallEvaluation(GdkInstallStatus.OlderInstalled, installed);
        }

        private static bool TrySplitFamilyName(string familyName, out string identityName, out string publisherId)
        {
            identityName = null;
            publisherId = null;

            if (string.IsNullOrWhiteSpace(familyName))
                return false;

            string trimmed = familyName.Trim();
            int separator = trimmed.LastIndexOf('_');
            if (separator <= 0 || separator == trimmed.Length - 1)
                return false;

            identityName = trimmed.Substring(0, separator);
            publisherId = trimmed.Substring(separator + 1);
            return identityName.IndexOf('_') < 0;
        }
    }
}
