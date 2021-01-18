using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Linq;
using System;
using Microsoft.Win32;

namespace BedrockLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Debug.Listeners.Add(new TextWriterTraceListener("Log.txt"));
            Debug.AutoFlush = true;

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

    }
}
