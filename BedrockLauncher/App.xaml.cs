using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Linq;
using System;
using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BedrockLauncher.Classes;
using System.Windows.Input;
using BedrockLauncher.Handlers;

namespace BedrockLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Version
        {
            get
            {
                return System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
            }
        }
        public App() : base()
        {
            this.DispatcherUnhandledException += RuntimeHandler.OnDispatcherUnhandledException;
        }
    }
}
