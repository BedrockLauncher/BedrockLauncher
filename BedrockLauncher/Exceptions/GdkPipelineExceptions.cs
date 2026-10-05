using System;

namespace BedrockLauncher.Exceptions
{
    /// <summary>The Microsoft account or the Store license required for a GDK version could not be confirmed.</summary>
    public class GdkEntitlementException : PackageManagerException
    {
        public GdkEntitlementException(string message) : base(message, new InvalidOperationException(message)) { }
    }

    /// <summary>
    /// No supported source exists for a GDK version: Windows has no copy of it, the launcher has no archived copy,
    /// and the version list has no download resource for it. Never thrown before those three sources were checked.
    /// </summary>
    public class GdkVersionUnavailableException : PackageManagerException
    {
        public GdkVersionUnavailableException(string message) : base(message, new InvalidOperationException(message)) { }
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
