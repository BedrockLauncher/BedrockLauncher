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
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections;
using BL_Core.Classes;
using BL_Core.Interfaces;

namespace BedrockLauncher.Methods
{

    public class ConfigManager : IConfigManager
    {
        public static ConfigManager Default { get; set; }

        static ConfigManager()
        {
            Default = new ConfigManager();
            MCInstallation.SetConfigManager(Default);
        }

        #region Helpers

        public string CurrentProfile 
        {
            get
            {
                if (ProfileList.profiles != null)
                {
                    if (ProfileList.profiles.ContainsKey(Properties.LauncherSettings.Default.CurrentProfile))
                    {
                        return Properties.LauncherSettings.Default.CurrentProfile;
                    }
                    else if (ProfileList.profiles.Count != 0)
                    {
                        string result = ProfileList.profiles.First().Key;
                        Properties.LauncherSettings.Default.CurrentProfile = result;
                        Properties.LauncherSettings.Default.Save();
                        return result;
                    }
                }
                return Properties.LauncherSettings.Default.CurrentProfile;
            }
        }
        public MCInstallation CurrentInstallation 
        { 
            get
            {
                if (CurrentInstallations == null || Properties.LauncherSettings.Default.CurrentInstallation == -1) return null;
                return CurrentInstallations[Properties.LauncherSettings.Default.CurrentInstallation];
            }
        }

        #endregion

        #region Storage Holders


        public MCProfilesList ProfileList { get; private set; }
        public MCVersionList Versions { get; private set; }
        public GameManager GameManager { get; private set; } = new GameManager();
        public  List<MCInstallation> CurrentInstallations { get; private set; }


        #endregion

        #region Events

        public  event EventHandler ConfigStateChanged;
        public  void OnConfigStateChanged(object sender, Events.ConfigStateArgs e)
        {
            EventHandler handler = ConfigStateChanged;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        #endregion

        #region Init

        public  void Reload()
        {
            LoadProfiles();
            LoadInstallations();
        }

        public  void Init()
        {
            LoadVersions();
            LoadProfiles();
            LoadInstallations();
        }

        #endregion

        #region General

        private  JsonSerializerSettings JsonSerializerSettings
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

        public  void LoadVersions()
        {
            Versions = new MCVersionList(FilepathManager.Default.GetVersionsFilePath(), FilepathManager.Default.GetUserVersionsFilePath());
            ReloadVersions();
        }
        public  void ReloadVersions()
        {
            ViewModels.LauncherModel.MainThread.Dispatcher.Invoke((Func<Task>)(async () =>
            {
                await GameManager.VersionDownloader.UpdateVersions(Versions);
            }));

        }

        #endregion

        #region Profiles

        public  void LoadProfiles()
        {
            string json;
            MCProfilesList profileList;
            if (File.Exists(FilepathManager.Default.GetProfilesFilePath()))
            {
                json = File.ReadAllText(FilepathManager.Default.GetProfilesFilePath());
                try { profileList = JsonConvert.DeserializeObject<MCProfilesList>(json, JsonSerializerSettings); }
                catch { profileList = new MCProfilesList(); }
            }
            else profileList = new MCProfilesList();

            if (profileList.profiles == null) profileList.profiles = new Dictionary<string, MCProfile>();

            ProfileList = profileList;

            if (ProfileList.profiles.Count() == 0) ViewModels.LauncherModel.Default.SetOverlayFrame_Strict(new Pages.FirstLaunch.WelcomePage());
        }
        public  MCProfilesList CleanProfiles()
        {
            MCProfilesList cleaned_list = ProfileList;
            foreach (var profile in cleaned_list.profiles)
            {
                profile.Value.Installations.RemoveAll(x => x.ReadOnly);
            }
            return cleaned_list;

        }
        public  void SaveProfiles()
        {
            string json = JsonConvert.SerializeObject(CleanProfiles(), Formatting.Indented);
            File.WriteAllText(FilepathManager.Default.GetProfilesFilePath(), json);
            Reload();
        }
        public  bool CreateProfile(string profile)
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

                Properties.LauncherSettings.Default.CurrentProfile = profile;
                Properties.LauncherSettings.Default.Save();

                SaveProfiles();
                return true;
            }

            string ValidatePathName(string pathName)
            {
                char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
                return new string(pathName.Where(ch => !invalidFileNameChars.Contains(ch)).ToArray());
            }
        }
        public  void RemoveProfile(string profile)
        {
            if (ProfileList.profiles.ContainsKey(profile) && ProfileList.profiles.Count > 1)
            {
                ProfileList.profiles.Remove(profile);
                SaveProfiles();

                var first_profile = ProfileList.profiles.FirstOrDefault();

                SwitchProfile(first_profile.Key);
            }

        }
        public  void SwitchProfile(string profile)
        {
            if (ProfileList.profiles.ContainsKey(profile))
            {
                Properties.LauncherSettings.Default.CurrentProfile = profile;
                Properties.LauncherSettings.Default.Save();
                LoadInstallations(profile);
            }

        }


        #endregion

        #region Installations
        public  void LoadInstallations(string profile = null)
        {
            if (profile == null) profile = CurrentProfile;
            CurrentInstallations = new List<MCInstallation>();
            CurrentInstallations = GetInstallations(profile);

            MCInstallation latest_release = new MCInstallation();
            latest_release.DisplayName = "Latest Release";
            latest_release.DirectoryName = "Latest Release";
            latest_release.VersionUUID = "latest_release";
            latest_release.UseLatestVersion = true;
            latest_release.UseLatestBeta = false;
            latest_release.IconPath_Full = @"/BedrockLauncher;component/Resources/images/installation_icons/Grass_Block.png";
            latest_release.ReadOnly = true;
            if (!CurrentInstallations.Exists(x => x.DisplayName == "Latest Release" && x.ReadOnly)) CurrentInstallations.Add(latest_release);

            MCInstallation latest_beta = new MCInstallation();
            latest_beta.DisplayName = "Latest Beta";
            latest_beta.DirectoryName = "Latest Beta";
            latest_beta.VersionUUID = "latest_beta";
            latest_beta.UseLatestVersion = true;
            latest_beta.UseLatestBeta = true;
            latest_beta.IconPath_Full = @"/BedrockLauncher;component/Resources/images/installation_icons/Crafting_Table.png";
            latest_beta.ReadOnly = true;
            if (!CurrentInstallations.Exists(x => x.DisplayName == "Latest Beta" && x.ReadOnly)) CurrentInstallations.Add(latest_beta);
        }
        public  List<MCInstallation> GetInstallations(string profile)
        {
            if (ProfileList.profiles.ContainsKey(profile))
            {
                if (ProfileList.profiles[profile].Installations == null) ProfileList.profiles[profile].Installations = new List<MCInstallation>();
                return ProfileList.profiles[profile].Installations;
            }
            else return new List<MCInstallation>();
        }
        public  void EditInstallation(int index, string name, string directory, MCVersion version, string iconPath = @"/BedrockLauncher;component/Resources/images/installation_icons/Furnace.png", bool isCustom = false)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfile) && ProfileList.profiles[CurrentProfile].Installations.Count > index)
            {
                MCInstallation installation = new MCInstallation();
                installation.DisplayName = name;
                installation.IconPath_Full = iconPath;
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
        public  void CreateInstallation(string name, MCVersion version, string directory, string iconPath = @"/BedrockLauncher;component/Resources/images/installation_icons/Furnace.png", bool isCustom = false)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfile))
            {
                MCInstallation installation = new MCInstallation();
                installation.DisplayName = name;
                installation.IconPath_Full = iconPath;
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
        public  void DuplicateInstallation(MCInstallation installation)
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

                CreateInstallation(newName, installation.Version, installation.DirectoryName, installation.IconPath_Full, installation.IsCustomIcon);
            }
        }
        public  void DeleteInstallation(MCInstallation installation)
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

        public  bool Filter_PatchNotes(object obj)
        {
            MCPatchNotesItem v = obj as MCPatchNotesItem;

            if (v != null)
            {
                if (!Properties.LauncherSettings.Default.ShowBetas && v.isBeta) return false;
                else if (!Properties.LauncherSettings.Default.ShowReleases && !v.isBeta) return false;
                else return true;
            }
            else return false;
        }
        public  bool Filter_VersionList(object obj)
        {
            MCVersion v = obj as MCVersion;

            if (v.IsInstalled)
            {
                if (!Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return false;
                else if (!Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return false;
                else return true;
            }
            else return false;

        }
        public  bool Filter_InstallationList(object obj)
        {
            MCInstallation v = obj as MCInstallation;
            if (!Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return false;
            else if (!Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return false;
            else return true;
        }

        #endregion
    }
}
