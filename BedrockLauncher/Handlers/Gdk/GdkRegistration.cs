using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        /// <summary>
        /// Starts the installed package like the Start menu does, so the game gets its package identity. Returns the
        /// process Windows started (GameLaunchHelper.exe), or null when it exited before it could be opened.
        /// Throws when Windows refuses the activation (the HRESULT says why, e.g. Gaming Services missing).
        /// </summary>
        internal static Process Activate(string packageFamilyName, string applicationId)
        {
            string appUserModelId = $"{packageFamilyName}!{applicationId}";
            var manager = (IApplicationActivationManager)new ApplicationActivationManager();

            try
            {
                int hresult = manager.ActivateApplication(appUserModelId, null, ActivateOptionsNone, out uint processId);
                if (hresult < 0)
                    throw new Win32Exception(hresult, $"Windows could not start {appUserModelId} (HRESULT 0x{hresult:X8}): {new Win32Exception(hresult).Message}");

                try
                {
                    return Process.GetProcessById((int)processId);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }
            finally
            {
                Marshal.ReleaseComObject(manager);
            }
        }

        private const int ActivateOptionsNone = 0;

        [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IApplicationActivationManager
        {
            [PreserveSig]
            int ActivateApplication(
                [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
                [MarshalAs(UnmanagedType.LPWStr)] string arguments,
                int options,
                out uint processId);
        }

        [ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
        private class ApplicationActivationManager
        {
        }
    }
}
