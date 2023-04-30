using System;

namespace BedrockLauncher.Exceptions
{
    public class PackageDeregistrationFailedException : PackageManagerException
    {
        public PackageDeregistrationFailedException(string message, Exception innerException) : base(message, innerException) { }
        public PackageDeregistrationFailedException(Exception innerException) : base(innerException.Message, innerException) { }

    };
}
