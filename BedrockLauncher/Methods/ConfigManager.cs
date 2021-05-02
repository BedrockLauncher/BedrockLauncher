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
using MCVersion = BedrockLauncher.Classes.MCVersion;
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections;

namespace BedrockLauncher.Methods
{

    public static class ConfigManager
    {
        #region Helpers

        public static string CurrentProfile { get => Properties.Settings.Default.CurrentProfile; }
        public static MCInstallation CurrentInstallation 
        { 
            get
            {
                if (ConfigManager.CurrentInstallations == null) return null;
                return ConfigManager.CurrentInstallations[Properties.Settings.Default.CurrentInstallation];
            }
        }

        #endregion

        #region Storage Holders


        public static MCProfilesList ProfileList { get; private set; }
        public static MCVersionList Versions { get; private set; }
        public static GameManager GameManager { get; private set; } = new GameManager();
        public static List<MCInstallation> CurrentInstallations { get; private set; }


        #endregion

        #region Events

        public static event EventHandler ConfigStateChanged;
        public class ConfigStateArgs : EventArgs
        {
            public static new ConfigStateArgs Empty => new ConfigStateArgs();
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

        public static void Init(bool SkipVersions = false)
        {
            if (!SkipVersions) LoadVersions();
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

        #region Versions

        public static void LoadVersions()
        {
            Versions = new MCVersionList(Filepaths.GetVersionsFilePath(), GameManager);
            ReloadVersions();
        }
        public static void ReloadVersions()
        {
            MainThread.Dispatcher.Invoke((Func<Task>)(async () =>
            {
                try
                {
                    await ConfigManager.Versions.LoadFromCache();
                }
                catch (Exception e)
                {
                    Program.Log("List cache load failed:\n" + e.ToString());
                }
                try
                {
                    await ConfigManager.Versions.DownloadList();
                }
                catch (Exception e)
                {
                    Program.Log("List download failed:\n" + e.ToString());
                }
            }));

        }

        #endregion

        #region Profiles

        public static void LoadProfiles()
        {
            string json;
            MCProfilesList profileList;
            if (File.Exists(Filepaths.GetProfilesFilePath()))
            {
                json = File.ReadAllText(Filepaths.GetProfilesFilePath());
                try { profileList = JsonConvert.DeserializeObject<MCProfilesList>(json, JsonSerializerSettings); }
                catch { profileList = new MCProfilesList(); }
            }
            else profileList = new MCProfilesList();

            if (profileList.profiles == null) profileList.profiles = new Dictionary<string, MCProfile>();

            Program.LogConsoleLine("Profile count: " + profileList.profiles.Count);
            foreach (MCProfile setting in profileList.profiles.Values)
            {
                Program.LogConsoleLine("\nProfile found!: ");
                Program.LogConsoleLine("Name: " + setting.Name);
                Program.LogConsoleLine("Path: " + setting.ProfilePath);

                if (setting.Installations == null) setting.Installations = new List<MCInstallation>();
                Program.LogConsoleLine("Installations: " + setting.Installations.Count);
            }

            ProfileList = profileList;

            if (ProfileList.profiles.Count() == 0) MainThread.SetOverlayFrame(new Pages.FirstLaunch.WelcomePage());
        }
        public static MCProfilesList CleanProfiles()
        {
            MCProfilesList cleaned_list = ProfileList;
            foreach (var profile in cleaned_list.profiles)
            {
                profile.Value.Installations.RemoveAll(x => x.ReadOnly);
            }
            return cleaned_list;

        }
        public static void SaveProfiles()
        {
            string json = JsonConvert.SerializeObject(CleanProfiles(), Formatting.Indented);
            File.WriteAllText(Filepaths.GetProfilesFilePath(), json);
            Init(true);
        }
        public static bool CreateProfile(string profile)
        {
            MCProfile profileSettings = new MCProfile();

            if (ProfileList == null) ProfileList = new MCProfilesList();

            if (ProfileList.profiles.ContainsKey(profile)) return false;
            else
            {
                // default settings
                profileSettings.Name = profile;
                profileSettings.ProfilePath = ValidatePathName(profile);
                profileSettings.Installations = new List<MCInstallation>();

                ProfileList.profiles.Add(profile, profileSettings);

                Properties.Settings.Default.CurrentProfile = profile;
                Properties.Settings.Default.Save();

                SaveProfiles();
                return true;
            }

            string ValidatePathName(string pathName)
            {
                char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
                return new string(pathName.Where(ch => !invalidFileNameChars.Contains(ch)).ToArray());
            }
        }
        public static void RemoveProfile(string profile)
        {
            if (ProfileList.profiles.ContainsKey(profile) && ProfileList.profiles.Count > 1)
            {
                ProfileList.profiles.Remove(profile);
                SaveProfiles();

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
            CurrentInstallations = ConfigManager.GetInstallations();

            MCInstallation latest_release = new MCInstallation();
            latest_release.DisplayName = "Latest Release";
            latest_release.DirectoryName = "Latest Release";
            latest_release.VersionUUID = "latest_release";
            latest_release.UseLatestVersion = true;
            latest_release.UseLatestBeta = false;
            latest_release.IconPath = @"/BedrockLauncher;component/Resources/images/installation_icons/Grass_Block.png";
            latest_release.ReadOnly = true;
            CurrentInstallations.Add(latest_release);

            MCInstallation latest_beta = new MCInstallation();
            latest_beta.DisplayName = "Latest Beta";
            latest_beta.DirectoryName = "Latest Beta";
            latest_beta.VersionUUID = "latest_beta";
            latest_beta.UseLatestVersion = true;
            latest_beta.UseLatestBeta = true;
            latest_beta.IconPath = @"/BedrockLauncher;component/Resources/images/installation_icons/Crafting_Table.png";
            latest_beta.ReadOnly = true;
            CurrentInstallations.Add(latest_beta);
        }
        public static List<MCInstallation> GetInstallations()
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfile))
            {
                if (ProfileList.profiles[CurrentProfile].Installations == null) ProfileList.profiles[CurrentProfile].Installations = new List<MCInstallation>();
                return ProfileList.profiles[CurrentProfile].Installations;
            }
            else return new List<MCInstallation>();
        }
        public static void EditInstallation(int index, string name, string directory, MCVersion version, string iconPath = @"/BedrockLauncher;component/Resources/images/installation_icons/Furnace.png", bool isCustom = false)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfile) && ProfileList.profiles[CurrentProfile].Installations.Count > index)
            {
                MCInstallation installation = new MCInstallation();
                installation.DisplayName = name;
                installation.IconPath = iconPath;
                installation.IsCustomIcon = isCustom;
                installation.DirectoryName = directory;


                if (version == null || version.UUID == "latest_release")
                {
                    installation.UseLatestVersion = true;
                    installation.VersionUUID = "latest_release";
                }
                else if (version.UUID == "latest_beta")
                {
                    installation.UseLatestVersion = true;
                    installation.UseLatestBeta = true;
                    installation.VersionUUID = "latest_beta";
                }
                else
                {
                    installation.VersionUUID = version.UUID;
                }


                if (ProfileList.profiles[CurrentProfile].Installations == null) ProfileList.profiles[CurrentProfile].Installations = new List<MCInstallation>();
                ProfileList.profiles[CurrentProfile].Installations[index] = installation;
                SaveProfiles();
            }
        }

        public static void CreateInstallation(string name, MCVersion version, string directory, string iconPath = @"/BedrockLauncher;component/Resources/images/installation_icons/Furnace.png", bool isCustom = false)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfile))
            {
                MCInstallation installation = new MCInstallation();
                installation.DisplayName = name;
                installation.IconPath = iconPath;
                installation.IsCustomIcon = isCustom;
                installation.DirectoryName = directory;


                if (version == null || version.UUID == "latest_release")
                {
                    installation.UseLatestVersion = true;
                    installation.VersionUUID = "latest_release";
                }
                else if (version.UUID == "latest_beta")
                {
                    installation.UseLatestVersion = true;
                    installation.UseLatestBeta = true;
                    installation.VersionUUID = "latest_beta";
                }
                else
                {
                    installation.VersionUUID = version.UUID;
                }


                if (ProfileList.profiles[CurrentProfile].Installations == null) ProfileList.profiles[CurrentProfile].Installations = new List<MCInstallation>();
                ProfileList.profiles[CurrentProfile].Installations.Add(installation);
                SaveProfiles();
            }
        }

        public static void DuplicateInstallation(MCInstallation installation)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfile))
            {
                if (ProfileList.profiles[CurrentProfile].Installations == null) return;

                string newName = installation.DisplayName;
                int i = 1;

                while (ProfileList.profiles[CurrentProfile].Installations.Exists(x => x.DisplayName == newName))
                {
                    newName = newName + "(" + i + ")";
                    i++;
                }

                CreateInstallation(newName, installation.Version, installation.DirectoryName, installation.IconPath, installation.IsCustomIcon);
            }
        }

        public static void DeleteInstallation(MCInstallation installation)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfile))
            {
                if (ProfileList.profiles[CurrentProfile].Installations == null) return;

                ProfileList.profiles[CurrentProfile].Installations.RemoveAll(x => x.DisplayName == installation.DisplayName && !x.ReadOnly);
                SaveProfiles();
            }
        }

        #endregion

        #region ListView Filters/Sorting

        public static bool Filter_PatchNotes(object obj)
        {
            MCPatchNotesItem v = obj as MCPatchNotesItem;

            if (v != null)
            {
                if (!Properties.Settings.Default.ShowBetas && v.isBeta) return false;
                else if (!Properties.Settings.Default.ShowReleases && !v.isBeta) return false;
                else return true;
            }
            else return false;
        }
        public static bool Filter_VersionList(object obj)
        {
            MCVersion v = obj as MCVersion;

            if (v.IsInstalled)
            {
                if (!Properties.Settings.Default.ShowBetas && v.IsBeta) return false;
                else if (!Properties.Settings.Default.ShowReleases && !v.IsBeta) return false;
                else return true;
            }
            else return false;

        }
        public static bool Filter_InstallationList(object obj)
        {
            MCInstallation v = obj as MCInstallation;
            if (!Properties.Settings.Default.ShowBetas && v.IsBeta) return false;
            else if (!Properties.Settings.Default.ShowReleases && !v.IsBeta) return false;
            else return true;
        }

        #endregion
    }
}
