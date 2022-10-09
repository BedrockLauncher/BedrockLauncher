using System;

namespace BedrockLauncher.Exceptions
{
    public class PackageRemovalFailedException : PackageManagerException
    {
        public PackageRemovalFailedException(string message, Exception innerException) : base(message, innerException) { }
        public PackageRemovalFailedException(Exception innerException) : base(innerException.Message, innerException) { }

    };
}
