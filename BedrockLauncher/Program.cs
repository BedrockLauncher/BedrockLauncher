using BedrockLauncher.Downloaders;
using BedrockLauncher.Handlers;
using BedrockLauncher.Localization.Language;
using BedrockLauncher.ViewModels;
using JemExtensions;
using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace BedrockLauncher
{
    public static class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static ConsoleWindow cli = new ConsoleWindow();

        [STAThread]
        public static void Main()
        {
            RuntimeHandler.StartLogging();
            RuntimeHandler.LogStartupInformation();
            RuntimeHandler.ValidateOSArchitecture();
            Trace.WriteLine("Application Starting...");
            if (CheckForWindowsVersion() && CheckForVCRuntime() && RuntimeHandler.EnableDeveloperMode())
            {
                var application = new App();
                application.Startup += OnApplicationInitalizing;
                application.InitializeComponent();
                application.Run();
            }
        }
        public static void OnApplicationInitalizing(object sender, StartupEventArgs e)
        {
            Trace.WriteLine("Application Initalization Started!");
            StartupArgsHandler.SetStartupArgs(e.Args);
            StartupArgsHandler.RunPreStartupArgs();
            Trace.WriteLine("Application Initalization Finished!");
        }
        public static async Task OnApplicationLoaded()
        {
            await MainViewModel.Default.ShowWaitingDialog(async () =>
            {
                Trace.WriteLine("Preparing Application...");
                await RuntimeHandler.InitalizeBugRockOfTheWeek();
                LanguageManager.Init();
                MainDataModel.Default.LoadConfig();
                await MainDataModel.Default.LoadVersions(true);
                MainDataModel.Default.ProgressBarState.PlayButtonLanguageChanged = !MainDataModel.Default.ProgressBarState.PlayButtonLanguageChanged;
                if (await MainDataModel.Updater.CheckForUpdatesAsync(true)) MainViewModel.Default.UpdateButton.ShowUpdateButton();
                Trace.WriteLine("Preparing Application: DONE");
            });
        }

        public static async Task OnApplicationRefresh()
        {

            await MainViewModel.Default.ShowWaitingDialog(async () =>
            {
                Trace.WriteLine("Refreshing Application...");
                MainDataModel.Default.LoadConfig();
                await MainDataModel.Default.LoadVersions();
                Trace.WriteLine("Refreshing Application: DONE");
            });
        }

        public static bool CheckForVCRuntime()
        {
            Trace.WriteLine("Checking VC Runtime version");
            Thread.Sleep(500);
            bool result = false;
            string minimumVersionS = "14.14.26405.0";

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\VisualStudio\\14.0\\VC\\Runtimes\\x64"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("Version");
                        if (o != null)
                        {
                            Version currentVersion = new Version((o as String).Replace("v", ""));
                            Version minimumVersion = new Version(minimumVersionS);
                            if (currentVersion.CompareTo(minimumVersion) >= 0) result = true;
                        }

                    }

                }
            }
            catch (Exception) { }

            if (!result)
            {
                Trace.WriteLine("You need VC++ Runtime " + minimumVersionS + " or higher to run this application! Please download it!");
                System.Windows.Forms.MessageBox.Show("You need VC++ Runtime " + minimumVersionS + " or higher to run this application! Please download it!", "Error");
            }
            else
            {
                Trace.WriteLine("VC++ Runtime OK");
            }
                return result;
        }
        public static bool CheckForWindowsVersion()
        {
            Trace.WriteLine("Checking Windows Version");
            Thread.Sleep(500);
            bool result = false;
            string minimumVersionS = "10.0.19041.0";

            try
            {
                Version currentVersion = Environment.OSVersion.Version;
                Version minimumVersion = new Version(minimumVersionS);
                if (currentVersion.CompareTo(minimumVersion) >= 0) result = true;
            }
            catch (Exception) { }

            if (!result)
            {
                Trace.WriteLine("This application only works on Windows version " + minimumVersionS + " or above!");
                System.Windows.Forms.MessageBox.Show("This application only works on Windows version " + minimumVersionS + " or above!", "Error");
            }
            else
            {
                Trace.WriteLine("Windows Version OK");
            }
                return result;
        }
    }
}
