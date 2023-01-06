using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System;

namespace JemExtensions
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