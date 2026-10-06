using BedrockLauncher.UpdateProcessor.Extensions;
using System;
using System.IO;
using System.Runtime.InteropServices;

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

        public static string GetWUAccountId(int userIndex)
        {
            return isTokenBrokerAvailable
                ? NativeGetWUAccountId(userIndex)
                : string.Empty;
        }

        public static string GetWUAccountUserName(int userIndex)
        {
            return isTokenBrokerAvailable ? NativeGetWUAccountUserName(userIndex) : string.Empty;
        }

        public static string GetWUProviderName(int userIndex)
        {
            return isTokenBrokerAvailable ? NativeGetWUProviderName(userIndex) : string.Empty;
        }

        [DllImport(
            DLLName,
            EntryPoint = "GetWUToken",
            CallingConvention = CallingConvention.StdCall)]
        private static extern int NativeGetWUToken(int userIndex, [MarshalAs(UnmanagedType.LPWStr)] out string token);

        [DllImport(
            DLLName,
            EntryPoint = "GetTotalWUAccounts",
            CallingConvention = CallingConvention.StdCall)]
        private static extern int NativeGetTotalWUAccounts();

        [DllImport(
            DLLName,
            EntryPoint = "GetWUAccountID",
            CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string NativeGetWUAccountId(int userIndex);

        [DllImport(
            DLLName,
            EntryPoint = "GetWUAccountUserName",
            CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string NativeGetWUAccountUserName(int userIndex);

        [DllImport(
            DLLName,
            EntryPoint = "GetWUProviderName",
            CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string NativeGetWUProviderName(int userIndex);
    }
}