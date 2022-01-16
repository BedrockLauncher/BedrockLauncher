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
            string dllImport = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Runtimes", "WUTokenHelper", Environment.Is64BitProcess ? "x64" : "Win32", "WUTokenHelper.dll");
            InteropExtensions.LoadLibrary(dllImport);
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
