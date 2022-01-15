using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Collections;
using Path = System.IO.Path;
using System.Resources;

namespace BedrockLauncher.Core.Language
{
    public static class LanguageManager
    {

        private static List<LanguageDefinition> AvaliableInternalLanguages 
        {
            get
            {
                return new List<LanguageDefinition>
                {
                    new LanguageDefinition("en-US"),
                    new LanguageDefinition("pt-PT"),
                    new LanguageDefinition("ru-RU"),
                    new LanguageDefinition("pt-BR")
                };
            }
        }
        private static void LanguageChange(LanguageDefinition definition)
        {
            var possible = Application.Current.Resources.MergedDictionaries.Where(x => x is LanguageDictionary).FirstOrDefault();
            Application.Current.Resources.MergedDictionaries.Remove(possible);

            LanguageDictionary dict = new LanguageDictionary(definition);
            LanguageDictionary default_dic = new LanguageDictionary(AvaliableInternalLanguages.FirstOrDefault());
            Application.Current.Resources.MergedDictionaries.Add(default_dic);
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }


        #region Public Methods

        public static string GetAssemblyDirectory()
        {
            string Assembly = string.Empty;

            int i = 0;

            while (String.IsNullOrEmpty(Assembly) && i < 10)
            {
                var ass = System.Reflection.Assembly.GetEntryAssembly();
                Assembly = ass.Location;
                i++;
            }

            return System.IO.Path.GetDirectoryName(Assembly);
        }

        public static List<LanguageDefinition> GetResourceDictonaries()
        {
            string Path = GetAssemblyDirectory();
            string ExternalPath = System.IO.Path.Combine(Path, "data", "lang");
            List<LanguageDefinition> Languages = AvaliableInternalLanguages;


            if (!Directory.Exists(ExternalPath)) return Languages;

            var ExternalLangFolder = new DirectoryInfo(ExternalPath);
            foreach (var File in ExternalLangFolder.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                if (File.Extension != ".xaml") continue;
                if (LanguageDefinition.TryPrase(File, out LanguageDefinition ExternalLang)) Languages.Add(ExternalLang);
            }

            return Languages;
        }

        public static void SetLanguage(string language)
        {
            var AvaliableLanguages = GetResourceDictonaries();

            if (language == "none" || language == "default")
            {
                CultureInfo ci = CultureInfo.InstalledUICulture;
                if (AvaliableLanguages.Exists(x => x.Locale == ci.Name)) SetLocaleLanguage(ci.Name);
                else SetToDefaultLangauge();
            }
            else
            {
                if (AvaliableLanguages.Exists(x => x.Locale == language)) SetLocaleLanguage(language);
                else SetToDefaultLangauge();
            }

            void SetToDefaultLangauge()
            {
                LanguageChange(LanguageDefinition.Default);
                Core.Properties.Settings.Default.Language = "default";
                Core.Properties.Settings.Default.Save();
            }

            void SetLocaleLanguage(string locale)
            {
                var Lang = AvaliableLanguages.Where(x => x.Locale == locale).FirstOrDefault();
                LanguageChange(Lang);
                Core.Properties.Settings.Default.Language = locale;
                Core.Properties.Settings.Default.Save();
            }
        }
        public static void Init()
        {
            string language = Core.Properties.Settings.Default.Language;
            SetLanguage(language);
        }

        #endregion
    }


}
