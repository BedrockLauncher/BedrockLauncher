using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;
using BedrockLauncher.Exceptions;

namespace BedrockLauncher.Handlers
{
    internal enum GdkLaunchHelperState
    {
        AlreadyDeployed,
        Deployed
    }

    /// <summary>
    /// Puts BedrockLauncher-dll in place of the package's gamelaunchhelper.dll.
    ///
    /// An installed MSIXVC package starts through GameLaunchHelper.exe, which loads gamelaunchhelper.dll and calls
    /// its GameLaunch export. The stock library hands the launch to the Windows PC Bootstrapper, which (per the GDK
    /// documentation) updates the game to the latest available version before starting it. BedrockLauncher-dll keeps
    /// the bootstrapper duties the launcher needs (package working directory, single instance, foreground
    /// activation) without the update step, so the package that runs is the one the launcher validated.
    ///
    /// This class only deploys and verifies the file; process launch, instance detection and window activation are
    /// the DLL's job, and version management stays in the launcher.
    /// </summary>
    internal sealed class GdkLaunchHelperDeployment
    {
        public const string HelperExecutableName = "GameLaunchHelper.exe";
        public const string HelperLibraryName = "gamelaunchhelper.dll";
        public const string OriginalLibrarySuffix = ".original";
        public const string SupportedArchitecture = "x64";

        private readonly string sourceLibraryPath;
        private string sourceSha256;

        public GdkLaunchHelperDeployment(string sourceLibraryPath)
        {
            this.sourceLibraryPath = sourceLibraryPath;
        }

        public string SourceSha256 => sourceSha256 ??= ComputeSha256(RequireSource());

        /// <summary>Idempotent: when the deployed library already is BedrockLauncher-dll nothing is written.</summary>
        public GdkLaunchHelperState EnsureDeployed(string packageInstallLocation, string packageArchitecture, string applicationId)
        {
            RequireSource();

            if (!string.Equals(packageArchitecture, SupportedArchitecture, StringComparison.OrdinalIgnoreCase))
            {
                throw new GdkBootstrapException(
                    $"The launch helper is built for {SupportedArchitecture}; the installed package is {packageArchitecture}.");
            }

            if (string.IsNullOrWhiteSpace(packageInstallLocation) || !Directory.Exists(packageInstallLocation))
                throw new GdkBootstrapException($"The package install location '{packageInstallLocation}' does not exist.");

            string executable = ReadApplicationExecutable(packageInstallLocation, applicationId);
            if (!string.Equals(Path.GetFileName(executable), HelperExecutableName, StringComparison.OrdinalIgnoreCase))
            {
                throw new GdkBootstrapException(
                    $"The installed package starts '{executable ?? "<none>"}' instead of {HelperExecutableName}, so the launch " +
                    "helper cannot take over the bootstrapper and the version cannot be guaranteed.");
            }

            if (!File.Exists(Path.Combine(packageInstallLocation, HelperExecutableName)))
                throw new GdkBootstrapException($"{HelperExecutableName} was not found in '{packageInstallLocation}'.");

            string target = Path.Combine(packageInstallLocation, HelperLibraryName);

            if (File.Exists(target) && IsDeployed(target))
                return GdkLaunchHelperState.AlreadyDeployed;

            try
            {
                string original = target + OriginalLibrarySuffix;
                if (File.Exists(target) && !File.Exists(original))
                    File.Copy(target, original);

                string temporary = target + ".bedrocklauncher.tmp";
                File.Copy(sourceLibraryPath, temporary, overwrite: true);
                File.Move(temporary, target, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new GdkBootstrapException(
                    $"Could not write {HelperLibraryName} in '{packageInstallLocation}': {ex.Message}", ex);
            }

            if (!IsDeployed(target))
                throw new GdkBootstrapException($"{target} does not match BedrockLauncher-dll after deployment.");

            return GdkLaunchHelperState.Deployed;
        }

        public bool IsDeployed(string libraryPath)
        {
            return File.Exists(libraryPath) &&
                   string.Equals(ComputeSha256(libraryPath), SourceSha256, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Executable of the given application in the installed AppxManifest.xml (null if not found).</summary>
        internal static string ReadApplicationExecutable(string packageInstallLocation, string applicationId)
        {
            string manifestPath = Path.Combine(packageInstallLocation, "AppxManifest.xml");
            if (!File.Exists(manifestPath))
                return null;

            XDocument manifest = XDocument.Load(manifestPath);
            XElement application = manifest.Descendants()
                .Where(x => x.Name.LocalName == "Application")
                .FirstOrDefault(x => string.IsNullOrWhiteSpace(applicationId) ||
                                     string.Equals((string)x.Attribute("Id"), applicationId, StringComparison.OrdinalIgnoreCase));

            return (string)application?.Attribute("Executable");
        }

        private string RequireSource()
        {
            if (string.IsNullOrWhiteSpace(sourceLibraryPath) || !File.Exists(sourceLibraryPath))
                throw new GdkBootstrapException($"BedrockLauncher-dll was not found at '{sourceLibraryPath}'. Reinstall the launcher.");

            return sourceLibraryPath;
        }

        private static string ComputeSha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
    }
}
