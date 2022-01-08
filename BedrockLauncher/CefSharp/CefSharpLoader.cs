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
using BedrockLauncher.Downloaders;
using System.Runtime.InteropServices;

namespace BedrockLauncher.CefSharp
{
    public static class CefSharpLoader
    {
        // Any CefSharp references have to be in another method with NonInlining
        // attribute so the assembly rolver has time to do it's thing.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Init()
        {
            AppDomain.CurrentDomain.AssemblyResolve += Resolver;
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

            settings.CefCommandLineArgs.Add("allow-universal-access-from-files");
            settings.CefCommandLineArgs.Add("allow-file-access-from-files");


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
        
    }
}
