using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BL_Core.Language
{
    public partial class LanguageDictionary : ResourceDictionary
    {
        public string Langauge { get; set; }

        public LanguageDictionary(LanguageDefinition language)
        {         
            this.Langauge = language.Locale;
            this.Source = new Uri(language.Path, (language.IsExternal ? UriKind.Absolute : UriKind.Relative));
        }

        public LanguageDictionary()
        {
            var language = LanguageDefinition.Default;

            this.Langauge = language.Locale;
            this.Source = new Uri(language.Path, (language.IsExternal ? UriKind.Absolute : UriKind.Relative));
        }
    }
}
