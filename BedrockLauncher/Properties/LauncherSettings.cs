using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using BedrockLauncher.Extensions;
using System.ComponentModel;
using BedrockLauncher.Components;
using BedrockLauncher.ViewModels;
using PostSharp.Patterns.Model;
using BedrockLauncher.UI.Components;

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
                if (File.Exists(MainViewModel.Default.FilePaths.GetSettingsFilePath()))
                {
                    json = File.ReadAllText(MainViewModel.Default.FilePaths.GetSettingsFilePath());
                    try { Default = JsonConvert.DeserializeObject<LauncherSettings>(json, JsonSerializerSettings); }
                    catch { Default = new LauncherSettings(); }
                }
                else Default = new LauncherSettings();
            }

            Default.Init();
        }

        public void Init()
        {
            Navigator.AnimatePageTransitions = _AnimatePageTransitions;
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(MainViewModel.Default.FilePaths.GetSettingsFilePath(), json);
        }

        #region Launcher Animation Settings

        private bool _AnimatePageTransitions = false;
        public bool AnimatePageTransitions
        {
            get
            {
                return _AnimatePageTransitions;
            }
            set
            {
                _AnimatePageTransitions = value;
                Navigator.AnimatePageTransitions = value;
            }
        }

        public bool AnimatePlayButton { get; set; } = false;

        #endregion

        #region Launcher Settings






        public bool ShowAdvancedInstallDetails { get; set; } = false;

        public string CurrentTheme { get; set; } = "LatestUpdate";
        public bool KeepLauncherOpen { get; set; } = false;
        public bool UseBetaBuilds { get; set; } = false;



        #endregion

        #region Advanced Settings


        public bool PortableMode { get; set; } = false;
        public string FixedDirectory { get; set; } = "";

        #endregion

        #region Status Storage

        private bool _ShowBetas = true;
        private bool _ShowReleases = true;

        public bool IsFirstLaunch { get; set; } = true;
        public bool GetIsFirstLaunch(int LoadedConfigCount)
        {
            return CurrentProfile == "" || IsFirstLaunch || LoadedConfigCount == 0;
        }

        public string CurrentInstallation { get; set; } = string.Empty;

        public string CurrentProfile { get; set; } = "";
        public bool ShowReleases
        {
            get { return _ShowReleases; }
            set 
            {
                if (!(_ShowBetas == false && value == false)) _ShowReleases = value;
            }
        }
        public bool ShowBetas
        {
            get { return _ShowBetas; }
            set 
            {
                if (!(_ShowReleases == false && value == false)) _ShowBetas = value;
            }
        }
        public int CurrentInsiderAccountIndex { get; set; } = 0;

        #endregion

        #region Shortcut Settings

        public bool HideJavaShortcut { get; set; } = false;
        public bool ShowExternalLauncher { get; set; } = false;
        public string ExternalLauncherName { get; set; } = "";
        public string ExternalLauncherPath { get; set; } = "";
        public string ExternalLauncherArguments { get; set; } = "";
        public string ExternalLauncherIconPath { get; set; } = "";
        public bool CloseLauncherOnSwitch { get; set; } = true;
        public bool EnableDungeonsSupport { get; set; } = false;

        #endregion

    }
}
