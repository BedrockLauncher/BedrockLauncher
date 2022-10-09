using System;

namespace BedrockLauncher.Exceptions
{
    public class PackageExtractionFailedException : PackageManagerException
    {
        public PackageExtractionFailedException(string message, Exception innerException) : base(message, innerException) { }
        public PackageExtractionFailedException(Exception innerException) : base(innerException.Message, innerException) { }

    };
}
