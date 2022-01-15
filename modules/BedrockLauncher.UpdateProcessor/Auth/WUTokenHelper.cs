using ExtensionsDotNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.UpdateProcessor.Auth
{
    public class WUTokenHelper
    {
        static WUTokenHelper() { Init(); }
        private static void Init()
        {
            var myPath = new Uri(AppDomain.CurrentDomain.BaseDirectory).LocalPath;
            var myFolder = Path.Combine(Path.GetDirectoryName(myPath), "runtime", "WUTokenHelper");

            var is64 = Environment.Is64BitProcess;
            var subfolder = is64 ? "\\x64\\" : "\\Win32\\";
            var path = myFolder + subfolder + "WUTokenHelper.dll";

            InteropExtensions.LoadLibrary(path);
        }




        [DllImport("WUTokenHelper.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int GetWUToken(int userIndex, [MarshalAs(UnmanagedType.LPWStr)] out string token);

        [DllImport("WUTokenHelper.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int GetTotalWUAccounts();

        [DllImport("WUTokenHelper.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public static extern string GetWUAccountUserName(int userIndex);

        [DllImport("WUTokenHelper.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public static extern string GetWUProviderName(int userIndex);
    }
}
