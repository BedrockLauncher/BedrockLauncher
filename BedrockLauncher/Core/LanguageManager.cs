using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms.Design;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Data;
using BedrockLauncher.Interfaces;
using BedrockLauncher.Methods;
using BedrockLauncher.Classes;

namespace BedrockLauncher.Core
{
    public static class LanguageManager
    {
        public static void LanguageChange(string language)
        {
            ResourceDictionary dict = new ResourceDictionary
            {
                Source = new Uri($"..\\Resources\\text\\lang.{language}.xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        public static void Init()
        {
            // Language setting on startup
            switch (Properties.Settings.Default.Language)
            {
                case "none":
                    CultureInfo ci = CultureInfo.InstalledUICulture; // get system locale
                    switch (ci.Name)
                    {
                        default:
                            Debug.WriteLine("default language");
                            Properties.Settings.Default.Language = "default";
                            Properties.Settings.Default.Save();
                            break;
                        case "ru-RU":
                            LanguageChange(ci.Name);
                            Properties.Settings.Default.Language = "ru-RU";
                            Properties.Settings.Default.Save();
                            break;
                        case "en-US":
                            LanguageChange(ci.Name);
                            Properties.Settings.Default.Language = "en-US";
                            Properties.Settings.Default.Save();
                            break;
                    }
                    break;
                case "ru-RU":
                    LanguageChange("ru-RU");
                    break;

                case "en-US":
                    LanguageChange("en-US");
                    break;
                default:
                    break;

            }
        }
    }
}
