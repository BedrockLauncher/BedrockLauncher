using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace BedrockLauncher.Localization.Language
{
    public class LanguageDefinition
    {
        public string Locale { get; set; }
        public string Name { get; set; }
        public LanguageDefinition(string locale)
        {
            Locale = locale;
            Name = locale;
        }
    }

    public static class LanguageManager
    {
        public static void Init()
        {
            try
            {
                var saved = BedrockLauncher.Localization.Properties.Settings.Default.Language;
                if (!string.IsNullOrWhiteSpace(saved))
                    SetLanguage(saved);
            }
            catch { }
        }

        public static void SetLanguage(string locale)
        {
            try
            {
                BedrockLauncher.Localization.Properties.Settings.Default.Language = locale ?? "en-US";
                BedrockLauncher.Localization.Properties.Settings.Default.Save();
            }
            catch { }
        }

        public static void SetLanguage(CultureInfo locale) => SetLanguage(locale?.ToString() ?? "en-US");

        public static object GetResource(string key)
        {
            try
            {
                if (Application.Current != null)
                {
                    var value = Application.Current.TryFindResource(key);
                    if (value != null) return value;
                }
            }
            catch { }

            return key;
        }

        public static List<LanguageDefinition> GetResourceDictonaries() => new List<LanguageDefinition>()
            {
                new LanguageDefinition("en-US") { Name = "English - United States" },
                new LanguageDefinition("it-IT") { Name = "Italiano - Italia" }
            };
    }
}
