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
using BedrockLauncher.Core.Components;
using System.Windows.Data;

namespace BedrockLauncher.ViewModels
{
    public class ConfigManager : NotifyPropertyChangedBase
    {
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

        #region Versions

        #region Definitions
        public ObservableCollection<BLVersion> Versions { get; private set; } = new ObservableCollection<BLVersion>();

        private bool _IsVersionsUpdating = false;
        public bool IsVersionsUpdating
        {
            get { return _IsVersionsUpdating; }
            set { _IsVersionsUpdating = value; OnPropertyChanged(nameof(IsVersionsUpdating)); }
        }
        #endregion

        #region Methods
        public async void LoadVersions()
        {
            if (IsVersionsUpdating) return;
            await Application.Current.Dispatcher.Invoke((Func<Task>)(async () =>
            {
                IsVersionsUpdating = true;
                await LauncherModel.Default.VersionDownloader.UpdateVersions(Versions);
                IsVersionsUpdating = false;
            }));
        }
        #endregion

        #region Filters & Sorting

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

        #endregion

        #endregion

        #region Profiles

        #region Definitions
        public MCProfile CurrentProfile
        {
            get
            {
                string savedProfileKey = Properties.LauncherSettings.Default.CurrentProfile;
                if (ProfileList.profiles != null)
                {
                    if (ProfileList.profiles.ContainsKey(savedProfileKey)) return ProfileList.profiles[savedProfileKey];
                    else if (ProfileList.profiles.Count != 0)
                    {
                        var result = ProfileList.profiles.First();
                        Properties.LauncherSettings.Default.CurrentProfile = result.Key;
                        Properties.LauncherSettings.Default.Save();
                        return result.Value;
                    }
                }
                return null;
            }
        }
        public MCProfilesList ProfileList { get; private set; } = new MCProfilesList();
        public string CurrentProfileName
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
        #endregion

        #region Methods
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
                    try { fileData = JsonConvert.DeserializeObject<MCProfilesList>(json, GetSerializerSettings()); }
                    catch { fileData = new MCProfilesList(); }
                }
                else fileData = new MCProfilesList();

                if (fileData.profiles == null) return new Dictionary<string, MCProfile>();
                else return fileData.profiles;
            }

            JsonSerializerSettings GetSerializerSettings()
            {
                var settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.MissingMemberHandling = MissingMemberHandling.Ignore;
                return settings;
            }
        }
        public void SaveProfiles()
        {
            string json = JsonConvert.SerializeObject(ProfileList, Formatting.Indented);
            File.WriteAllText(LauncherModel.Default.FilepathManager.GetProfilesFilePath(), json);
            Reload();
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

        #endregion

        #region Installations

        #region Constants & Enums

        public enum SortBy_Installation : int
        {
            LatestPlayed,
            Name
        }

        public const string InstallationDefaultIcon = @"Furnace.png";

        #endregion

        #region Definitions

        public ObservableCollection<BLInstallation> CurrentInstallations { get; private set; } = new ObservableCollection<BLInstallation>();

        public BLInstallation CurrentInstallation
        {
            get
            {
                string uuid = Properties.LauncherSettings.Default.CurrentInstallation;
                if (CurrentInstallations == null || !CurrentInstallations.ToList().Exists(x => x.InstallationUUID == uuid)) return null;
                return CurrentInstallations.Where(x => x.InstallationUUID == uuid).First();
            }
        }

        public SortBy_Installation Installations_SortFilter { get; set; } = SortBy_Installation.LatestPlayed;
        public string Installations_SearchFilter { get; set; } = string.Empty;

        #endregion

        #region Methods

        public void InitalizeInstallations()
        {
            BLInstallation latest_release = new BLInstallation();
            latest_release.DisplayName = "Latest Release";
            latest_release.DirectoryName = "Latest Release";
            latest_release.VersionUUID = "latest_release";
            latest_release.UseLatestVersion = true;
            latest_release.UseLatestBeta = false;
            latest_release.IconPath = "Grass_Block.png";
            latest_release.IsCustomIcon = false;
            latest_release.ReadOnly = true;
            latest_release.InstallationUUID = "latest_release";
            if (!CurrentInstallations.ToList().Exists(x => x.InstallationUUID == "latest_release" && x.ReadOnly))
            {
                CurrentInstallations.Add(latest_release);
                ProfileList.profiles[CurrentProfileName].Installations.Add(latest_release);
                SaveProfiles();
            }

            BLInstallation latest_beta = new BLInstallation();
            latest_beta.DisplayName = "Latest Beta";
            latest_beta.DirectoryName = "Latest Beta";
            latest_beta.VersionUUID = "latest_beta";
            latest_beta.UseLatestVersion = true;
            latest_beta.UseLatestBeta = true;
            latest_beta.IconPath = "Crafting_Table.png";
            latest_beta.IsCustomIcon = false;
            latest_beta.ReadOnly = true;
            latest_beta.InstallationUUID = "latest_beta";
            if (!CurrentInstallations.ToList().Exists(x => x.InstallationUUID == "latest_beta" && x.ReadOnly))
            {
                CurrentInstallations.Add(latest_beta);
                ProfileList.profiles[CurrentProfileName].Installations.Add(latest_beta);
                SaveProfiles();
            }
        }
        public void LoadInstallations(string profile = null)
        {
            if (profile == null) profile = CurrentProfileName;
            CurrentInstallations.Clear();
            GetInstallations().ForEach(x => CurrentInstallations.Add(x));
            InitalizeInstallations();

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
        public void EditInstallation(int index, string name, string directory, BLVersion version, string iconPath = InstallationDefaultIcon, bool isCustom = false)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfileName) && ProfileList.profiles[CurrentProfileName].Installations.Count > index)
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


                if (ProfileList.profiles[CurrentProfileName].Installations == null) ProfileList.profiles[CurrentProfileName].Installations = new List<MCInstallation>();
                ProfileList.profiles[CurrentProfileName].Installations[index] = installation;
                SaveProfiles();
            }
        }
        public void CreateInstallation(string name, MCVersion version, string directory, string iconPath = InstallationDefaultIcon, bool isCustom = false)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfileName))
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


                if (ProfileList.profiles[CurrentProfileName].Installations == null) ProfileList.profiles[CurrentProfileName].Installations = new List<MCInstallation>();
                ProfileList.profiles[CurrentProfileName].Installations.Add(installation);
                SaveProfiles();
            }
        }
        public void DuplicateInstallation(BLInstallation installation)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfileName))
            {
                if (ProfileList.profiles[CurrentProfileName].Installations == null) return;

                string newName = installation.DisplayName;
                int i = 1;

                while (ProfileList.profiles[CurrentProfileName].Installations.Exists(x => x.DisplayName == newName))
                {
                    newName = newName + "(" + i + ")";
                    i++;
                }

                CreateInstallation(newName, installation.Version, installation.DirectoryName, installation.IconPath, installation.IsCustomIcon);
            }
        }
        public void DeleteInstallation(BLInstallation installation)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfileName))
            {
                if (ProfileList.profiles[CurrentProfileName].Installations == null) return;
                ProfileList.profiles[CurrentProfileName].Installations.RemoveAll(x => x.InstallationUUID == installation.InstallationUUID && !x.ReadOnly);
                SaveProfiles();
            }
        }
        public void TimestampInstallation(BLInstallation installation)
        {
            if (ProfileList.profiles.ContainsKey(CurrentProfileName) && ProfileList.profiles[CurrentProfileName].Installations.Exists(x => x.InstallationUUID == installation.InstallationUUID))
            {
                if (ProfileList.profiles[CurrentProfileName].Installations == null) ProfileList.profiles[CurrentProfileName].Installations = new List<MCInstallation>();
                ProfileList.profiles[CurrentProfileName].Installations.Where(x => x.InstallationUUID == installation.InstallationUUID).FirstOrDefault().LastPlayed = DateTime.Now;
                SaveProfiles();
            }
        }


        #endregion

        #region Filters & Sorting

        public bool Filter_InstallationList(object obj)
        {
            BLInstallation v = BLInstallation.Convert(obj as MCInstallation);
            if (v == null) return false;
            else if (!Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return false;
            else if (!Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return false;
            else if (!v.DisplayName.Contains(Installations_SearchFilter)) return false;
            else return true;
        }
        public void Sort_InstallationList(ref CollectionView view)
        {
            view.SortDescriptions.Clear();
            if (Installations_SortFilter == SortBy_Installation.LatestPlayed) view.SortDescriptions.Add(new System.ComponentModel.SortDescription("LastPlayedT", System.ComponentModel.ListSortDirection.Descending));
            if (Installations_SortFilter == SortBy_Installation.Name) view.SortDescriptions.Add(new System.ComponentModel.SortDescription("DisplayName", System.ComponentModel.ListSortDirection.Ascending));
        }

        #endregion

        #endregion
    }
}
