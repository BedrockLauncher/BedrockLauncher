using System;
using System.Runtime.InteropServices;

namespace JemExtensions
{
    public static class InteropExtensions
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);
    }
}
