using BedrockLauncher.Classes;
using BedrockLauncher.Enums;
using BedrockLauncher.ViewModels;
using Newtonsoft.Json;
using PostSharp.Patterns.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            try
            {
                string json;

                if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                {
                    Default = new LauncherSettings();
                }
                else
                {
                    string settingsPath = MainDataModel.Default.FilePaths.GetSettingsFilePath();
                    if (File.Exists(settingsPath))
                    {
                        json = File.ReadAllText(settingsPath);
                        try
                        {
                            Default = JsonConvert.DeserializeObject<LauncherSettings>(json, JsonSerializerSettings)
                                      ?? new LauncherSettings();
                        }
                        catch
                        {
                            Default = new LauncherSettings();
                        }
                    }
                    else Default = new LauncherSettings();
                }

                Default.Init();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("LauncherSettings.Load failed; using defaults.");
                System.Diagnostics.Trace.WriteLine(ex);
                Default = new LauncherSettings();
            }
        }

        public void Init() =>
            // Host is registered by MainViewModel; during early splash startup it may still be null.
            MainDataModel.BackwardsCommunicationHost?.UpdateAnimatePageTransitions(_AnimatePageTransitions);

        public void Save()
        {
            try
            {
                string path = MainDataModel.Default.FilePaths.GetSettingsFilePath();
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Failed to save launcher settings:");
                System.Diagnostics.Trace.WriteLine(ex);
            }
        }

        public bool GetIsFirstLaunch(int LoadedConfigCount) => CurrentProfileUUID == "" || IsFirstLaunch || LoadedConfigCount == 0;

        private bool _AnimatePageTransitions = false;
        private bool _ShowBetas = true;
        private bool _ShowReleases = true;
        private bool _ShowPreviews = true;
        private bool _ShowUWP = true;
        private bool _ShowGDK = true;
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
        public bool FetchVersionsFromMicrosoftStore { get; set; } = true;
        public bool AnimatePageTransitions
        {
            get
            {
                return _AnimatePageTransitions;
            }
            set
            {
                _AnimatePageTransitions = value;
                MainDataModel.BackwardsCommunicationHost?.UpdateAnimatePageTransitions(value);
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
        public bool ShowUWP
        {
            get { return _ShowUWP; }
            set { _ShowUWP = value; }
        }
        public bool ShowGDK
        {
            get { return _ShowGDK; }
            set { _ShowGDK = value; }
        }
        public int CurrentInsiderAccountIndex { get; set; } = 0;

    }
}
