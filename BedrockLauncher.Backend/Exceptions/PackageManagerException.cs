using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Exceptions
{
    public class PackageManagerException : Exception
    {
        public PackageManagerException(string message, Exception innerException) : base(message, innerException) { }
        public PackageManagerException(Exception innerException) : base(innerException.Message, innerException) { }
    }
}
