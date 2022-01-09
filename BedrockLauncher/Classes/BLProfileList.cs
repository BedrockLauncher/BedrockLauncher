using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using BedrockLauncher.Components;
using ExtensionsDotNET;
using Newtonsoft.Json;

namespace BedrockLauncher.Classes
{
    public class MCProfilesList : NotifyPropertyChangedBase
    {
        public int Version = 2;
        [JsonIgnore] public string FilePath { get; private set; } = string.Empty;
        public Dictionary<string, MCProfile> profiles { get; set; } = new Dictionary<string, MCProfile>();

        #region Runtime Values

        [JsonIgnore] private string _CurrentProfileUUID  = string.Empty;
        [JsonIgnore] private string _CurrentInstallationUUID  = string.Empty;

        [JsonIgnore] public string CurrentProfileUUID
        {
            get
            {
                return _CurrentProfileUUID;
            }
            set
            {
                _CurrentProfileUUID = value;
                MCProfileExtensions.SetCurrentProfile(value);
                OnPropertyChanged(nameof(CurrentProfileUUID));
                OnPropertyChanged(nameof(CurrentProfile));
                OnPropertyChanged(nameof(CurrentInstallations));
                OnPropertyChanged(nameof(CurrentInstallationUUID));
                OnPropertyChanged(nameof(CurrentInstallation));
            }
        }
        [JsonIgnore] public string CurrentInstallationUUID
        {
            get
            {
                return _CurrentInstallationUUID;
            }
            set
            {
                _CurrentInstallationUUID = value;
                MCProfileExtensions.SetCurrentInstallation(value);
                OnPropertyChanged(nameof(CurrentInstallationUUID));
                OnPropertyChanged(nameof(CurrentInstallation));
            }
        }
        [JsonIgnore] public MCProfile CurrentProfile
        {
            get
            {
                if (profiles.ContainsKey(CurrentProfileUUID)) return profiles[CurrentProfileUUID];
                else return null;
            }
            set
            {
                if (profiles.ContainsKey(CurrentProfileUUID)) profiles[CurrentProfileUUID] = value;
                OnPropertyChanged(nameof(CurrentProfile));
            }
        }
        [JsonIgnore] public BLInstallation CurrentInstallation
        {
            get
            {
                if (CurrentProfile == null) return null;
                else if (CurrentInstallations == null) return null;
                else if (CurrentInstallations.Any(x => x.InstallationUUID == CurrentInstallationUUID))
                    return CurrentInstallations.First(x => x.InstallationUUID == CurrentInstallationUUID);
                else return null;
            }
            set
            {
                if (CurrentProfile == null) return;
                else if (CurrentInstallations == null) return;
                else if (CurrentInstallations.Any(x => x.InstallationUUID == CurrentInstallationUUID))
                {
                    int index = CurrentInstallations.FindIndex(x => x.InstallationUUID == CurrentInstallationUUID);
                    CurrentInstallations[index] = value;
                }
                else return;
                OnPropertyChanged(nameof(CurrentInstallation));
            }
        }
        [JsonIgnore] public ObservableCollection<BLInstallation> CurrentInstallations
        {
            get
            {
                if (CurrentProfile == null) return null;
                else if (CurrentProfile.Installations == null) return null;
                else return CurrentProfile.Installations;
            }
            set
            {
                if (CurrentProfile == null) return;
                else if (CurrentProfile.Installations == null) return;
                else CurrentProfile.Installations = value;
                OnPropertyChanged(nameof(CurrentInstallations));
            }
        }

        #endregion

        #region IO Methods

        public static MCProfilesList Load(string filePath, string lastProfile = null, string lastInstallation = null)
        {
            string json;
            MCProfilesList fileData = new MCProfilesList();
            if (File.Exists(filePath))
            {
                json = File.ReadAllText(filePath);
                try
                {
                    fileData = JsonConvert.DeserializeObject<MCProfilesList>(json, new JsonSerializerSettings()
                    {
                        NullValueHandling = NullValueHandling.Include,
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    });
                }
                catch
                {
                    fileData = new MCProfilesList();
                }
            }
            fileData.FilePath = filePath;
            fileData.Init(lastProfile, lastInstallation);
            fileData.Validate();
            return fileData;
        }
        public void Init(string lastProfile = null, string lastInstallation = null)
        {
            if (profiles.ContainsKey(CurrentProfileUUID)) CurrentProfileUUID = lastProfile;
            else if (profiles.Count != 0) CurrentProfileUUID = profiles.First().Key;

            if (CurrentProfile != null)
            {
                if (CurrentInstallations.Any(x => x.InstallationUUID == lastInstallation)) CurrentInstallationUUID = lastInstallation;
                else if (CurrentInstallations.Count != 0) CurrentInstallationUUID = CurrentInstallations.First().InstallationUUID;
            }
        }
        public void Save(string filePath)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        public void Save()
        {
            if (!string.IsNullOrEmpty(FilePath)) Save(FilePath);
        }
        public void Validate()
        {
            BLInstallation latest_release = new BLInstallation()
            {
                DisplayName = "Latest Release",
                DirectoryName = "Latest Release",
                VersionUUID = "latest_release",
                VersioningMode = VersioningMode.LatestRelease,
                IconPath = "Grass_Block.png",
                IsCustomIcon = false,
                ReadOnly = true,
                InstallationUUID = "latest_release"
            };
            BLInstallation latest_beta = new BLInstallation()
            {
                DisplayName = "Latest Beta",
                DirectoryName = "Latest Beta",
                VersionUUID = "latest_beta",
                VersioningMode = VersioningMode.LatestBeta,
                IconPath = "Crafting_Table.png",
                IsCustomIcon = false,
                ReadOnly = true,
                InstallationUUID = "latest_beta"
            };


            foreach (var profile in profiles.Values)
            {
                if (!profile.Installations.Any(x => x.InstallationUUID == latest_release.InstallationUUID && x.ReadOnly))
                    Installation_Add(latest_release);
                if (!profile.Installations.Any(x => x.InstallationUUID == latest_beta.InstallationUUID && x.ReadOnly))
                    Installation_Add(latest_beta);

                foreach (var installation in profile.Installations.Where(x => x.VersionUUID == latest_release.VersionUUID))
                    installation.VersioningMode = VersioningMode.LatestRelease;

                foreach (var installation in profile.Installations.Where(x => x.VersionUUID == latest_beta.VersionUUID))
                    installation.VersioningMode = VersioningMode.LatestBeta;
            }

            Save();
        }

        #endregion

        #region Management Methods

        public bool Profile_Add(string profile)
        {
            MCProfile profileSettings = new MCProfile(profile, ValidatePathName(profile));

            if (profiles.ContainsKey(profile)) return false;
            else
            {
                profiles.Add(profile, profileSettings);
                Profile_Switch(profile);
                Validate();
                Save();
                return true;
            }

            string ValidatePathName(string pathName)
            {
                char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
                return new string(pathName.Where(ch => !invalidFileNameChars.Contains(ch)).ToArray());
            }
        }
        public void Profile_Remove(string profile)
        {
            if (profiles.ContainsKey(profile) && profiles.Count > 1)
            {
                profiles.Remove(profile);
                Save();
                Profile_Switch(profiles.FirstOrDefault().Key);
            }

        }
        public void Profile_Switch(string profileUUID)
        {
            if (profiles.ContainsKey(profileUUID))
            {
                this.CurrentProfileUUID = profileUUID;
            }
        }

        public void Installation_Add(BLInstallation installation)
        {
            if (CurrentProfile == null) return;
            if (CurrentInstallations == null) return;
            if (!CurrentInstallations.Any(x => x.InstallationUUID == installation.InstallationUUID))
            {
                CurrentInstallations.Add(installation);
                Save();
            }
        }
        public void Installation_Clone(BLInstallation installation)
        {
            if (CurrentProfile == null) return;
            if (CurrentInstallations == null) return;
            if (CurrentInstallations.Any(x => x.InstallationUUID == installation.InstallationUUID))
            {
                string newName = installation.DisplayName;
                int i = 1;

                while (CurrentInstallations.Any(x => x.DisplayName == newName))
                {
                    newName = newName + "(" + i + ")";
                    i++;
                }

                Installation_Add(installation.Clone(newName));
            }
        }
        public void Installation_Create(string name, MCVersion version, string directory, string iconPath = null, bool isCustom = false)
        {
            if (CurrentProfile == null) return;
            if (CurrentInstallations == null) return;

            MCProfileExtensions.GetVersionParams(version, out VersioningMode versioningMode, out string version_uuid);
            BLInstallation new_installation = new BLInstallation()
            {
                DisplayName = name,
                IconPath = (iconPath == null ? @"Furnace.png" : iconPath),
                IsCustomIcon = isCustom,
                DirectoryName = directory,
                VersioningMode = versioningMode,
                VersionUUID = version_uuid
            };

            Installation_Add(new_installation);
        }
        public void Installation_Edit(string uuid, string name, MCVersion version, string directory, string iconPath = null, bool isCustom = false)
        {
            if (CurrentProfile == null) return;
            if (CurrentInstallations == null) return;

            MCProfileExtensions.GetVersionParams(version, out VersioningMode versioningMode, out string version_uuid);
            BLInstallation new_installation = new BLInstallation()
            {
                DisplayName = name,
                IconPath = (iconPath == null ? @"Furnace.png" : iconPath),
                IsCustomIcon = isCustom,
                DirectoryName = directory,
                VersioningMode = versioningMode,
                VersionUUID = version_uuid
            };

            if (CurrentInstallations.Any(x => x.InstallationUUID == uuid))
            {
                int index = CurrentInstallations.FindIndex(x => x.InstallationUUID == uuid);
                CurrentInstallations[index] = new_installation;
                Save();
            }
        }
        public void Installation_Delete(BLInstallation installation)
        {
            if (CurrentProfile == null) return;
            if (CurrentInstallations == null) return;
            CurrentInstallations.Remove(installation);
            Save();
        }
        public void Installation_UpdateLP(BLInstallation installation)
        {
            if (installation == null) return;
            installation.LastPlayed = DateTime.Now;
            Save();
        }

        #endregion
    }
    public class MCProfile
    {
        public string Name { get; set; }
        public string ProfilePath { get; set; }
        public ObservableCollection<BLInstallation> Installations { get; set; } = new ObservableCollection<BLInstallation>();

        public MCProfile() { }
        public MCProfile(string name, string path)
        {
            Name = name;
            ProfilePath = path;
        }
    }
    public static class MCProfileExtensions
    {
        public static void GetVersionParams(MCVersion version, out VersioningMode versioningMode, out string version_uuid)
        {
            version_uuid = "latest_release";
            versioningMode = VersioningMode.LatestRelease;

            if (version != null)
            {
                if (version.UUID == "latest_beta") versioningMode = VersioningMode.LatestBeta;
                else if (version.UUID == "latest_release") versioningMode = VersioningMode.LatestRelease;
                else versioningMode = VersioningMode.None;

                version_uuid = version.UUID;
            }
        }
        public static void SetCurrentProfile(string profileUUID)
        {
            Properties.LauncherSettings.Default.CurrentProfile = profileUUID;
            Properties.LauncherSettings.Default.Save();
        }
        public static void SetCurrentInstallation(string installationUUID)
        {
            Properties.LauncherSettings.Default.CurrentInstallation = installationUUID;
            Properties.LauncherSettings.Default.Save();
        }
    }

}
