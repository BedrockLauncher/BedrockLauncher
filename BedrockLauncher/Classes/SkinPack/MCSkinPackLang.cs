using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IniParser;
using IniParser.Model;
using Newtonsoft.Json;

namespace BedrockLauncher.Classes.SkinPack
{
    public class MCSkinPackLang
    {
        public string[] LangFiles { get; private set; } = new string[0];
        public string Directory { get; private set; } = string.Empty;
        public Dictionary<string, IniData> Values { get; private set; } = new Dictionary<string, IniData>();

        public void AddLang(string lang_name)
        {
            var list = LangFiles.ToList();
            if (!list.Contains(lang_name)) list.Add(lang_name);
            LangFiles = list.ToArray();
            SaveLang();
            LoadLang();
        }

        public void RemoveLang(string lang_name)
        {
            var list = LangFiles.ToList();
            list.RemoveAll(x => x == lang_name);
            LangFiles = list.ToArray();
            SaveLang();
            LoadLang();
        }

        public void SaveLang()
        {
            try
            {
                string json = JsonConvert.SerializeObject(LangFiles, Formatting.Indented);
                File.WriteAllText(Path.Combine(Directory, "languages.json"), json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
            }

        }

        public void LoadLang()
        {
            this.LangFiles = new string[] { };
            this.Values.Clear();

            try
            {
                string filePath = Path.Combine(this.Directory, "languages.json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    string[] fileNames = JsonConvert.DeserializeObject<string[]>(json);
                    this.LangFiles = fileNames;
                }
                else this.LangFiles = new string[] { };

            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                this.LangFiles = new string[] { };
            }

            foreach (var langFile in this.LangFiles)
            {
                try
                {
                    var parser = new IniDataParser();
                    string filePath = Path.Combine(this.Directory, langFile + ".lang");
                    if (File.Exists(filePath))
                    {
                        var iniStrings = File.ReadAllText(filePath);
                        IniData data = parser.Parse(iniStrings);
                        this.Values.Add(langFile, data);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex);
                }

            }
        }

        public static MCSkinPackLang InitLang(string Directory)
        {
            MCSkinPackLang lang = new MCSkinPackLang();
            lang.Directory = Directory;
            lang.LoadLang();
            return lang;
        }
    }
}
