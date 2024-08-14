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
using BedrockLauncher.Localization.Language;
using BedrockLauncher.Handlers;
using JemExtensions;
using NLog;

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
            
            var application = new App();
            application.Startup += OnApplicationInitalizing;
            application.InitializeComponent();
            application.Run();
        }
        public static void OnApplicationInitalizing(object sender, StartupEventArgs e)
        {
            Trace.WriteLine("Application Initalization Started!");
            StartupArgsHandler.SetStartupArgs(e.Args);
            StartupArgsHandler.RunPreStartupArgs();
            RuntimeHandler.EnableDeveloperMode();
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
    }
}
