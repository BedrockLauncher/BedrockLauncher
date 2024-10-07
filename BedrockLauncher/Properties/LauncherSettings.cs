using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using System.ComponentModel;
using BedrockLauncher.ViewModels;
using PostSharp.Patterns.Model;
using BedrockLauncher.Enums;

namespace BedrockLauncher.Properties
{

    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]    //99 Lines
    public class LauncherSettings
    {
        public static LauncherSettings Default { get; private set; } = new LauncherSettings();
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

        static LauncherSettings()
        {
            Load();
        }

        public static void Load()
        {
            string json;

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                Default = new LauncherSettings();
            }
            else
            {
                if (File.Exists(MainDataModel.Default.FilePaths.GetSettingsFilePath()))
                {
                    json = File.ReadAllText(MainDataModel.Default.FilePaths.GetSettingsFilePath());
                    try { Default = JsonConvert.DeserializeObject<LauncherSettings>(json, JsonSerializerSettings); }
                    catch { Default = new LauncherSettings(); }
                }
                else Default = new LauncherSettings();
            }

            Default.Init();
        }

        public void Init()
        {
            MainDataModel.BackwardsCommunicationHost.UpdateAnimatePageTransitions(_AnimatePageTransitions);
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(MainDataModel.Default.FilePaths.GetSettingsFilePath(), json);
        }

        public bool GetIsFirstLaunch(int LoadedConfigCount)
        {
            return CurrentProfileUUID == "" || IsFirstLaunch || LoadedConfigCount == 0;
        }

        private bool _AnimatePageTransitions = false;
        private bool _ShowBetas = true;
        private bool _ShowReleases = true;
        private bool _ShowPreviews = true;
        private InstallationSort _InstallationsSortMode = InstallationSort.LatestPlayed;

        public InstallationSort InstallationsSortMode
        {
            get
            {
                return _InstallationsSortMode;
            }
            set
            {
                _InstallationsSortMode = value;
                Save();
            }
        }
        public bool FetchVersionsFromMicrosoftStore { get; set; } = false;
        public bool AnimatePageTransitions
        {
            get
            {
                return _AnimatePageTransitions;
            }
            set
            {
                _AnimatePageTransitions = value;
                MainDataModel.BackwardsCommunicationHost.UpdateAnimatePageTransitions(value);
            }
        }
        public string CurrentTheme { get; set; } = "LatestUpdate";
        public bool KeepLauncherOpen { get; set; } = false;
        public bool KeepAppx { get; set; } = false;
        public bool UseBetaBuilds { get; set; } = false;
        public bool PortableMode { get; set; } = false;
        public string FixedDirectory { get; set; } = "";
        public bool IsFirstLaunch { get; set; } = true;
        public string CurrentInstallationUUID { get; set; } = string.Empty;
        public string CurrentProfileUUID { get; set; } = "";
        public bool ShowReleases
        {
            get { return _ShowReleases; }
            set { _ShowReleases = value; }
        }
        public bool ShowBetas
        {
            get { return _ShowBetas; }
            set { _ShowBetas = value; }
        }
        public bool ShowPreviews
        {
            get { return _ShowPreviews; }
            set { _ShowPreviews = value; }
        }
        public int CurrentInsiderAccountIndex { get; set; } = 0;

    }
}
