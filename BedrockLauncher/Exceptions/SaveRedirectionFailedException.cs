using System;

namespace BedrockLauncher.Exceptions
{
    public class SaveRedirectionFailedException : PackageManagerException
    {
        public SaveRedirectionFailedException(string message, Exception innerException) : base(message, innerException) { }
        public SaveRedirectionFailedException(Exception innerException) : base(innerException.Message, innerException) { }

    };
}
