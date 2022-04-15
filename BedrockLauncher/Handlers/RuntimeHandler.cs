using BedrockLauncher.Downloaders;
using Microsoft.Win32;
using NLog;
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
        static TraceSwitch traceSwitch = new TraceSwitch("General", "Entire Application") { Level = TraceLevel.Verbose };

        private static bool IsBugrockEnabled = false; 
        public static bool IsBugRockOfTheWeek()
        {
            return IsBugrockEnabled;
        }
        public static async Task InitalizeBugRockOfTheWeek()
        {
            IsBugrockEnabled = await ChangelogDownloader.GetBedrockOfTheWeekStatus();
        }

        public static void LogStartupInformation()
        {
            Trace.WriteLine("Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            Trace.WriteLine("Git Repo: " + ThisAssembly.Git.RepositoryUrl);
            Trace.WriteLine("Git Branch: " + ThisAssembly.Git.Branch);
            Trace.WriteLine("Git Commit: " + ThisAssembly.Git.Commit);
            Trace.WriteLine("Git Sha: " + ThisAssembly.Git.Sha);
        }

        public static void EnableDeveloperMode()
        {
            try
            {
                string value64 = string.Empty;
                RegistryKey localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, GetCurrentView());
                localKey = localKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock", true);
                if (localKey != null)
                {
                    switch (localKey.GetValue("AllowDevelopmentWithoutDevLicense"))
                    {
                        case 0:
                            System.Diagnostics.Trace.WriteLine("Developer mode disabled, trying to turn on");
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
                string message = "Cant enable developer mode: " + r;
                System.Diagnostics.Trace.WriteLine(message);
                MessageBox.Show(message + r);
                throw new Exception(message, r);
            }

            RegistryView GetCurrentView()
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64) return RegistryView.Registry64;
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86) return RegistryView.Registry32;
                else return RegistryView.Default;
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
                    canRun = true;
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
            Trace.WriteLine(e.Exception.ToString());
        }
        public static void StartLogging()
        {
            Trace.Listeners.Add(new NLogTraceListener());
            Trace.AutoFlush = true;
        }
    }
}
