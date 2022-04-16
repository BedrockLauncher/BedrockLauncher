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
using System.ComponentModel;
using System.Diagnostics;

namespace BedrockLauncher.Classes.SkinPack
{
    public class MCSkinPack
    {
        public bool InvalidUsage { get; private set; } = false;

        public MCSkinPack()
        {
            InvalidUsage = true;
        }

        public MCSkinPack(string _Directory)
        {
            Directory = _Directory;

            Metadata = new MCSkinPackMainfest();
            Metadata.format_version = 2;

            Metadata.header.description = string.Empty;
            Metadata.header.name = string.Empty;
            Metadata.header.version = new int[] { 1, 0, 0 };
            Metadata.header.uuid = Guid.NewGuid().ToString();

            MCSkinPackMainfest.Module module = new MCSkinPackMainfest.Module();

            module.description = string.Empty;
            module.type = "skin_pack";
            module.version = new int[] { 1, 0, 0 };
            module.uuid = Guid.NewGuid().ToString();

            Metadata.modules.Add(module);

            SaveMainifest();

            Content = new MCSkinPackContent();

            SaveSkins();

            Texts = new MCSkinPackLang();

            SaveTexts();
        }

        public MCSkinPackMainfest Metadata { get; private set; }

        public MCSkinPackContent Content { get; private set; }

        public MCSkinPackLang Texts { get; private set; }

        public string Directory { get; private set; }

        public bool isDev { get; private set; }

        public const string FallbackIcon = @"/BedrockLauncher;component/resources/images/packs/invalid_pack_icon.png";

        public Uri CurrentIcon
        {
            get
            {
                string icon_path = Path.Combine(Directory, "pack_icon.png");
                if (File.Exists(icon_path)) return new Uri(icon_path, UriKind.Absolute);
                else return new Uri(FallbackIcon, UriKind.Relative);
            }
        }

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
                string rawName = Metadata?.header?.name ?? "pack.name";
                GetLocalizedString(rawName, rawName);
                return rawName + (isDev ? " (DEV)" : "");
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

        #region Loading

        public MCSkinPack(string _Directory, MCSkinPackMainfest _Metadata, bool _isDev = false) : base()
        {
            Directory = _Directory;
            Metadata = _Metadata;
            isDev = _isDev;
            Content = ValidateSkins(_Directory);
            Texts = ValidateTexts(_Directory);
        }

        public string GetLocalizedString(string localization_name, string keyName, string Lang = null)
        {
            string DefaultLang = BedrockLauncher.Localization.Language.LanguageDefinition.Default.Locale.Replace("-", "_");
            if (Lang == null) Lang = BedrockLauncher.Localization.Properties.Settings.Default.Language.Replace("-", "_");

            var data = GetData();
            if (data == null) return GetAvaliable();
            if (!data.Global.Contains(keyName)) return GetAvaliable();
            return data.Global[keyName];



            string GetAvaliable()
            {


                if (Texts.Values.Keys.Any())
                {
                    string Avaliable_Lang;

                    if (Texts.Values.ContainsKey(DefaultLang)) Avaliable_Lang = Texts.Values.Keys.FirstOrDefault(x => x == DefaultLang);
                    else Avaliable_Lang = Texts.Values.Keys.FirstOrDefault();

                    if (Texts.Values[Avaliable_Lang].Global.Contains(keyName)) return Texts.Values[Avaliable_Lang].Global[keyName];
                }
                return localization_name;
            }


            IniParser.IniData GetData()
            {
                if (Lang == null)
                {
                    if (Texts.Values != null && Texts.Values.Count != 0) return Texts.Values.First().Value;
                    else return null;
                }

                return (Texts.Values.ContainsKey(Lang) ? Texts.Values[Lang] : null);
            }
        }

        public string GetLocalizedSkinName(string localization_name, string Lang = null)
        {
            string keyName = string.Format("skin.{0}.{1}", Content.localization_name, localization_name);
            return GetLocalizedString(localization_name, keyName, Lang);
        }

        public static MCSkinPack ValidatePack(string Directory, bool isDev)
        {
            try 
            {
                string json = File.ReadAllText(Path.Combine(Directory, "manifest.json"));
                MCSkinPackMainfest mainfest = JsonConvert.DeserializeObject<MCSkinPackMainfest>(json);
                return new MCSkinPack(Directory, mainfest, isDev);
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
                return MCSkinPackLang.InitLang(TextDir);
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

        #endregion

        #region Updating

        public void AddSkin(MCSkin skin)
        {
            Content.skins.Add(skin);
            SaveSkins();
        }

        public void EditSkin(int index, MCSkin skin)
        {
            Content.skins[index] = skin;
            SaveSkins();
        }

        public void RemoveSkin(int index)
        {
            Content.skins.RemoveAt(index);
            SaveSkins();
        }

        #endregion

        #region Saving

        public void Save()
        {
            SaveSkins();
            SaveMainifest();
            SaveTexts();
        }


        public void SaveDirectory()
        {
            string text_folder = Path.Combine(Directory, "texts");
            if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);
            if (!System.IO.Directory.Exists(text_folder)) System.IO.Directory.CreateDirectory(text_folder);

        }
        public void SaveSkins()
        {
            SaveDirectory();
            string json = JsonConvert.SerializeObject(Content, Formatting.Indented);
            File.WriteAllText(Path.Combine(Directory, "skins.json"), json);
        }
        public void SaveMainifest()
        {
            SaveDirectory();
            string json = JsonConvert.SerializeObject(Metadata, Formatting.Indented);
            File.WriteAllText(Path.Combine(Directory, "manifest.json"), json);
        }
        public void SaveTexts()
        {
            SaveDirectory();
            Texts.SaveLang();
        }

        #endregion

        public void OpenDirectory()
        {
            if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);
            Process.Start("explorer.exe", Directory);
        }
    }
}
