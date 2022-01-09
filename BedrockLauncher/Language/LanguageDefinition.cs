using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BedrockLauncher.Language
{
    public class LanguageDefinition
    {
        #region Definitions
        public string Locale { get; set; }
        public string Path { get; set; }
        public bool IsExternal { get; set; } = false;
        public string Name { get; set; }
        public override string ToString()
        {
            return Name;
        }

        #endregion

        #region Functions
        public static bool TryPrase(System.IO.FileInfo fileInfo, out LanguageDefinition definition) 
        {
            try
            {
                ResourceDictionary resourceDictionary = new ResourceDictionary();
                resourceDictionary.Source = new Uri(fileInfo.FullName, UriKind.Absolute);
                if (resourceDictionary.Count == 0 || !fileInfo.Name.StartsWith("lang."))
                {
                    definition = null;
                    return false;
                }
                else
                {
                    string locale = fileInfo.Name.Remove(0, 5).Replace(fileInfo.Extension, "");
                    definition = new LanguageDefinition(locale, fileInfo.FullName, true);
                    return true;
                }
            }
            catch
            {
                definition = null;
                return false;
            }

        }
        private static bool TryGetName(string path, bool isExternal, out string name)
        {
            try
            {
                ResourceDictionary resourceDictionary = new ResourceDictionary();
                resourceDictionary.Source = new Uri(path, isExternal ? UriKind.Absolute : UriKind.Relative);
                name = resourceDictionary["LanguageString"].ToString();
                return true;
            }
            catch
            {
                name = null;
                return false;
            }
        }
        #endregion

        #region Declerations

        public static readonly LanguageDefinition Default = new LanguageDefinition();
        public LanguageDefinition(string _locale, string _path, bool _isExternal)
        {
            this.Locale = _locale;
            this.Path = _path;
            this.IsExternal = _isExternal;
            if (TryGetName(this.Path, this.IsExternal, out string _name)) this.Name = _name;
        }
        public LanguageDefinition(string _locale)
        {
            this.Locale = _locale;
            this.Path = $"/BedrockLauncher;component/Resources/lang/lang.{_locale}.xaml";
            this.IsExternal = false;
            if (TryGetName(this.Path, this.IsExternal, out string _name)) this.Name = _name;
        }
        public LanguageDefinition()
        {
            this.Locale = "en-US";
            this.Path = $"/BedrockLauncher;component/Resources/lang/lang.en-US.xaml";
            this.IsExternal = false;
            if(TryGetName(this.Path, this.IsExternal, out string _name)) this.Name = _name;
        }

        #endregion
    }
}
