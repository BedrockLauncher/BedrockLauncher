using BedrockLauncher.UpdateProcessor.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    public sealed class MinecraftVersion : IComparable<MinecraftVersion>
    {
        private static readonly Regex ParseEx = new Regex(
            @"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)\.(?<revision>\d+)",
            RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(0.5));

        public long Major { get; }
        public long Minor { get; }
        public long Patch { get; }
        public long Revision { get; }

        public MinecraftVersion(long major, long minor = 0, long patch = 0, long revision = 0)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Revision = revision;
        }

        public override string ToString() => $"{Major}.{Minor}.{Patch}.{Revision}";

        public override bool Equals(object obj)
        {
            if (obj is MinecraftVersion other)
                return CompareTo(other) == 0;
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Revision);

        public int CompareTo(MinecraftVersion other)
        {
            if (other is null) return 1;

            int result = Major.CompareTo(other.Major);
            if (result != 0) return result;

            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;

            result = Patch.CompareTo(other.Patch);
            if (result != 0) return result;

            return Revision.CompareTo(other.Revision);
        }

        public static MinecraftVersion Parse(string version)
        {
            Match match = ParseEx.Match(version);
            if (!match.Success)
                throw new ArgumentException("Invalid version string.", nameof(version));

            long major    = long.Parse(match.Groups["major"].Value,    CultureInfo.InvariantCulture);
            long minor    = long.Parse(match.Groups["minor"].Value,    CultureInfo.InvariantCulture);
            long patch    = long.Parse(match.Groups["patch"].Value,    CultureInfo.InvariantCulture);
            long revision = long.Parse(match.Groups["revision"].Value, CultureInfo.InvariantCulture);

            return new MinecraftVersion(major, minor, patch, revision);
        }

        public static bool TryParse(string version, out MinecraftVersion ver)
        {
            ver = null;
            if (version == null) return false;

            Match match = ParseEx.Match(version);
            if (!match.Success) return false;

            if (!long.TryParse(match.Groups["major"].Value,    NumberStyles.Integer, CultureInfo.InvariantCulture, out long major))    return false;
            if (!long.TryParse(match.Groups["minor"].Value,    NumberStyles.Integer, CultureInfo.InvariantCulture, out long minor))    return false;
            if (!long.TryParse(match.Groups["patch"].Value,    NumberStyles.Integer, CultureInfo.InvariantCulture, out long patch))    return false;
            if (!long.TryParse(match.Groups["revision"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long revision)) return false;

            ver = new MinecraftVersion(major, minor, patch, revision);
            return true;
        }

        /// <summary>
        /// Extracts a MinecraftVersion from a UWP/Preview package moniker string.
        /// Returns version 0.0.0.0 if the moniker does not match the expected pattern.
        /// </summary>
        public static MinecraftVersion ConvertVersion(string packageMoniker, VersionType type)
        {
            Regex regex = Extensions.VersionDbExtensions.GetRegex(type);
            Match match = regex.Match(packageMoniker);
            // Fix: check match.Success instead of null (Regex.Match never returns null).
            if (!match.Success) return new MinecraftVersion(0, 0, 0, 0);

            if (long.TryParse(match.Groups[2].Value, out long major) &&
                long.TryParse(match.Groups[3].Value, out long minor) &&
                long.TryParse(match.Groups[4].Value, out long patch) &&
                long.TryParse(match.Groups[5].Value, out long revision))
            {
                return new MinecraftVersion(major, minor, patch, revision);
            }

            return new MinecraftVersion(0, 0, 0, 0);
        }

        /// <summary>
        /// Returns the human-readable version string (e.g. "1.21.120.20").
        /// Equivalent to <see cref="ToString"/> now that legacy version conversion is removed.
        /// </summary>
        public string ToRealString() => ToString();
    }
}
