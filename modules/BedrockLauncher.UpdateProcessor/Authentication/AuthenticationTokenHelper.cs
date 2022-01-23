using BedrockLauncher.UpdateProcessor.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.UpdateProcessor.Authentication
{
    public class AuthenticationTokenHelper
    {
        private const string DLLName = "TokenBroker.dll";
        static AuthenticationTokenHelper() { Init(); }
        private static void Init()
        {
            string dllImport = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Runtimes", "WUTokenHelper", Environment.Is64BitProcess ? "x64" : "Win32", DLLName);
            InteropExtensions.LoadLibrary(dllImport);
        }


        [DllImport(DLLName, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetWUToken(int userIndex, [MarshalAs(UnmanagedType.LPWStr)] out string token);

        [DllImport(DLLName, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetTotalWUAccounts();

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public static extern string GetWUAccountUserName(int userIndex);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public static extern string GetWUProviderName(int userIndex);
    }
}
