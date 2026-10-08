using System;
using BedrockLauncher.UpdateProcessor.Enums;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    /// <summary>
    /// Single source for the Minecraft package identities. Release and Beta builds use Microsoft.MinecraftUWP,
    /// Preview builds use Microsoft.MinecraftWindowsBeta; both share the Microsoft publisher id. Matching is exact
    /// (case-insensitive) on the whole identity name, never by prefix or substring.
    /// </summary>
    public static class MinecraftPackageFamilies
    {
        public const string ReleaseIdentityName = "Microsoft.MinecraftUWP";
        public const string PreviewIdentityName = "Microsoft.MinecraftWindowsBeta";
        public const string PublisherId = "8wekyb3d8bbwe";

        public static string GetIdentityName(VersionType type) =>
            type == VersionType.Preview ? PreviewIdentityName : ReleaseIdentityName;

        public static string GetFamilyName(VersionType type) => $"{GetIdentityName(type)}_{PublisherId}";

        /// <summary>
        /// Maps an identity name to its channel. Beta builds cannot be told apart from Release by identity, so an
        /// identity of Microsoft.MinecraftUWP maps to Release; callers that know the channel keep their own value.
        /// </summary>
        public static bool TryGetVersionType(string identityName, out VersionType type)
        {
            if (string.Equals(identityName?.Trim(), ReleaseIdentityName, StringComparison.OrdinalIgnoreCase))
            {
                type = VersionType.Release;
                return true;
            }

            if (string.Equals(identityName?.Trim(), PreviewIdentityName, StringComparison.OrdinalIgnoreCase))
            {
                type = VersionType.Preview;
                return true;
            }

            type = VersionType.Release;
            return false;
        }

        /// <summary>True when the identity belongs to the package family of the given channel.</summary>
        public static bool BelongsTo(GdkPackageIdentity identity, VersionType type)
        {
            return identity != null &&
                   string.Equals(identity.Name, GetIdentityName(type), StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(identity.PublisherId, PublisherId, StringComparison.OrdinalIgnoreCase);
        }
    }

    public enum PackageClassificationResult
    {
        Unresolved = 0,
        Uwp = 1,
        Gdk = 2
    }

    public readonly struct MinecraftPackageClassification
    {
        public PackageClassificationResult Result { get; }
        public VersionType VersionType { get; }
        public string Reason { get; }

        public MinecraftPackageClassification(PackageClassificationResult result, VersionType versionType, string reason)
        {
            Result = result;
            VersionType = versionType;
            Reason = reason;
        }

        public bool IsResolved => Result != PackageClassificationResult.Unresolved;

        public PackageType PackageType => Result == PackageClassificationResult.Gdk ? PackageType.GDK : PackageType.UWP;
    }

    /// <summary>
    /// Classifies a package the launcher holds on disk (a version folder) from that package's own files only.
    /// Installed Windows packages are never consulted: what is registered on the system does not define what a
    /// version is.
    /// </summary>
    public static class MinecraftPackageClassifier
    {
        /// <param name="manifestIdentityName">Identity/@Name of the folder's AppxManifest.xml, or null if absent.</param>
        /// <param name="gameConfigIdentityName">Identity/@Name of the folder's MicrosoftGame.Config, or null if absent.</param>
        public static MinecraftPackageClassification Classify(string manifestIdentityName, string gameConfigIdentityName)
        {
            bool hasManifest = !string.IsNullOrWhiteSpace(manifestIdentityName);
            bool hasGameConfig = !string.IsNullOrWhiteSpace(gameConfigIdentityName);

            if (hasGameConfig)
            {
                // MicrosoftGame.Config only ships with GDK builds.
                if (!MinecraftPackageFamilies.TryGetVersionType(gameConfigIdentityName, out VersionType gdkType))
                    return Unresolved($"MicrosoftGame.Config identity '{gameConfigIdentityName}' is not a Minecraft package.");

                if (hasManifest &&
                    (!MinecraftPackageFamilies.TryGetVersionType(manifestIdentityName, out VersionType manifestType) || manifestType != gdkType))
                {
                    return Unresolved(
                        $"AppxManifest identity '{manifestIdentityName}' does not match MicrosoftGame.Config identity '{gameConfigIdentityName}'.");
                }

                return new MinecraftPackageClassification(PackageClassificationResult.Gdk, gdkType, "MicrosoftGame.Config present");
            }

            if (hasManifest)
            {
                if (!MinecraftPackageFamilies.TryGetVersionType(manifestIdentityName, out VersionType uwpType))
                    return Unresolved($"AppxManifest identity '{manifestIdentityName}' is not a Minecraft package.");

                return new MinecraftPackageClassification(PackageClassificationResult.Uwp, uwpType, "AppxManifest without MicrosoftGame.Config");
            }

            return Unresolved("Neither AppxManifest.xml nor MicrosoftGame.Config identifies the package.");
        }

        private static MinecraftPackageClassification Unresolved(string reason) =>
            new MinecraftPackageClassification(PackageClassificationResult.Unresolved, VersionType.Release, reason);
    }
}
