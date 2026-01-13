using System;

namespace BedrockLauncher.Exceptions
{
    internal class NoVersionAccessibleException : Exception
    {
        public NoVersionAccessibleException(string message): base(message) { }
    }
}
