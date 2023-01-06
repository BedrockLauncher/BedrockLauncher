using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Semver;
using BedrockLauncher.UpdateProcessor.Classes;
using System.Runtime.InteropServices;
using BedrockLauncher.UpdateProcessor.Enums;

namespace BedrockLauncher.UpdateProcessor.Extensions
{
    public class VersionDbExtensions
    {
        public static Regex GetRegex(VersionType type)
        {
            var id = type == VersionType.Preview ? @"Microsoft\.MinecraftWindowsBeta_" : @"Microsoft\.MinecraftUWP_";
            Regex regex = new Regex(@$"({id}([0-9]+)\.([0-9]+)\.([0-9]+)\.([0-9]+)_(.*)__8wekyb3d8bbwe.*)");
            return regex;
        }
        public static string FallbackArch => "???";
        public static string GetVersionArch(string packageMoniker, VersionType versionType)
        {
            Regex regex = GetRegex(versionType);
            Match match = regex.Match(packageMoniker);
            if (match == null) return FallbackArch;
            return match.Groups[6].Value;
        }
        public static bool DoesVerionArchMatch(string sourceArch, string targetArch)
        {
            bool result = sourceArch.Equals(targetArch);
            return result; 
        }
    }
}
