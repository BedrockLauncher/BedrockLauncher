using System;

namespace BedrockLauncher.Exceptions
{
    public class SaveRedirectionFailedException : Exception
    {
        public SaveRedirectionFailedException(string message, Exception innerException) : base(message, innerException) { }
        public SaveRedirectionFailedException(Exception innerException) : base(innerException.Message, innerException) { }

    };
}
