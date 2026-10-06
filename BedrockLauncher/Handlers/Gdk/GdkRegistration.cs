using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BedrockLauncher.Handlers
{
    /// <summary>
    /// Helpers for the officially-installed GDK package: reading its activation application id, removing it while
    /// keeping user data, and activating it through Windows. The launcher never builds a loose/local GDK package, so
    /// there is no manifest-generation or loose-registration code here — Gaming Services installs GDK content itself.
    /// </summary>
    internal static class GdkRegistration
    {
        internal static string GetApplicationId(string packageDirectory)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(packageDirectory) || !Directory.Exists(packageDirectory))
                    return "Game";

                string manifestPath = Directory.EnumerateFiles(packageDirectory, "AppxManifest.xml", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.EnumerateFiles(packageDirectory, "appxmanifest.xml", SearchOption.TopDirectoryOnly))
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(manifestPath))
                {
                    XDocument manifest = XDocument.Load(manifestPath);
                    string manifestAppId = manifest.Descendants()
                        .FirstOrDefault(x => x.Name.LocalName == "Application")?
                        .Attribute("Id")?
                        .Value;

                    if (!string.IsNullOrWhiteSpace(manifestAppId))
                        return manifestAppId.Trim();
                }

                string configPath = Path.Combine(packageDirectory, "MicrosoftGame.Config");
                if (File.Exists(configPath))
                {
                    XDocument gameConfig = XDocument.Load(configPath);
                    string configAppId = gameConfig.Descendants()
                        .FirstOrDefault(x =>
                            x.Name.LocalName == "Executable" &&
                            string.Equals((string)x.Attribute("TargetDeviceFamily"), "PC", StringComparison.OrdinalIgnoreCase))?
                        .Attribute("Id")?
                        .Value;

                    if (!string.IsNullOrWhiteSpace(configAppId))
                        return configAppId.Trim();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Unable to read GDK application id: {ex}");
            }

            return "Game";
        }

        /// <summary>Removes a registration but keeps the user's application data.</summary>
        internal static async Task UnregisterAsync(string packageFullName)
        {
            string command = $"Remove-AppxPackage -Package '{PowerShellPackageCommand.Escape(packageFullName)}' -PreserveApplicationData";
            await PowerShellPackageCommand.RunAsync(command, "GDK package removal");
        }

        /// <summary>Starts the installed package through its Start-menu activation, so the game gets its package identity.</summary>
        internal static bool Activate(string packageFamilyName, string applicationId)
        {
            try
            {
                using Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $@"shell:AppsFolder\{packageFamilyName}!{applicationId}",
                    UseShellExecute = false
                });

                return process != null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"GDK package activation failed: {ex}");
                return false;
            }
        }
    }
}
