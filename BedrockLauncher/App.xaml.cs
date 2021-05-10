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
using CefSharp.SchemeHandler;
using BedrockLauncher.Classes;
using System.Windows.Input;

namespace BedrockLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Program.StartLogging(e);
            Program.EnableDeveloperMode();
        }

        public App()
        {
            BL_Core.LanguageManager.Init();
            AppDomain.CurrentDomain.AssemblyResolve += Program.Resolver;
            Program.InitializeCefSharp();
            Program.Init_IsBugRockOfTheWeek();
        }


    }
}
