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
using BedrockLauncher.Core.Classes;
using BedrockLauncher.Core.Interfaces;
using System.Collections.ObjectModel;

namespace BedrockLauncher.ViewModels
{

    public class ConfigManager
    {

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
        public BLInstallation CurrentInstallation 
        { 
            get
            {
                if (CurrentInstallations == null || Properties.LauncherSettings.Default.CurrentInstallation == -1) return null;
                return CurrentInstallations[Properties.LauncherSettings.Default.CurrentInstallation];
            }
        }

        #endregion

        #region Storage Holders


        public MCProfilesList ProfileList { get; private set; } = new MCProfilesList();
        public ObservableCollection<BLVersion> Versions { get; private set; } = new ObservableCollection<BLVersion>();
        public ObservableCollection<BLInstallation> CurrentInstallations { get; private set; } = new ObservableCollection<BLInstallation>();


        #endregion

        #region Init

        public void Reload()
        {
            LoadProfiles();
            LoadInstallations();
        }

        public void Init()
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
        public void LoadVersions()
        {
            LauncherModel.MainThread.Dispatcher.Invoke((Func<Task>)(async () =>
            {
                await LauncherModel.Default.VersionDownloader.UpdateVersions(Versions);
            }));
        }

        #endregion

        #region Profiles

        public void LoadProfiles()
        {
            if (ProfileList.profiles == null) ProfileList.profiles = new Dictionary<string, MCProfile>();
            else ProfileList.profiles.Clear();

            ProfileList.profiles = GetProfiles();
            if (ProfileList.profiles.Count() == 0) ViewModels.LauncherModel.Default.SetOverlayFrame_Strict(new Pages.FirstLaunch.WelcomePage());

            Dictionary<string, MCProfile> GetProfiles()
            {
                string json;
                MCProfilesList fileData;
                if (File.Exists(LauncherModel.Default.FilepathManager.GetProfilesFilePath()))
                {
                    json = File.ReadAllText(LauncherModel.Default.FilepathManager.GetProfilesFilePath());
                    try { fileData = JsonConvert.DeserializeObject<MCProfilesList>(json, JsonSerializerSettings); }
                    catch { fileData = new MCProfilesList(); }
                }
                else fileData = new MCProfilesList();

                if (fileData.profiles == null) return new Dictionary<string, MCProfile>();
                else return fileData.profiles;
            }
        }
        public void SaveProfiles()
        {
            string json = JsonConvert.SerializeObject(CleanProfiles(), Formatting.Indented);
            File.WriteAllText(LauncherModel.Default.FilepathManager.GetProfilesFilePath(), json);
            Reload();

            MCProfilesList CleanProfiles()
            {
                MCProfilesList cleaned_list = ProfileList;
                foreach (var profile in cleaned_list.profiles) profile.Value.Installations.RemoveAll(x => x.ReadOnly);
                return cleaned_list;
            }
        }
        public bool CreateProfile(string profile)
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
        public void RemoveProfile(string profile)
        {
            if (ProfileList.profiles.ContainsKey(profile) && ProfileList.profiles.Count > 1)
            {
                ProfileList.profiles.Remove(profile);
                SaveProfiles();

                var first_profile = ProfileList.profiles.FirstOrDefault();

                SwitchProfile(first_profile.Key);
            }

        }
        public void SwitchProfile(string profile)
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
        public void LoadInstallations(string profile = null)
        {
            if (profile == null) profile = CurrentProfile;
            CurrentInstallations.Clear();
            GetInstallations().ForEach(x => CurrentInstallations.Add(x));

            BLInstallation latest_release = new BLInstallation();
            latest_release.DisplayName = "Latest Release";
            latest_release.DirectoryName = "Latest Release";
            latest_release.VersionUUID = "latest_release";
            latest_release.UseLatestVersion = true;
            latest_release.UseLatestBeta = false;
            latest_release.IconPath = LauncherModel.Default.FilepathManager.PrefabedIconRootPath + "Grass_Block.png";
            latest_release.ReadOnly = true;
            if (!CurrentInstallations.ToList().Exists(x => x.DisplayName == "Latest Release" && x.ReadOnly)) CurrentInstallations.Add(latest_release);

            BLInstallation latest_beta = new BLInstallation();
            latest_beta.DisplayName = "Latest Beta";
            latest_beta.DirectoryName = "Latest Beta";
            latest_beta.VersionUUID = "latest_beta";
            latest_beta.UseLatestVersion = true;
            latest_beta.UseLatestBeta = true;
            latest_beta.IconPath = LauncherModel.Default.FilepathManager.PrefabedIconRootPath + "Crafting_Table.png";
            latest_beta.ReadOnly = true;
            if (!CurrentInstallations.ToList().Exists(x => x.DisplayName == "Latest Beta" && x.ReadOnly)) CurrentInstallations.Add(latest_beta);

            List<BLInstallation> GetInstallations()
            {
                if (ProfileList.profiles.ContainsKey(profile))
                {
                    if (ProfileList.profiles[profile].Installations == null) ProfileList.profiles[profile].Installations = new List<MCInstallation>();
                    return new List<BLInstallation>(ProfileList.profiles[profile].Installations.ConvertAll(x => BLInstallation.Convert(x)));
                }
                else return new List<BLInstallation>();
            }
        }
        public void EditInstallation(int index, string name, string directory, BLVersion version, string iconPath = @"/BedrockLauncher;component/Resources/images/installation_icons/Furnace.png", bool isCustom = false)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfile) && ProfileList.profiles[CurrentProfile].Installations.Count > index)
            {
                BLInstallation installation = new BLInstallation();
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
        public void CreateInstallation(string name, MCVersion version, string directory, string iconPath = @"/BedrockLauncher;component/Resources/images/installation_icons/Furnace.png", bool isCustom = false)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfile))
            {
                BLInstallation installation = new BLInstallation();
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
        public void DuplicateInstallation(BLInstallation installation)
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
        public void DeleteInstallation(BLInstallation installation)
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

        public bool Filter_VersionList(object obj)
        {
            BLVersion v = BLVersion.Convert(obj as MCVersion);

            if (v != null && v.IsInstalled)
            {
                if (!Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return false;
                else if (!Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return false;
                else return true;
            }
            else return false;

        }
        public bool Filter_InstallationList(object obj)
        {
            BLInstallation v = BLInstallation.Convert(obj as MCInstallation);
            if (v == null) return false;
            else if (!Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return false;
            else if (!Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return false;
            else return true;
        }

        #endregion
    }
}
