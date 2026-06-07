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
        private const string DLLName = "BedrockLauncher.TokenBroker.dll";
        private const string RuntimesDirName = "Runtimes";
        private static bool isTokenBrokerAvailable;

        private static string GetEnv()
        {
            return Environment.Is64BitProcess ? "win-x64" : "win-x86";
        }

        static AuthenticationTokenHelper() { Init(); }

        private static void Init()
        {
            string dllImport = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RuntimesDirName, GetEnv(), DLLName);
            if (!File.Exists(dllImport))
            {
                isTokenBrokerAvailable = false;
                return;
            }

            try
            {
                InteropExtensions.LoadLibrary(dllImport);
                isTokenBrokerAvailable = true;
            }
            catch
            {
                isTokenBrokerAvailable = false;
            }
        }

        public static int GetWUToken(int userIndex, [MarshalAs(UnmanagedType.LPWStr)] out string token)
        {
            token = string.Empty;
            return isTokenBrokerAvailable ? NativeGetWUToken(userIndex, out token) : AuthenticationTokenException.WU_NO_ACCOUNT;
        }

        public static int GetTotalWUAccounts()
        {
            return isTokenBrokerAvailable ? NativeGetTotalWUAccounts() : 0;
        }

        public static string GetWUAccountUserName(int userIndex)
        {
            return isTokenBrokerAvailable ? NativeGetWUAccountUserName(userIndex) : string.Empty;
        }

        public static string GetWUProviderName(int userIndex)
        {
            return isTokenBrokerAvailable ? NativeGetWUProviderName(userIndex) : string.Empty;
        }

        [DllImport(DLLName, CallingConvention = CallingConvention.StdCall)]
        private static extern int NativeGetWUToken(int userIndex, [MarshalAs(UnmanagedType.LPWStr)] out string token);

        [DllImport(DLLName, CallingConvention = CallingConvention.StdCall)]
        private static extern int NativeGetTotalWUAccounts();

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string NativeGetWUAccountUserName(int userIndex);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string NativeGetWUProviderName(int userIndex);
    }
}
