using BedrockLauncher.Developer;
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
            Trace.WriteLine("Version: " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version);
            Trace.WriteLine("Git Repo: " + ThisAssembly.Git.RepositoryUrl);
            Trace.WriteLine("Git Branch: " + ThisAssembly.Git.Branch);
            Trace.WriteLine("Git Commit: " + ThisAssembly.Git.Commit);
            Trace.WriteLine("Git Sha: " + ThisAssembly.Git.Sha);
        }
        public static bool EnableDeveloperMode()
        {
            // This method now checks Developer Mode status instead of automatically enabling it
            // Users should enable Developer Mode manually through Windows Settings if they want unprivileged symbolic links
            System.Diagnostics.Trace.WriteLine("Checking Developer Mode..");
            if (IsDeveloperModeEnabled())
            {
                System.Diagnostics.Trace.WriteLine("Developer mode is enabled - Good to go.");
                return true;
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("Developer mode is disabled - Please enable it in windows settings.");
                MessageBox.Show("You need to enable Developer mode in windows settings to use the launcher.", "Developer Mode Disabled");
                return false;
            }
        }

        /// <summary>
        /// Checks if Windows Developer Mode is enabled
        /// </summary>
        public static bool IsDeveloperModeEnabled()
        {
            try
            {
                RegistryKey localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, GetCurrentView());
                localKey = localKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock", false);
                if (localKey != null)
                {
                    var value = localKey.GetValue("AllowDevelopmentWithoutDevLicense");
                    return value is int intValue && intValue == 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Could not check Developer Mode status: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Shows a user-friendly message about enabling Developer Mode if it's not enabled
        /// </summary>
        public static void ShowDeveloperModeGuidance()
        {
            if (!IsDeveloperModeEnabled())
            {
                string message = "Please enable Developer Mode in Windows Settings:\n\n" +
                               "1. Open Windows Settings\n" +
                               "2. Go to Privacy & security → For developers\n" +
                               "3. Turn on Developer Mode\n\n" +
                               "This allows the launcher to run without requiring administrator privileges each time.";
                
                MessageBox.Show(message, "Enable Windows Developer Mode", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private static RegistryView GetCurrentView()
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64) return RegistryView.Registry64;
            else if (RuntimeInformation.ProcessArchitecture == Architecture.X86) return RegistryView.Registry32;
            else return RegistryView.Default;
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

        public static NLogTraceListener InternalTraceListener { get; set; } = new NLogTraceListener();

        public static void StartLogging()
        {
            Trace.Listeners.Add(InternalTraceListener);
            Trace.AutoFlush = true;
        }
    }
}
