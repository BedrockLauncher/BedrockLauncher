using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System;
using Microsoft.Win32;

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
            Directory = 1,
            AllowUnprivilegedCreate = 2
        }

        /// <summary>
        /// Creates a symbolic link, attempting unprivileged creation first if Developer Mode is enabled
        /// </summary>
        public static bool CreateSymbolicLinkSafe(string linkPath, string targetPath, SymbolicLinkType linkType)
        {
            // First try with unprivileged flag if Developer Mode is enabled
            if (IsDeveloperModeEnabled())
            {
                var unprivilegedFlags = linkType | SymbolicLinkType.AllowUnprivilegedCreate;
                if (CreateSymbolicLink(linkPath, targetPath, unprivilegedFlags))
                {
                    return true;
                }
            }

            // Fallback to traditional method (requires admin if Developer Mode is not enabled)
            return CreateSymbolicLink(linkPath, targetPath, linkType);
        }

        /// <summary>
        /// Checks if Windows Developer Mode is enabled
        /// </summary>
        private static bool IsDeveloperModeEnabled()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock"))
                {
                    if (key?.GetValue("AllowDevelopmentWithoutDevLicense") is int value)
                        return value == 1;
                }
            }
            catch
            {
                // If we can't read the registry, assume Developer Mode is not enabled
            }
            return false;
        }
    }
}
