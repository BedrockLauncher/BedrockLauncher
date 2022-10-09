using System;

namespace BedrockLauncher.Exceptions
{
    public class AppInstallFailedException : PackageManagerException
    {
        public AppInstallFailedException(string message, Exception innerException) : base(message, innerException) { }
        public AppInstallFailedException(Exception innerException) : base(innerException.Message, innerException) { }

    };
}
