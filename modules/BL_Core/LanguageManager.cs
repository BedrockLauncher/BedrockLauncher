using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace BL_Core
{
    public static class LanguageManager
    {
        public static void LanguageChange(string language)
        {
            var possible = Application.Current.Resources.MergedDictionaries.Where(x => x is LanguageDictionary).FirstOrDefault();
            Application.Current.Resources.MergedDictionaries.Remove(possible);
            LanguageDictionary dict = new LanguageDictionary(language);
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
                case "default":
                    LanguageChange("en-US");
                    break;
                default:
                    LanguageChange("en-US");
                    break;

            }
        }
    }

    public class LanguageDictionary : ResourceDictionary
    {


        public const string DefaultLang = "en-US";

        public LanguageDictionary(string language) : base()
        {
            Init(language);
        }

        public LanguageDictionary() : base()
        {
            Init();
        }

        private void Init(string language = null)
        {
            if (language == null) language = DefaultLang;
            LoadLang(language);
        }


        private void LoadLang(string language)
        {
            try
            {
                this.Clear();
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"BL_Core.Resources.lang.{language}.lang";
                List<string> localizations = new List<string>();

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            localizations.Add(line);
                        }
                    }
                }

                foreach (var item in localizations)
                {
                    try
                    {
                        if (item.StartsWith("text.") && item.Contains("="))
                        {
                            string keypair = item.Remove(0, 5);
                            int index = keypair.IndexOf("=");
                            string key = keypair.Substring(0, index);
                            index += 1;
                            string value = keypair.Substring(index, keypair.Length - index);
                            value = value.Replace("\\n", Environment.NewLine);
                            this.Add(key, value);
                        }
                    }
                    catch
                    {
                        Debug.WriteLine($"Unable to add \"{item}\" to localization");
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to load \"{language}\" localization!\n{ex}");
            }
        }
    }
}
