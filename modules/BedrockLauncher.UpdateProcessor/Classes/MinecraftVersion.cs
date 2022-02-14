using BedrockLauncher.UpdateProcessor.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    public sealed class MinecraftVersion : IComparable<MinecraftVersion>
    {
        private static readonly Regex ParseEx = new Regex("^(?<major>\\d+).(?<minor>\\d+).(?<patch>\\d+).(?<revision>\\d+)", RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(0.5));

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

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}.{Revision}";
        }
        public int CompareTo(MinecraftVersion other)
        {
            if ((object)other == null)
            {
                return 1;
            }

            int num = Major.CompareTo(other.Major);
            if (num != 0)
            {
                return num;
            }

            num = Minor.CompareTo(other.Minor);
            if (num != 0)
            {
                return num;
            }

            num = Patch.CompareTo(other.Patch);
            if (num != 0)
            {
                return num;
            }

            return Revision.CompareTo(other.Revision);
        }
        public static MinecraftVersion Parse(string version)
        {
            Match match = ParseEx.Match(version);
            if (!match.Success)
            {
                throw new ArgumentException("Invalid version.", "version");
            }

            long major = long.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture);
            long minor = long.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture);
            long patch = long.Parse(match.Groups["patch"].Value, CultureInfo.InvariantCulture);
            long revision = long.Parse(match.Groups["revision"].Value, CultureInfo.InvariantCulture);



            return new MinecraftVersion(major, minor, patch, revision);
        }
        public static bool TryParse(string version, out MinecraftVersion ver)
        {
            ver = null;
            if (version == null)
            {
                return false;
            }

            Match match = ParseEx.Match(version);
            if (!match.Success)
            {
                return false;
            }

            if (!long.TryParse(match.Groups["major"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long major)) return false;
            if (!long.TryParse(match.Groups["minor"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long minor)) return false;
            if (!long.TryParse(match.Groups["patch"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long patch)) return false;
            if (!long.TryParse(match.Groups["revision"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long revision)) return false;
            ver = new MinecraftVersion(major, minor, patch, revision);
            return true;
        }
        public static MinecraftVersion ConvertVersion(string packageMonker, VersionType type)
        {
            Regex regex = Extensions.VersionDbExtensions.GetRegex(type);
            Match match;
            match = regex.Match(packageMonker);
            if (match == null) return new MinecraftVersion(0, 0, 0, 0);

            string major_s = match.Groups[2].Value;
            string minor_s = match.Groups[3].Value;
            string patch_s = match.Groups[4].Value;
            string revision_s = match.Groups[5].Value;

            if (long.TryParse(major_s, out long major_i) && long.TryParse(minor_s, out long minor_i) && long.TryParse(patch_s, out long patch_i) && long.TryParse(revision_s, out long revision_i))
            {
                return new MinecraftVersion(major_i, minor_i, patch_i, revision_i);
            }
            else return new MinecraftVersion(0, 0, 0, 0);
        }

        public string ToRealString()
        {
            int major;
            int minor;
            int patch;
            int revision;

            int major_i = Convert.ToInt32(Major);
            int minor_i = Convert.ToInt32(Minor);
            int patch_i = Convert.ToInt32(Patch);

            if (major_i == 0 && minor_i < 1000)
            {
                major = major_i;
                minor = minor_i / 10;
                patch = minor_i % 10;
                revision = patch_i;
            }
            else if (major_i == 0)
            {
                major = major_i;
                minor = minor_i / 100;
                patch = minor_i % 100;
                revision = patch_i;
            }
            else
            {
                major = major_i;
                minor = minor_i;
                patch = patch_i / 100;
                revision = patch_i % 100;
            }
            return $"{major}.{minor}.{patch}.{revision}";
        }
    }
}
