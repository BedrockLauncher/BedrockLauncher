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
using BedrockLauncher.Handlers;
using BedrockLauncher.Components.CefSharp;
using BedrockLauncher.Pages.Common;
using ExtensionsDotNET;

namespace BedrockLauncher
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            RuntimeHandler.StartLogging();
            RuntimeHandler.ValidateOSArchitecture();
            Debug.WriteLine("Application Starting...");
            
            var application = new App();
            application.DispatcherUnhandledException += RuntimeHandler.OnDispatcherUnhandledException;
            application.Startup += OnApplicationInitalizing;
            application.InitializeComponent();
            application.Run();
        }
        public static void OnApplicationInitalizing(object sender, StartupEventArgs e)
        {
            Debug.WriteLine("Application Initalization Started!");
            StartupArgsHandler.SetStartupArgs(e.Args);
            CefSharpLoader.Init();
            RuntimeHandler.EnableDeveloperMode();
            Debug.WriteLine("Application Initalization Finished!");
        }
        public static async Task OnApplicationLoaded()
        {
            await MainViewModel.Default.ShowWaitingDialog(async () =>
            {
                Debug.WriteLine("Preparing Application...");
                await RuntimeHandler.InitalizeBugRockOfTheWeek();
                LanguageManager.Init();
                MainViewModel.Default.LoadConfig();
                await MainViewModel.Default.LoadVersions(true);
                await MainViewModel.Updater.CheckForUpdatesAsync(true);
                Debug.WriteLine("Preparing Application: DONE");
            });
        }

        public static async Task OnApplicationRefresh()
        {

            await MainViewModel.Default.ShowWaitingDialog(async () =>
            {
                Debug.WriteLine("Refreshing Application...");
                MainViewModel.Default.LoadConfig();
                await MainViewModel.Default.LoadVersions();
                Debug.WriteLine("Refreshing Application: DONE");
            });
        }
    }
}
