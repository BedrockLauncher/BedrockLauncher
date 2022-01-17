using System;

namespace BedrockLauncher.Exceptions
{
    public class PackageDownloadCanceledException : PackageManagerException
    {
        public PackageDownloadCanceledException(string message, Exception innerException) : base(message, innerException) { }
        public PackageDownloadCanceledException(Exception innerException) : base(innerException.Message, innerException) { }

    };
}
