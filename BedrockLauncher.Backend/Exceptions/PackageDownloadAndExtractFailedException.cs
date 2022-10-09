using System;

namespace BedrockLauncher.Exceptions
{
    public class PackageDownloadAndExtractFailedException : PackageManagerException
    {
        public PackageDownloadAndExtractFailedException(string message, Exception innerException) : base(message, innerException) { }
        public PackageDownloadAndExtractFailedException(Exception innerException) : base(innerException.Message, innerException) { }
    }
}
