using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;

namespace BedrockLauncher.Classes.SkinPack
{
    public class MCSkinPack
    {
        public MCSkinPack()
        {

        }

        public MCSkinPackMainfest Metadata { get; private set; }

        public MCSkinPackContent Content { get; private set; }

        public MCSkinPackLang Texts { get; private set; }

        public string Directory { get; private set; }

        public bool isDev { get; private set; }

        public string IconPath
        {
            get
            {
                return Path.Combine(Directory, "pack_icon.png");
            }
        }

        public string DisplayName
        {
            get
            {
                return Metadata?.header?.name ?? "pack.name" + (isDev ? " (DEV)" : "");
            }
        }

        public string VersionName
        {
            get
            {
                var version = Metadata?.header?.version ?? new int[] { 0, 0, 1 };
                return string.Join(".", version);
            }
        }

        public MCSkinPack(string _Directory, MCSkinPackMainfest _Metadata, bool _isDev = false) : base()
        {
            Directory = _Directory;
            Metadata = _Metadata;
            isDev = _isDev;
            Content = ValidateSkins(_Directory);
            Texts = ValidateTexts(_Directory);
        }

        public string GetLocalizedSkinName(string localization_name, string Lang = null)
        {
            string keyName = string.Format("skin.{0}.{1}", Content.localization_name, localization_name);
            var data = GetData();
            if (data == null) return localization_name;
            if (!data.Global.ContainsKey(keyName)) return localization_name;
            return data.Global[keyName];


            IniParser.Model.IniData GetData()
            {
                if (Lang == null)
                {
                    if (Texts.Values != null && Texts.Values.Count != 0) return Texts.Values.First().Value;
                    else return null;
                }

                return (Texts.Values.ContainsKey(Lang) ? Texts.Values[Lang] : null);
            }
        }

        public static MCSkinPack ValidatePack(string Directory)
        {
            try 
            {
                string json = File.ReadAllText(Path.Combine(Directory, "manifest.json"));
                MCSkinPackMainfest mainfest = JsonConvert.DeserializeObject<MCSkinPackMainfest>(json);
                return new MCSkinPack(Directory, mainfest);
            }
            catch
            {
                return null;
            }
        }

        public static MCSkinPackLang ValidateTexts(string Directory)
        {
            try
            {
                string TextDir = Path.Combine(Directory, "texts");
                string json = File.ReadAllText(Path.Combine(TextDir, "languages.json"));
                string[] lang_meta = JsonConvert.DeserializeObject<string[]>(json);
                return MCSkinPackLang.LoadLang(TextDir, lang_meta);
            }
            catch
            {
                return new MCSkinPackLang();
            }
        }

        public static MCSkinPackContent ValidateSkins(string Directory)
        {
            try
            {
                string json = File.ReadAllText(Path.Combine(Directory, "skins.json"));
                MCSkinPackContent skins_meta = JsonConvert.DeserializeObject<MCSkinPackContent>(json);
                if (skins_meta == null) return null;
                if (skins_meta.skins == null) return null;
                
                foreach (var skin in skins_meta.skins)
                {
                    skin.skin_directory = Directory;
                }

                return skins_meta;
            }
            catch
            {
                return new MCSkinPackContent();
            }
        }
    }
}
