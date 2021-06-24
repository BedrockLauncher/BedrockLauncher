using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Linq;
using System;
using Microsoft.Win32;
using System.IO;
using BedrockLauncher.Methods;
using System.Reflection;
using CefSharp;
using CefSharp.Wpf;
using System.Runtime.CompilerServices;
using BedrockLauncher.Classes.Html;
using BedrockLauncher.Downloaders;

namespace BedrockLauncher
{
    public static class Program
    {
        #region Actions
        public static void StartLogging()
        {
            if (File.Exists("Log.txt")) { File.Delete("Log.txt"); }
            Debug.Listeners.Add(new TextWriterTraceListener("Log.txt"));
            Debug.AutoFlush = true;
        }

        public static void EnableDeveloperMode()
        {
            //changing registry to enable developer mode
            switch (System.Environment.Is64BitOperatingSystem)
            {
                case true:
                    try
                    {
                        string value64 = string.Empty;
                        RegistryKey localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
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
                    break;
                case false:
                    try
                    {
                        string value32 = string.Empty;
                        RegistryKey localKey32 = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry32);
                        localKey32 = localKey32.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock", true);
                        if (localKey32 != null)
                        {
                            switch (localKey32.GetValue("AllowDevelopmentWithoutDevLicense"))
                            {
                                case 0:
                                    System.Diagnostics.Debug.WriteLine("Developer mode disabled, trying to turn on");
                                    localKey32.SetValue("AllowDevelopmentWithoutDevLicense", 1);
                                    break;
                                case null:
                                    localKey32.SetValue("AllowDevelopmentWithoutDevLicense", 1, RegistryValueKind.DWord);
                                    break;
                            }
                        }
                    }
                    catch (Exception r)
                    {
                        string message = "Cant enable developer mode for X86 machine Error: " + r;
                        System.Diagnostics.Debug.WriteLine(message);
                        MessageBox.Show(message + r);
                        throw r;
                    }
                    break;
            }
        }


        private static bool _IsBugRockOfTheWeek = false;

        public static void Init_IsBugRockOfTheWeek()
        {
            _IsBugRockOfTheWeek = ChangelogDownloader.GetBedrockOfTheWeekStatus();
        }

        public static bool IsBugRockOfTheWeek()
        {
            return _IsBugRockOfTheWeek;
        }

        // Any CefSharp references have to be in another method with NonInlining
        // attribute so the assembly rolver has time to do it's thing.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void InitializeCefSharp()
        {
            var settings = new CefSettings();

            // Set BrowserSubProcessPath based on app bitness at runtime
            settings.BrowserSubprocessPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                   Environment.Is64BitProcess ? "x64" : "x86",
                                                   "CefSharp.BrowserSubprocess.exe");

            settings.LogSeverity = LogSeverity.Disable;

            settings.CefCommandLineArgs.Add("--disable-web-security");
            settings.CefCommandLineArgs.Add("enable-gpu", "1");
            settings.CefCommandLineArgs.Add("enable-webgl", "1");
            settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");
            settings.CefCommandLineArgs.Add("--off-screen-frame-rate", "60");

            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = ResourceSchemeHandlerFactory.SchemeName,
                SchemeHandlerFactory = new ResourceSchemeHandlerFactory()
            });

            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = FileSchemeHandlerFactory.SchemeName,
                SchemeHandlerFactory = new FileSchemeHandlerFactory()
            });

            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = SkinViewResourceSchemeHandlerFactory.SchemeName,
                SchemeHandlerFactory = new SkinViewResourceSchemeHandlerFactory()
            });

            // Make sure you set performDependencyCheck false
            Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
        }

        public static Assembly Resolver(object sender, ResolveEventArgs args)
        {
            // Will attempt to load missing assembly from either x86 or x64 subdir
            // Required by CefSharp to load the unmanaged dependencies when running using AnyCPU
            if (args.Name.StartsWith("CefSharp"))
            {
                string assemblyName = args.Name.Split(new[] { ',' }, 2)[0] + ".dll";
                string archSpecificPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                       Environment.Is64BitProcess ? "x64" : "x86",
                                                       assemblyName);

                return File.Exists(archSpecificPath)
                           ? Assembly.LoadFile(archSpecificPath)
                           : null;
            }


            return null;
        }
        #endregion

        private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Debug.WriteLine(e.Exception.Message);
            MessageBox.Show("Unhandled exception occurred: \n" + e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static string[] Arguments { get; private set; }

        [STAThread]
        public static void Main()
        {
            Program.StartLogging();
            Debug.WriteLine("Application Starting...");
            var application = new App();
            application.DispatcherUnhandledException += OnDispatcherUnhandledException;
            application.Startup += Application_Startup;
            application.InitializeComponent();
            application.Run();
        }

        private static void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.AssemblyResolve += Program.Resolver;
            Program.EnableDeveloperMode();
            Program.InitializeCefSharp();
            Program.Init_IsBugRockOfTheWeek();
            Arguments = e.Args;
            Debug.WriteLine("Application Pre-Initalization Finished!");
        }
    }
}
