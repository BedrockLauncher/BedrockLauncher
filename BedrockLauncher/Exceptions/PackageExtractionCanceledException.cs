using System;

namespace BedrockLauncher.Exceptions
{
    public class PackageExtractionCanceledException : PackageManagerException
    {
        public PackageExtractionCanceledException(string message, Exception innerException) : base(message, innerException) { }
        public PackageExtractionCanceledException(Exception innerException) : base(innerException.Message, innerException) { }

    };
}
