using System;
using System.IO;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    /// <summary>
    /// Exact Windows package identity of a GDK (MSIXVC) Minecraft build, e.g.
    /// Microsoft.MinecraftUWP_1.26.4005.0_x64__8wekyb3d8bbwe.
    ///
    /// This is the "required GDK" of a GDK Minecraft version: the launcher persists it once, when the version is
    /// first catalogued, and every later step (download, install, validation, launch) compares against it exactly.
    /// Names, architectures and publisher ids compare case-insensitively because Windows normalizes them
    /// (GdkLinks lists both "MICROSOFT.MINECRAFTUWP" and "Microsoft.MinecraftUWP"); versions compare exactly.
    /// </summary>
    public sealed class GdkPackageIdentity : IEquatable<GdkPackageIdentity>
    {
        public const string MsixvcExtension = ".msixvc";

        public string Name { get; }
        public Version Version { get; }
        public string Architecture { get; }
        public string PublisherId { get; }

        public GdkPackageIdentity(string name, Version version, string architecture, string publisherId)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Package name is required.", nameof(name));
            if (version == null || version.Build < 0 || version.Revision < 0)
                throw new ArgumentException("A four-part package version is required.", nameof(version));
            if (string.IsNullOrWhiteSpace(architecture)) throw new ArgumentException("Package architecture is required.", nameof(architecture));
            if (string.IsNullOrWhiteSpace(publisherId)) throw new ArgumentException("Package publisher id is required.", nameof(publisherId));

            Name = name.Trim();
            Version = version;
            Architecture = architecture.Trim().ToLowerInvariant();
            PublisherId = publisherId.Trim().ToLowerInvariant();
        }

        public string FamilyName => $"{Name}_{PublisherId}";

        /// <summary>Package full name in the Windows format: Name_Version_Architecture__PublisherId.</summary>
        public string FullName => $"{Name}_{Version}_{Architecture}__{PublisherId}";

        public bool IsSameFamily(string familyName)
        {
            return !string.IsNullOrWhiteSpace(familyName) &&
                   string.Equals(FamilyName, familyName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Parses a package full name (Name_Version_Architecture_ResourceId_PublisherId). Resource packages and
        /// neutral/bundle identities are rejected because a GDK game package is always architecture specific.
        /// </summary>
        public static bool TryParseFullName(string fullName, out GdkPackageIdentity identity)
        {
            identity = null;
            if (string.IsNullOrWhiteSpace(fullName)) return false;

            string[] parts = fullName.Trim().Split('_');

            // Name _ Version _ Architecture _ ResourceId(empty) _ PublisherId
            if (parts.Length != 5) return false;
            if (string.IsNullOrWhiteSpace(parts[0])) return false;
            if (!string.IsNullOrEmpty(parts[3])) return false;
            if (!TryParseExactVersion(parts[1], out Version version)) return false;
            if (!IsGamePackageArchitecture(parts[2])) return false;
            if (string.IsNullOrWhiteSpace(parts[4])) return false;

            identity = new GdkPackageIdentity(parts[0], version, parts[2], parts[4]);
            return true;
        }

        /// <summary>Parses the identity from a GdkLinks resource URL (its file name is the package full name).</summary>
        public static bool TryParseFromUrl(string url, out GdkPackageIdentity identity)
        {
            identity = null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) return false;

            string fileName = Path.GetFileName(uri.AbsolutePath);
            if (!fileName.EndsWith(MsixvcExtension, StringComparison.OrdinalIgnoreCase)) return false;

            return TryParseFullName(Path.GetFileNameWithoutExtension(fileName), out identity);
        }

        private static bool TryParseExactVersion(string value, out Version version)
        {
            version = null;
            if (!Version.TryParse(value, out Version parsed)) return false;
            if (parsed.Build < 0 || parsed.Revision < 0) return false;
            version = parsed;
            return true;
        }

        private static bool IsGamePackageArchitecture(string architecture)
        {
            return string.Equals(architecture, "x64", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(architecture, "x86", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(architecture, "arm64", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(architecture, "arm", StringComparison.OrdinalIgnoreCase);
        }

        public bool Equals(GdkPackageIdentity other)
        {
            return other != null &&
                   string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                   Version == other.Version &&
                   string.Equals(Architecture, other.Architecture, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(PublisherId, other.PublisherId, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) => Equals(obj as GdkPackageIdentity);

        public override int GetHashCode()
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(Name),
                Version,
                StringComparer.OrdinalIgnoreCase.GetHashCode(Architecture),
                StringComparer.OrdinalIgnoreCase.GetHashCode(PublisherId));
        }

        public override string ToString() => FullName;
    }
}
