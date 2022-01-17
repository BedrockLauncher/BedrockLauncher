using System;

namespace BedrockLauncher.Exceptions
{
    public class PackageRegistrationFailedException : PackageManagerException
    {
        public PackageRegistrationFailedException(string message, Exception innerException) : base(message, innerException) { }
        public PackageRegistrationFailedException(Exception innerException) : base(innerException.Message, innerException) { }

    };
}
