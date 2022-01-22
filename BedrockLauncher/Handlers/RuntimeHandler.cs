using BedrockLauncher.Downloaders;
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

namespace BedrockLauncher.Handlers
{
    public static class RuntimeHandler
    {
        private static bool IsBugrockEnabled = false; 
        public static bool IsBugRockOfTheWeek()
        {
            return IsBugrockEnabled;
        }
        public static async Task InitalizeBugRockOfTheWeek()
        {
            IsBugrockEnabled = await ChangelogDownloader.GetBedrockOfTheWeekStatus();
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
                throw new Exception(message, r);
            }
        }
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
        public static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Debug.WriteLine(e.ToString());
            string message = e.Exception.Message + Environment.NewLine + Environment.NewLine + e.ToString();
            string title = e.Exception.HResult.ToString();

            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        public static void StartLogging()
        {
            if (File.Exists("Log.txt")) { File.Delete("Log.txt"); }
            //TODO: Reimplement Logging
            //Debug..Add(new TextWriterTraceListener("Log.txt"));
            Debug.AutoFlush = true;
        }
    }
}
