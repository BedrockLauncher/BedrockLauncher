using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Semver;
using BedrockLauncher.UpdateProcessor.Classes;
using System.Runtime.InteropServices;

namespace BedrockLauncher.UpdateProcessor.Extensions
{
    public class VersionDbExtensions
    {
        public static string FallbackArch => "???";
        public static string GetVersionArch(string packageMoniker)
        {
            Regex regex = new Regex(@"(Microsoft\.MinecraftUWP_([0-9]+)\.([0-9]+)\.([0-9]+)\.([0-9]+)_(.*)__8wekyb3d8bbwe.*)");
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
