using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IniParser;
using IniParser.Model;

namespace BedrockLauncher.Classes.SkinPack
{
    public class MCSkinPackLang
    {
        public string[] LangFiles { get; private set; }
        public string Directory { get; private set; }
        public Dictionary<string, IniData> Values { get; private set; } = new Dictionary<string, IniData>();

        public static MCSkinPackLang LoadLang(string Directory, string[] fileNames)
        {
            MCSkinPackLang lang = new MCSkinPackLang();
            lang.LangFiles = fileNames;
            lang.Directory = Directory;

            foreach (var langFile in lang.LangFiles)
            {
                try
                {
                    var parser = new FileIniDataParser();
                    IniData data = parser.ReadFile(Path.Combine(Directory, langFile + ".lang"));
                    lang.Values.Add(langFile, data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

            }

            return lang;
        }
    }
}
