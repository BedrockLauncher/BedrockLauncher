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
                if (File.Exists(Filepaths.GetSettingsFilePath()))
                {
                    json = File.ReadAllText(Filepaths.GetSettingsFilePath());
                    try { Default = JsonConvert.DeserializeObject<LauncherSettings>(json, JsonSerializerSettings); }
                    catch { Default = new LauncherSettings(); }
                }
                else Default = new LauncherSettings();
            }


        }
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(Filepaths.GetSettingsFilePath(), json);
        }

        private bool _PortableMode = false;
        private string _FixedDirectory = "";

        private bool _ShowBetas = true;
        private int _CurrentInstallation = 0;
        private bool _IsFirstLaunch = true;
        private string _CurrentProfile = "";
        private bool _KeepLauncherOpen = false;
        private bool _ShowReleases = true;
        private int _CurrentInsiderAccount = 0;
        private bool _SaveRedirection = true;
        private bool _HideJavaShortcut = false;
        private bool _ShowExternalLauncher = false;
        private string _ExternalLauncherPath = "";
        private string _ExternalLauncherName = "";
        private string _ExternalLauncherIconPath = "";
        private string _CurrentTheme = "LatestUpdate";
        private bool _CloseLauncherOnSwitch = true;
        private bool _UseBetaBuilds = false;
        private bool _UseSilentUpdates = true;

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
        public bool IsFirstLaunch
        {
            get { return _IsFirstLaunch;}
            set { _IsFirstLaunch = value; OnPropertyChanged(nameof(IsFirstLaunch)); }
        }
        public int CurrentInstallation
        {
            get { return _CurrentInstallation; }
            set { _CurrentInstallation = value; OnPropertyChanged(nameof(CurrentInstallation)); }
        }
        public bool KeepLauncherOpen
        {
            get { return _KeepLauncherOpen; }
            set { _KeepLauncherOpen = value; OnPropertyChanged(nameof(KeepLauncherOpen)); }
        }
        public string CurrentProfile
        {
            get { return _CurrentProfile; }
            set { _CurrentProfile = value; OnPropertyChanged(nameof(CurrentProfile)); }
        }
        public bool ShowReleases
        {
            get { return _ShowReleases; }
            set { _ShowReleases = value; OnPropertyChanged(nameof(ShowReleases)); }
        }
        public bool ShowBetas
        {
            get { return _ShowBetas; }
            set { _ShowBetas = value; OnPropertyChanged(nameof(ShowBetas)); }
        }
        public int CurrentInsiderAccount
        {
            get { return _CurrentInsiderAccount; }
            set { _CurrentInsiderAccount = value; OnPropertyChanged(nameof(CurrentInsiderAccount)); }
        }
        public bool SaveRedirection
        {
            get { return _SaveRedirection; }
            set { _SaveRedirection = value; OnPropertyChanged(nameof(SaveRedirection)); }
        }
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
            set { _ExternalLauncherPath = value; OnPropertyChanged(nameof(_ExternalLauncherPath)); }
        }
        public string ExternalLauncherIconPath
        {
            get { return _ExternalLauncherIconPath; }
            set { _ExternalLauncherIconPath = value; OnPropertyChanged(nameof(ExternalLauncherIconPath)); }
        }
        public string CurrentTheme
        {
            get { return _CurrentTheme; }
            set { _CurrentTheme = value; OnPropertyChanged(nameof(CurrentTheme)); }
        }
        public bool CloseLauncherOnSwitch
        {
            get { return _CloseLauncherOnSwitch; }
            set { _CloseLauncherOnSwitch = value; OnPropertyChanged(nameof(CloseLauncherOnSwitch)); }
        }
        public bool UseBetaBuilds
        {
            get { return _UseBetaBuilds; }
            set { _UseBetaBuilds = value; OnPropertyChanged(nameof(UseBetaBuilds)); }
        }
        public bool UseSilentUpdates
        {
            get { return _UseSilentUpdates; }
            set { _UseSilentUpdates = value; OnPropertyChanged(nameof(UseSilentUpdates)); }
        }

    }
}
