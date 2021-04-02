using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using System.IO;
using BedrockLauncher.Classes;
using Version = BedrockLauncher.Classes.Version;
using System.Windows.Threading;
using System.Diagnostics;
using BedrockLauncher.Methods;

namespace BedrockLauncher.Core
{

    public static class ConfigManager
    {
        #region Public Storage

        public static ProfileList ProfileList { get => Profiles; }
        public static string CurrentProfile { get => Properties.Settings.Default.CurrentProfile; }
        public static VersionList AvaliableVersions { get => Versions; }
        public static List<Installation> CurrentInstallations { get => Installations; }
        public static GameManager GameManager { get; private set; } = new GameManager();

        #endregion

        #region Private Storage

        private static ProfileList Profiles { get; set; }
        private static List<Installation> Installations { get; set; }
        private static VersionList Versions { get; set; }

        #endregion

        #region Events

        public static event EventHandler ConfigStateChanged;
        public class ConfigStateArgs : EventArgs
        {
            public static ConfigStateArgs Empty => new ConfigStateArgs();
        }
        public static void OnConfigStateChanged(object sender, ConfigStateArgs e)
        {
            EventHandler handler = ConfigStateChanged;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        #endregion

        #region Init

        public static void Init()
        {
            LoadVersions();
            LoadProfiles();
            LoadInstallations();
        }

        #endregion

        #region General

        public static MainWindow MainThread => (MainWindow)System.Windows.Application.Current.MainWindow;

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

        #endregion

        #region Version Management

        public static void LoadVersions()
        {
            Versions = new VersionList("versions.json", GameManager);
            RefreshVersionsCache();
        }
        public static void RefreshVersionsCache()
        {
            MainThread.Dispatcher.Invoke(async () =>
            {
                try
                {
                    await Versions.LoadFromCache();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("List cache load failed:\n" + e.ToString());
                }
                try
                {
                    await Versions.DownloadList();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("List download failed:\n" + e.ToString());
                }
            });

        }

        #endregion

        #region Profiles

        public static void SaveProfiles(ProfileList profileList)
        {
            string json = JsonConvert.SerializeObject(profileList, Formatting.Indented);
            File.WriteAllText("user_profile.json", json);
        }
        public static void LoadProfiles()
        {
            string json;
            ProfileList profileList;
            if (File.Exists("user_profile.json"))
            {
                json = File.ReadAllText("user_profile.json");
                try { profileList = JsonConvert.DeserializeObject<ProfileList>(json, JsonSerializerSettings); }
                catch { profileList = new ProfileList(); }
            }
            else profileList = new ProfileList();

            if (profileList.profiles == null) profileList.profiles = new Dictionary<string, ProfileSettings>();

            Console.WriteLine("Profile count: " + profileList.profiles.Count);
            foreach (ProfileSettings setting in profileList.profiles.Values)
            {
                Console.WriteLine("\nProfile found!: ");
                Console.WriteLine("Name: " + setting.Name);
                Console.WriteLine("Path: " + setting.ProfilePath);
                Console.WriteLine("Skin: " + setting.SkinPath);

                if (setting.Installations == null) setting.Installations = new List<Installation>(); 
                Console.WriteLine("Installations: " + setting.Installations.Count);
            }

            Profiles = profileList;
        }
        public static bool CreateProfile(string profile)
        {
            ProfileSettings profileSettings = new ProfileSettings();

            if (ProfileList.profiles.ContainsKey(profile)) return false;
            else
            {
                // default settings
                profileSettings.Name = profile;
                profileSettings.SkinPath = null;
                profileSettings.ProfilePath = profile;

                ProfileList.profiles.Add(profile, profileSettings);

                Properties.Settings.Default.CurrentProfile = profile;
                Properties.Settings.Default.Save();

                SaveProfiles(ProfileList);
                return true;
            }
        }
        public static void RemoveProfile(string profile)
        {
            if (ProfileList.profiles.ContainsKey(profile) && ProfileList.profiles.Count > 1)
            {
                ProfileList.profiles.Remove(profile);
                SaveProfiles(ProfileList);

                var first_profile = ProfileList.profiles.FirstOrDefault();

                SwitchProfile(first_profile.Key);
            }

        }
        public static void SwitchProfile(string profile)
        {
            if (ProfileList.profiles.ContainsKey(profile))
            {
                Properties.Settings.Default.CurrentProfile = profile;
                Properties.Settings.Default.Save();
            }

        }

        #endregion

        #region Installations
        public static void LoadInstallations()
        {
            Installations = ConfigManager.GetInstallations();
            Installations = ConfigManager.GetEnforcedInstallations(Installations);
        }
        public static List<Installation> GetEnforcedInstallations(List<Installation> installations)
        {
            if (!installations.Exists(x => x.UseLatestVersion && !x.UseLatestBeta))
            {
                Installation latest_release = new Installation();
                latest_release.DisplayName = "Latest Release";
                latest_release.UseLatestVersion = true;
                latest_release.IconPath = @"/BedrockLauncher;component/Resources/images/grass_block_icon.ico";
                installations.Add(latest_release);
            }

            if (!installations.Exists(x => x.UseLatestVersion && x.UseLatestBeta))
            {
                Installation latest_beta = new Installation();
                latest_beta.DisplayName = "Latest Beta";
                latest_beta.UseLatestVersion = true;
                latest_beta.UseLatestBeta = true;
                latest_beta.IconPath = @"/BedrockLauncher;component/Resources/images/crafting_table_block_icon.ico";
                installations.Add(latest_beta);
            }

            return installations;

        }
        public static List<Installation> GetInstallations()
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfile))
            {
                if (ProfileList.profiles[CurrentProfile].Installations == null) ProfileList.profiles[CurrentProfile].Installations = new List<Installation>();
                return ProfileList.profiles[CurrentProfile].Installations;
            }
            else return new List<Installation>();
        }
        public static void CreateInstallation(string name, Version version, string iconPath = @"/BedrockLauncher;component/Resources/images/grass_block_icon.ico")
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfile))
            {
                Installation installation = new Installation();
                installation.DisplayName = name;
                installation.IconPath = iconPath;

                if (version.UUID == "latest_release")
                {
                    installation.UseLatestVersion = true;
                }
                else if (version.UUID == "latest_beta")
                {
                    installation.UseLatestVersion = true;
                    installation.UseLatestBeta = true;
                }
                else
                {
                    installation.VersionUUID = version.UUID;
                }


                if (ProfileList.profiles[CurrentProfile].Installations == null) ProfileList.profiles[CurrentProfile].Installations = new List<Installation>();
                ProfileList.profiles[CurrentProfile].Installations.Add(installation);
                SaveProfiles(ProfileList);
            }
        }
        public static void DeleteInstallation(Installation installation)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfile))
            {
                if (ProfileList.profiles[CurrentProfile].Installations == null) return;

                ProfileList.profiles[CurrentProfile].Installations.RemoveAll(x => x.DisplayName == installation.DisplayName);
                SaveProfiles(ProfileList);
            }
        }

        #endregion

        #region ListView Filters

        public static bool Filter_InstallationList(object obj)
        {
            Installation v = obj as Installation;
            if (!Properties.Settings.Default.ShowInstallationBetas && v.IsBeta) return false;
            else if (!Properties.Settings.Default.ShowInstallationReleases && !v.IsBeta) return false;
            else return true;
        }

        #endregion
    }
}
