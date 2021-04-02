using System.Runtime.InteropServices;
using System.IO;

namespace BedrockLauncher.Methods
{
    public class SymLinkHelper
    {
        [DllImport("kernel32.dll")]
        public static extern bool CreateSymbolicLink(
        string lpSymlinkFileName, string lpTargetFileName, SymbolicLinkType dwFlags);

        public enum SymbolicLinkType
        {
            File = 0,
            Directory = 1
        }
    }
}