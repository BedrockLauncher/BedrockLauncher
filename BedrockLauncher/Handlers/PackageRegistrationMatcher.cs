using System;

namespace BedrockLauncher.Handlers
{
    internal static class PackageRegistrationMatcher
    {
        internal static bool SameFamily(string expectedFamily, string installedFamily)
        {
            return !string.IsNullOrWhiteSpace(expectedFamily) &&
                   !string.IsNullOrWhiteSpace(installedFamily) &&
                   string.Equals(expectedFamily, installedFamily, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool SameVersion(string expectedVersion, string installedVersion)
        {
            return !string.IsNullOrWhiteSpace(expectedVersion) &&
                   !string.IsNullOrWhiteSpace(installedVersion) &&
                   string.Equals(expectedVersion, installedVersion, StringComparison.OrdinalIgnoreCase);
        }

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
    }
}
