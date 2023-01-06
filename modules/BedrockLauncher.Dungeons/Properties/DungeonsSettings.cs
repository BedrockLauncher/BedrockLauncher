using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using JemExtensions.WPF;

namespace BedrockLauncher.Dungeons.Properties
{
    public class DungeonSettings : NotifyPropertyChangedBase
    {
        public static string GetSettingsFilePath()
        {
            string ExecutableLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string ExecutableDirectory = Path.GetDirectoryName(ExecutableLocation);
            string path = Path.Combine(ExecutableDirectory, "data");
            Directory.CreateDirectory(path);
            return Path.Combine(path, "dungeons_settings.json");
        }


        public static DungeonSettings Default { get; private set; } = new DungeonSettings();
        private static JsonSerializerSettings JsonSerializerSettings
        {
            get
            {
                var settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.MissingMemberHandling = MissingMemberHandling.Ignore;
                return settings;
            }
        }

        static DungeonSettings()
        {
            Load();
        }

        public static void Load()
        {
            string json;

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                Default = new DungeonSettings();
            }
            else
            {
                if (File.Exists(GetSettingsFilePath()))
                {
                    json = File.ReadAllText(GetSettingsFilePath());
                    try { Default = JsonConvert.DeserializeObject<DungeonSettings>(json, JsonSerializerSettings); }
                    catch { Default = new DungeonSettings(); }
                }
                else Default = new DungeonSettings();
            }


        }
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(GetSettingsFilePath(), json);
        }

        #region Shortcut Settings

        private Enums.GameVariant _GameVariant = Enums.GameVariant.Launcher;
        private string _InstallLocation = string.Empty;
        private string _ModsLocation = string.Empty;

        public Enums.GameVariant GameVariant
        {
            get { return _GameVariant; }
            set { _GameVariant = value; OnPropertyChanged(nameof(GameVariant)); }
        }

        public string ModsLocation
        {
            get { return _ModsLocation; }
            set { _ModsLocation = value; OnPropertyChanged(nameof(ModsLocation)); }
        }

        public string InstallLocation
        {
            get { return _InstallLocation; }
            set { _InstallLocation = value; OnPropertyChanged(nameof(InstallLocation)); }
        }

        #endregion

    }
}
