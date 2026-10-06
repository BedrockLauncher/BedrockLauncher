using BedrockLauncher.UpdateProcessor.Enums;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace BedrockLauncher.UpdateProcessor.Extensions
{
    public static class VersionDbExtensions
    {
        private static readonly ConcurrentDictionary<VersionType, Regex>
            RegexCache = new ConcurrentDictionary<VersionType, Regex>();

        public const string FallbackArch = "???";

        public static Regex GetRegex(VersionType type)
        {
            return RegexCache.GetOrAdd(type, CreateRegex);
        }

        private static Regex CreateRegex(VersionType type)
        {
            string packageName =
                type == VersionType.Preview
                    ? @"Microsoft\.MinecraftWindowsBeta_"
                    : @"Microsoft\.MinecraftUWP_";

            return new Regex(
                @$"({packageName}" +
                @"([0-9]+)\.([0-9]+)\.([0-9]+)\.([0-9]+)_" +
                @"(.*)__8wekyb3d8bbwe.*)",
                RegexOptions.CultureInvariant,
                TimeSpan.FromSeconds(1));
        }

        public static string GetVersionArch(
            string packageMoniker,
            VersionType versionType)
        {
            if (string.IsNullOrWhiteSpace(packageMoniker))
                return FallbackArch;

            Match match = GetRegex(versionType).Match(packageMoniker);

            if (!match.Success)
                return FallbackArch;

            string architecture = match.Groups[6].Value;

            return string.IsNullOrWhiteSpace(architecture)
                ? FallbackArch
                : architecture;
        }

        public static bool DoesVersionArchMatch(
            string sourceArch,
            string targetArch)
        {
            return string.Equals(
                sourceArch,
                targetArch,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}