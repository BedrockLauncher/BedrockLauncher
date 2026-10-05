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
using System.Net.Http;
using System.Windows;
using System.Windows.Forms;

namespace BedrockLauncher
{
    public static class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static ConsoleWindow _cli;
        public static ConsoleWindow cli => _cli ??= new ConsoleWindow();
        private const string StoreEngagementHelperArgument = "Microsoft.Service.StoreEngagement_8wekyb3d8bbwe";
        private const string StoreEngagementMinimumVersion = "10.0.19011.0";
        private const string StoreEngagementAppxUri = "https://github.com/RaythCo-Creations/downloads/raw/main/Microsoft.Services.Store.Engagement_10.0.19011.0_x64__8wekyb3d8bbwe.Appx";


        [STAThread]
        public static void Main()
        {
            RuntimeHandler.StartLogging();
            RuntimeHandler.LogStartupInformation();
            RuntimeHandler.ValidateOSArchitecture();
            Trace.WriteLine("Application Starting...");
            if (/*CheckForWindowsVersion() &&*/ CheckForVCRuntime() && RuntimeHandler.EnableDeveloperMode())
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
                Trace.WriteLine("VC++ Runtime " + minimumVersionS + " or highet not found. Downloading and installing it now!");
                System.Windows.Forms.MessageBox.Show("Microsoft Visual C++ Red. is missing... BedrockLauncher will now download it for u :)", "BedrockLauncher");
                try
                {
                    string installerPatch = Path.Combine(Path.GetTempPath(), "vc_redist.x64.exe");
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromMinutes(5);
                        byte[] installerBytes = client.GetByteArrayAsync("https://aka.ms/vs/17/release/vc_redist.x64.exe").Result;
                        File.WriteAllBytes(installerPatch, installerBytes);
                    }

                    ProcessStartInfo startInfo = new ProcessStartInfo(installerPatch)
                    {
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                    using (Process process = Process.Start(startInfo))
                    {
                        process.WaitForExit();
                    }

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
                catch (Exception ex)
                {
                    Trace.WriteLine("Error while installing VC++ Runtime: " + ex.Message);
                    System.Windows.Forms.MessageBox.Show("Error while installing VC++ Runtime: " + ex.Message, "BedrockLauncher");
                }
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