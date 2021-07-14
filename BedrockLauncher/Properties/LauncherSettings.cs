using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using BedrockLauncher.Methods;
using System.ComponentModel;
using BedrockLauncher.Core.Components;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Properties
{
    public class LauncherSettings : NotifyPropertyChangedBase
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
                if (File.Exists(LauncherModel.Default.FilepathManager.GetSettingsFilePath()))
                {
                    json = File.ReadAllText(LauncherModel.Default.FilepathManager.GetSettingsFilePath());
                    try { Default = JsonConvert.DeserializeObject<LauncherSettings>(json, JsonSerializerSettings); }
                    catch { Default = new LauncherSettings(); }
                }
                else Default = new LauncherSettings();
            }


        }
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(LauncherModel.Default.FilepathManager.GetSettingsFilePath(), json);
        }

        #region Launcher Settings

        private string _CurrentTheme = "LatestUpdate";
        private bool _ShowAdvancedInstallDetails = false;
        private bool _UseBetaBuilds = false;
        private bool _KeepLauncherOpen = false;
        private bool _AnimatePlayButton = false;
        private bool _AnimatePageTransitions = false;


        public bool ShowAdvancedInstallDetails
        {
            get { return _ShowAdvancedInstallDetails; }
            set { _ShowAdvancedInstallDetails = value; OnPropertyChanged(nameof(ShowAdvancedInstallDetails)); }
        }

        public string CurrentTheme
        {
            get { return _CurrentTheme; }
            set { _CurrentTheme = value; OnPropertyChanged(nameof(CurrentTheme)); }
        }
        public bool KeepLauncherOpen
        {
            get { return _KeepLauncherOpen; }
            set { _KeepLauncherOpen = value; OnPropertyChanged(nameof(KeepLauncherOpen)); }
        }
        public bool UseBetaBuilds
        {
            get { return _UseBetaBuilds; }
            set { _UseBetaBuilds = value; OnPropertyChanged(nameof(UseBetaBuilds)); }
        }

        public bool AnimatePlayButton
        {
            get { return _AnimatePlayButton; }
            set { _AnimatePlayButton = value; OnPropertyChanged(nameof(AnimatePlayButton)); }
        }

        public bool AnimatePageTransitions
        {
            get { return _AnimatePageTransitions; }
            set { _AnimatePageTransitions = value; OnPropertyChanged(nameof(AnimatePageTransitions)); }
        }

        #endregion

        #region Advanced Settings

        private bool _PortableMode = false;
        private string _FixedDirectory = "";

        public bool PortableMode
        {
            get { return _PortableMode; }
            set { _PortableMode = value; OnPropertyChanged(nameof(PortableMode)); }
        }
        public string FixedDirectory
        {
            get { return _FixedDirectory; }
            set { _FixedDirectory = value; OnPropertyChanged(nameof(FixedDirectory)); }
        }

        #endregion

        #region Status Storage

        private bool _ShowBetas = true;
        private string _CurrentInstallation = string.Empty;
        private bool _IsFirstLaunch = true;
        private string _CurrentProfile = "";
        private bool _ShowReleases = true;
        private int _CurrentInsiderAccount = 0;

        public bool IsFirstLaunch
        {
            get { return _IsFirstLaunch; }
            set { _IsFirstLaunch = value; OnPropertyChanged(nameof(IsFirstLaunch)); }
        }

        public string CurrentInstallation
        {
            get { return _CurrentInstallation; }
            set { _CurrentInstallation = value; OnPropertyChanged(nameof(CurrentInstallation)); }
        }

        public string CurrentProfile
        {
            get { return _CurrentProfile; }
            set { _CurrentProfile = value; OnPropertyChanged(nameof(CurrentProfile)); }
        }
        public bool ShowReleases
        {
            get { return _ShowReleases; }
            set 
            {
                if (!(_ShowBetas == false && value == false)) _ShowReleases = value;
                OnPropertyChanged(nameof(ShowReleases));
                OnPropertyChanged(nameof(ShowBetas));
            }
        }
        public bool ShowBetas
        {
            get { return _ShowBetas; }
            set 
            {
                if (!(_ShowReleases == false && value == false)) _ShowBetas = value;
                OnPropertyChanged(nameof(ShowBetas));
                OnPropertyChanged(nameof(ShowReleases));
            }
        }
        public int CurrentInsiderAccount
        {
            get { return _CurrentInsiderAccount; }
            set { _CurrentInsiderAccount = value; OnPropertyChanged(nameof(CurrentInsiderAccount)); }
        }

        #endregion

        #region Shortcut Settings

        private bool _HideJavaShortcut = false;
        private bool _ShowExternalLauncher = false;
        private string _ExternalLauncherPath = "";
        private string _ExternalLauncherName = "";
        private string _ExternalLauncherIconPath = "";
        private bool _CloseLauncherOnSwitch = true;
        private bool _EnableDungeonsSupport = false;
        private string _ExternalLauncherArguments = "";

        public bool HideJavaShortcut
        {
            get { return _HideJavaShortcut; }
            set { _HideJavaShortcut = value; OnPropertyChanged(nameof(HideJavaShortcut)); }
        }
        public bool ShowExternalLauncher
        {
            get { return _ShowExternalLauncher; }
            set { _ShowExternalLauncher = value; OnPropertyChanged(nameof(ShowExternalLauncher)); }
        }
        public string ExternalLauncherName
        {
            get { return _ExternalLauncherName; }
            set { _ExternalLauncherName = value; OnPropertyChanged(nameof(ExternalLauncherName)); }
        }
        public string ExternalLauncherPath
        {
            get { return _ExternalLauncherPath; }
            set { _ExternalLauncherPath = value; OnPropertyChanged(nameof(ExternalLauncherPath)); }
        }
        public string ExternalLauncherArguments
        {
            get { return _ExternalLauncherArguments; }
            set { _ExternalLauncherArguments = value; OnPropertyChanged(nameof(ExternalLauncherArguments)); }
        }
        public string ExternalLauncherIconPath
        {
            get { return _ExternalLauncherIconPath; }
            set { _ExternalLauncherIconPath = value; OnPropertyChanged(nameof(ExternalLauncherIconPath)); }
        }
        public bool CloseLauncherOnSwitch
        {
            get { return _CloseLauncherOnSwitch; }
            set { _CloseLauncherOnSwitch = value; OnPropertyChanged(nameof(CloseLauncherOnSwitch)); }
        }
        public bool EnableDungeonsSupport
        {
            get { return _EnableDungeonsSupport; }
            set { _EnableDungeonsSupport = value; OnPropertyChanged(nameof(EnableDungeonsSupport)); }
        }

        #endregion

    }
}
