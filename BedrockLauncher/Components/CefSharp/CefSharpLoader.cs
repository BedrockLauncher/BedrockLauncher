using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Linq;
using System;
using Microsoft.Win32;
using System.IO;
using BedrockLauncher.Extensions;
using System.Reflection;
#if ENABLE_CEFSHARP
using CefSharp;
using CefSharp.Wpf;
#endif
using System.Runtime.CompilerServices;
using BedrockLauncher.Downloaders;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BedrockLauncher.Components.CefSharp
{
    public static class CefSharpLoader
    {
        // Any CefSharp references have to be in another method with NonInlining
        // attribute so the assembly rolver has time to do it's thing.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Init()
        {
#if ENABLE_CEFSHARP
            //AppDomain.CurrentDomain.AssemblyResolve += Resolver;

            var settings = InitSettings();

            RegisterScheme(ref settings, ResourceSchemeHandlerFactory.SchemeName, new ResourceSchemeHandlerFactory());
            RegisterScheme(ref settings, FileSchemeHandlerFactory.SchemeName, new FileSchemeHandlerFactory());
            RegisterScheme(ref settings, SkinViewResourceSchemeHandlerFactory.SchemeName, new SkinViewResourceSchemeHandlerFactory());

            // Make sure you set performDependencyCheck false
            Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
#endif
        }
#if ENABLE_CEFSHARP
        public static void InitBrowser(ref ChromiumWebBrowser browser)
        {
            browser.BrowserSettings = GetBrowserSettings();
        }


        private static void RegisterScheme(ref CefSettings settings, string SchemeName, ISchemeHandlerFactory SchemeHandlerFactory)
        {
            settings.RegisterScheme(new CefCustomScheme() { SchemeName = SchemeName, SchemeHandlerFactory = SchemeHandlerFactory });
        }
        private static CefSettings InitSettings()
        {
            CefSettings settings = new CefSettings();

            /// Disabled Settings
            //settings.RemoteDebuggingPort = 8088;
            //settings.UserAgent = "CefSharp Browser" + Cef.CefSharpVersion;
            //settings.MultiThreadedMessageLoop = true;
            //settings.ExternalMessagePump = false;
            settings.WindowlessRenderingEnabled = true;

            /// Enabled Settings
            settings.LogSeverity = LogSeverity.Disable;
            settings.CachePath = Path.Combine(GetCefPath(), "Cache");

            /// Set BrowserSubProcessPath based on app bitness at runtime
            //settings.BrowserSubprocessPath = Path.Combine(GetCefPath(), "CefSharp.BrowserSubprocess.exe");

        #region Command Line Args

            //Chromium Command Line args
            //http://peter.sh/experiments/chromium-command-line-switches/
            //NOTE: Not all relevant in relation to `CefSharp`, use for reference purposes only.

        #region External File Access

            /// External File Access
            settings.CefCommandLineArgs.Add("allow-universal-access-from-files");
            settings.CefCommandLineArgs.Add("allow-file-access-from-files");

        #endregion

        #region Renderer

            //settings.CefCommandLineArgs.Add("renderer-process-limit", "1");
            //settings.CefCommandLineArgs.Add("renderer-startup-dialog", "1");

        #endregion

        #region WebRTC

            ///Enable WebRTC
            //settings.CefCommandLineArgs.Add("enable-media-stream", "1"); 

        #endregion

        #region Proxy Server


            ///Don't use a proxy server, always make direct connections. Overrides any other proxy server flags that are passed.
            settings.CefCommandLineArgs.Add("no-proxy-server", "1"); 

        #endregion

        #region Debug Plugin Loading 

            ///Dumps extra logging about plugin loading to the log file.
            //settings.CefCommandLineArgs.Add("debug-plugin-loading", "1"); 

        #endregion

        #region Plugins Discovery

            ///Disable discovering third-party plugins. Effectively loading only ones shipped with the browser plus third-party ones as specified by --extra-plugin-dir and --load-plugin switches
            settings.CefCommandLineArgs.Add("disable-plugins-discovery", "1"); 

        #endregion

        #region System Flash

            ///Automatically discovered and load a system-wide installation of Pepper Flash.
            //settings.CefCommandLineArgs.Add("enable-system-flash", "1"); 

        #endregion

        #region Insecure Content

            ///By default, an https page cannot run JavaScript, CSS or plugins from http URLs. This provides an override to get the old insecure behavior. Only available in 47 and above.
            //settings.CefCommandLineArgs.Add("allow-running-insecure-content", "1"); 

        #endregion

        #region Logging

            ///Enable Logging for the Renderer process (will open with a cmd prompt and output debug messages - use in conjunction with setting LogSeverity = LogSeverity.Verbose;)
            //settings.CefCommandLineArgs.Add("enable-logging", "1"); 

        #endregion

        #region Extensions

            ///Extension support can be disabled
            settings.CefCommandLineArgs.Add("disable-extensions", "1"); 

        #endregion

        #region PDF Extension

            ///The PDF extension specifically can be disabled
            settings.CefCommandLineArgs.Add("disable-pdf-extension", "1");

        #endregion

        #region Flash

            ///Load the pepper flash player that comes with Google Chrome - may be possible to load these values from the registry and query the dll for it's version info (Step 2 not strictly required it seems)

            ///Load a specific pepper flash version (Step 1 of 2)
            //settings.CefCommandLineArgs.Add("ppapi-flash-path", @"C:\Program Files (x86)\Google\Chrome\Application\47.0.2526.106\PepperFlash\pepflashplayer.dll");

            ///Load a specific pepper flash version (Step 2 of 2)
            //settings.CefCommandLineArgs.Add("ppapi-flash-version", "20.0.0.228"); 

        #endregion

        #region GPU / OSR

            ///NOTE: For OSR best performance you should run with GPU disabled:
            /// `--disable-gpu --disable-gpu-compositing --enable-begin-frame-scheduling`
            /// (you'll loose WebGL support but gain increased FPS and reduced CPU usage).
            /// http://magpcss.org/ceforum/viewtopic.php?f=6&t=13271#p27075
            ///https://bitbucket.org/chromiumembedded/cef/commits/e3c1d8632eb43c1c2793d71639f3f5695696a5e8

            ///For OSR Best Performance
            //settings.SetOffScreenRenderingBestPerformanceArgs();

            ///NOTE: The SetOffScreenRenderingBestPerformanceArgs() function will set the param to 1
            //settings.CefCommandLineArgs.Add("disable-gpu", "1");

            ///NOTE: The SetOffScreenRenderingBestPerformanceArgs() function will set the param to 1
            //settings.CefCommandLineArgs.Add("disable-gpu-compositing", "1");

            ///NOTE: The SetOffScreenRenderingBestPerformanceArgs() function will set the param to 1
            //settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");

            ///Disable VSync
            //settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1"); 

            ///Misc Settings
            //settings.CefCommandLineArgs.Add("enable-3d-apis", "1");
            //settings.CefCommandLineArgs.Add("enable-webgl-draft-extensions", "1");
            //settings.CefCommandLineArgs.Add("enable-gpu", "1");
            //settings.CefCommandLineArgs.Add("enable-webgl", "1");

        #endregion

        #region DirectWrite

            ///Disables the DirectWrite font rendering system on windows.
            ///Possibly useful when experiencing blury fonts.
            //settings.CefCommandLineArgs.Add("disable-direct-write", "1");

        #endregion

        #endregion

            return settings;

        }
        private static string GetCefPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Runtimes", Environment.Is64BitProcess ? "win-x64" : "win-x86", "native");
        }
        private static IBrowserSettings GetBrowserSettings()
        {
            BrowserSettings settings = new BrowserSettings();
            settings.WindowlessFrameRate = 60;
            return settings;
        }
#endif
    }
}
