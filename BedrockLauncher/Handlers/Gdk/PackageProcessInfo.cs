using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BedrockLauncher.Handlers
{
    /// <summary>Reads the package identity a running process was started with.</summary>
    internal static class PackageProcessInfo
    {
        private const uint ProcessQueryLimitedInformation = 0x1000;
        private const int ErrorSuccess = 0;
        private const int ErrorInsufficientBuffer = 122;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPackageFullName(IntPtr process, ref uint packageFullNameLength, StringBuilder packageFullName);

        /// <summary>The package full name of the process, or null when it has no package identity or cannot be opened.</summary>
        public static string GetPackageFullName(int processId)
        {
            IntPtr process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
            if (process == IntPtr.Zero)
                return null;

            try
            {
                uint length = 0;
                int result = GetPackageFullName(process, ref length, null);
                if (result != ErrorInsufficientBuffer)
                    return null;

                var buffer = new StringBuilder((int)length);
                result = GetPackageFullName(process, ref length, buffer);
                return result == ErrorSuccess ? buffer.ToString() : null;
            }
            finally
            {
                CloseHandle(process);
            }
        }
    }
}
