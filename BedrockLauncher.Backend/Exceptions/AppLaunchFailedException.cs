using System;

namespace BedrockLauncher.Exceptions
{
    public class AppLaunchFailedException : PackageManagerException
    {
        public AppLaunchFailedException(string message, Exception innerException) : base(message, innerException) { }
        public AppLaunchFailedException(Exception innerException) : base(innerException.Message, innerException) { }

    };
}
