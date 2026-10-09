using System;

namespace BedrockLauncher.Exceptions
{
    /// <summary>
    /// The required GDK package is not installed and the catalog lists no download resource for exactly that package.
    /// Never thrown before the installed packages were checked.
    /// </summary>
    public class GdkVersionUnavailableException : PackageManagerException
    {
        public GdkVersionUnavailableException(string message) : base(message, new InvalidOperationException(message)) { }
    }

    /// <summary>A GDK version has no persisted required package identity, so the launcher cannot know which package to use.</summary>
    public class GdkRequirementUnresolvedException : PackageManagerException
    {
        public GdkRequirementUnresolvedException(string message) : base(message, new InvalidOperationException(message)) { }
    }

    /// <summary>
    /// The package Windows has registered is not the package the GDK version requires. The launch is blocked: the
    /// launcher never runs a different GDK build in place of the required one.
    /// </summary>
    public class GdkVersionMismatchException : PackageManagerException
    {
        public string RequiredPackage { get; }
        public string InstalledPackage { get; }

        public GdkVersionMismatchException(string requiredPackage, string installedPackage, string reason)
            : base(BuildMessage(requiredPackage, installedPackage, reason), new InvalidOperationException(reason))
        {
            RequiredPackage = requiredPackage;
            InstalledPackage = installedPackage;
        }

        private static string BuildMessage(string requiredPackage, string installedPackage, string reason) =>
            $"Required GDK: {requiredPackage}{Environment.NewLine}" +
            $"Installed GDK: {installedPackage ?? "none"}{Environment.NewLine}" +
            reason;
    }

    /// <summary>The launch helper (BedrockLauncher-dll) could not be put in place for the installed GDK package.</summary>
    public class GdkBootstrapException : PackageManagerException
    {
        public GdkBootstrapException(string message, Exception innerException = null)
            : base(message, innerException ?? new InvalidOperationException(message)) { }
    }

    /// <summary>Windows (the package deployment service) refused to install the downloaded GDK package.</summary>
    public class GdkDeploymentRejectedException : PackageManagerException
    {
        /// <summary>HRESULT reported by Windows, or 0 when Windows reported none.</summary>
        public int DeploymentHResult { get; }

        public GdkDeploymentRejectedException(string message, int deploymentHResult, Exception innerException)
            : base(message, innerException)
        {
            DeploymentHResult = deploymentHResult;
        }
    }
}
