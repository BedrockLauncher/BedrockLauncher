using BedrockLauncher.Downloaders;
using BedrockLauncher.ViewModels;
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

        public static void Error_Unhandled(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {

            Debug.WriteLine(e.ToString());
            string message = e.Exception.Message + Environment.NewLine + Environment.NewLine + e.ToString();
            string title = e.Exception.HResult.ToString();

            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region Inits

        #endregion

        public static async void OnStartup(object sender, StartupEventArgs e)
        {
            EnableDeveloperMode();
            CefSharp.CefSharpLoader.Init();
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
