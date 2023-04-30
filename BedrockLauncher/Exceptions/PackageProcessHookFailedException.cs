using System;

namespace BedrockLauncher.Exceptions
{
    public class PackageProcessHookFailedException : PackageManagerException
    {
        public PackageProcessHookFailedException(string message, Exception innerException) : base(message, innerException) { }
        public PackageProcessHookFailedException(Exception innerException) : base(innerException.Message, innerException) { }
    }
}
