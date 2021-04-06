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
using BedrockLauncher.Html;
using CefSharp.SchemeHandler;

namespace BedrockLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //base.OnStartup(e);
            if (File.Exists("Log.txt")) { File.Delete("Log.txt"); }
            Debug.Listeners.Add(new TextWriterTraceListener("Log.txt"));
            Debug.AutoFlush = true;

            if (e.Args != null) 
            {
                new ConsoleArgumentParser(e.Args);
            }

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
                                        Debug.WriteLine("Developer mode disabled, trying to turn on");
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
                        Debug.Write("Cant enable developer mode for X64 machine Error: " + r);
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
                                    Debug.WriteLine("Developer mode disabled, trying to turn on");
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
                        Debug.Write("Cant enable developer mode for X86 machine Error: " + r);
                    }
                    break;
            }    
        }

        private void ComboBoxItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // To prevent scrolling when mouseover
            e.Handled = true;
        }


        public App()
        {
            //Add Custom assembly resolver
            AppDomain.CurrentDomain.AssemblyResolve += Resolver;

            //Any CefSharp references have to be in another method with NonInlining
            // attribute so the assembly rolver has time to do it's thing.
            InitializeCefSharp();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InitializeCefSharp()
        {
            var settings = new CefSettings();

            // Set BrowserSubProcessPath based on app bitness at runtime
            settings.BrowserSubprocessPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                   Environment.Is64BitProcess ? "x64" : "x86",
                                                   "CefSharp.BrowserSubprocess.exe");

            settings.CefCommandLineArgs.Add("--disable-web-security");

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

            // Make sure you set performDependencyCheck false
            Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
        }

        // Will attempt to load missing assembly from either x86 or x64 subdir
        // Required by CefSharp to load the unmanaged dependencies when running using AnyCPU
        private static Assembly Resolver(object sender, ResolveEventArgs args)
        {
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
    }
}
