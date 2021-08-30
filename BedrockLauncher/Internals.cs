using BedrockLauncher.Downloaders;
using BedrockLauncher.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BedrockLauncher.Core.Language;
using BedrockLauncher.Methods;

namespace BedrockLauncher
{
    public static class Internals
    {

        #region Bugrock Of The Week

        private static bool _IsBugRockOfTheWeek = false;
        public static void Init_IsBugRockOfTheWeek()
        {
            _IsBugRockOfTheWeek = ChangelogDownloader.GetBedrockOfTheWeekStatus();
        }
        public static bool IsBugRockOfTheWeek()
        {
            return _IsBugRockOfTheWeek;
        }

        #endregion

        #region Startup Checks

        public static void ValidateOSArchitecture()
        {
            var Architecture = RuntimeInformation.OSArchitecture;
            bool canRun;
            switch (Architecture)
            {
                case Architecture.Arm:
                    ShowError("Unsupported Architexture", "This application can not run on ARM computers");
                    canRun = false;
                    break;
                case Architecture.Arm64:
                    ShowError("Unsupported Architexture", "This application can not run on ARM computers");
                    canRun = false;
                    break;
                case Architecture.X86:
                    ShowError("Unsupported Architexture", "This application can not run on x86 / 32-bit computers");
                    canRun = false;
                    break;
                case Architecture.X64:
                    canRun = true;
                    break;
                default:
                    ShowError("Unsupported Architexture", "Unable to determine architexture, not supported");
                    canRun = false;
                    break;
            }

            if (!canRun) Environment.Exit(0);


            void ShowError(string title, string message)
            {
                MessageBox.Show(message, title);
            }
        }
        public static void EnableDeveloperMode()
        {
            try
            {
                string value64 = string.Empty;
                RegistryKey localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                localKey = localKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock", true);
                if (localKey != null)
                {
                    switch (localKey.GetValue("AllowDevelopmentWithoutDevLicense"))
                    {
                        case 0:
                            System.Diagnostics.Debug.WriteLine("Developer mode disabled, trying to turn on");
                            localKey.SetValue("AllowDevelopmentWithoutDevLicense", 1);
                            break;
                        case null:
                            localKey.SetValue("AllowDevelopmentWithoutDevLicense", 1, RegistryValueKind.DWord);
                            break;
                    }
                }
            }
            catch (Exception r)
            {
                string message = "Cant enable developer mode for X64 machine Error: " + r;
                System.Diagnostics.Debug.WriteLine(message);
                MessageBox.Show(message + r);
                throw r;
            }
        }

        #endregion

        #region Errors

        public static void Error_WebView2RuntimeMissing()
        {
            string link = @"https://go.microsoft.com/fwlink/p/?LinkId=2124703";
            string message = "The WebView2 runtime can not be found. Please install the WebView2 Runtime! \r\n\r\n" +
                link + "\r\n\r\n" +
                "(Click 'Yes' to open the web browser to download it)";
            string title = "Missing WebView2 Runtime";
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Error);
            if (result == MessageBoxResult.Yes) Process.Start(link);
            Environment.Exit(0);
        }
        public static void Error_Unhandled(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException) Error_WebView2RuntimeMissing();

            Debug.WriteLine(e.ToString());
            string message = e.Exception.Message + Environment.NewLine + Environment.NewLine + e.ToString();
            string title = e.Exception.HResult.ToString();

            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region Enviorment Globals

        public async static Task<CoreWebView2Environment> GetCoreWebView2Environment()
        {
            var op = new CoreWebView2EnvironmentOptions("--disable-web-security");
            string cache_folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BedrockLauncher", "cache");
            return await CoreWebView2Environment.CreateAsync(null, cache_folder, op);
        }

        #endregion

        #region Inits

        #endregion

        public static async void OnStartup(object sender, StartupEventArgs e)
        {
            EnableDeveloperMode();
            Init_IsBugRockOfTheWeek();

            LanguageManager.Init();
            LauncherModel.Default.LoadConfig();
            await LauncherModel.Default.LoadVersions();

            Debug.WriteLine("Application Pre-Initalization Finished!");
            ConsoleArgumentManager.PraseArgs(e.Args);
        }
        public static void StartLogging()
        {
            if (File.Exists("Log.txt")) { File.Delete("Log.txt"); }
            Debug.Listeners.Add(new TextWriterTraceListener("Log.txt"));
            Debug.AutoFlush = true;
        }
    }
}
