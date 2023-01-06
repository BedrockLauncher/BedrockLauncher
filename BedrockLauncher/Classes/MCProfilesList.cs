using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using BedrockLauncher.Components;
using JemExtensions;
using Newtonsoft.Json;
using BedrockLauncher.Enums;
using PostSharp.Patterns.Model;

namespace BedrockLauncher.Classes
{

    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]    //224 Lines
    public class MCProfilesList
    {
        public int Version = 2;
        public Dictionary<string, MCProfile> profiles { get; set; } = new Dictionary<string, MCProfile>();

        #region Runtime Values
        [JsonIgnore]
        public string FilePath { get; private set; } = string.Empty;
        [JsonIgnore] 
        public string CurrentProfileUUID
        {
            get
            {
                Depends.On(Properties.LauncherSettings.Default.CurrentProfile);
                return Properties.LauncherSettings.Default.CurrentProfile;
            }
            set
            {
                Properties.LauncherSettings.Default.CurrentProfile = value;
                Properties.LauncherSettings.Default.Save();
            }
        }
        [JsonIgnore] 
        public string CurrentInstallationUUID
        {
            get
            {
                Depends.On(Properties.LauncherSettings.Default.CurrentInstallation);
                return Properties.LauncherSettings.Default.CurrentInstallation;
            }
            set
            {
                Properties.LauncherSettings.Default.CurrentInstallation = value;
                Properties.LauncherSettings.Default.Save();
            }
        }
        [JsonIgnore] 
        public MCProfile CurrentProfile
        {
            get
            {
                Depends.On(CurrentProfileUUID);
                if (profiles.ContainsKey(CurrentProfileUUID)) return profiles[CurrentProfileUUID];
                else return null;
            }
            set
            {
                if (profiles.ContainsKey(CurrentProfileUUID)) profiles[CurrentProfileUUID] = value;
            }
        }
        [JsonIgnore] 
        public BLInstallation CurrentInstallation
        {
            get
            {
                Depends.On(CurrentProfile, CurrentInstallations, CurrentInstallationUUID);
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
            }
        }
        [JsonIgnore] 
        public ObservableCollection<BLInstallation> CurrentInstallations
        {
            get
            {
                Depends.On(CurrentProfile);
                if (CurrentProfile == null) return null;
                else if (CurrentProfile.Installations == null) return null;
                else return CurrentProfile.Installations;
            }
            set
            {
                if (CurrentProfile == null) return;
                else if (CurrentProfile.Installations == null) return;
                else CurrentProfile.Installations = value;
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
                VersionUUID = Constants.LATEST_RELEASE_UUID,
                VersioningMode = VersioningMode.LatestRelease,
                IconPath = "Grass_Block.png",
                IsCustomIcon = false,
                ReadOnly = true,
                InstallationUUID = Constants.LATEST_RELEASE_UUID
            };
            BLInstallation latest_beta = new BLInstallation()
            {
                DisplayName = "Latest Beta",
                DirectoryName = "Latest Beta",
                VersionUUID = Constants.LATEST_BETA_UUID,
                VersioningMode = VersioningMode.LatestBeta,
                IconPath = "Crafting_Table.png",
                IsCustomIcon = false,
                ReadOnly = true,
                InstallationUUID = Constants.LATEST_BETA_UUID
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

            GetVersionParams(version, out VersioningMode versioningMode, out string version_uuid);
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

            GetVersionParams(version, out VersioningMode versioningMode, out string version_uuid);
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

        #region Extensions

        public static void GetVersionParams(MCVersion version, out VersioningMode versioningMode, out string version_uuid)
        {
            version_uuid = Constants.LATEST_RELEASE_UUID;
            versioningMode = VersioningMode.LatestRelease;

            if (version != null)
            {
                if (version.UUID == Constants.LATEST_BETA_UUID) versioningMode = VersioningMode.LatestBeta;
                else if (version.UUID == Constants.LATEST_RELEASE_UUID) versioningMode = VersioningMode.LatestRelease;
                else versioningMode = VersioningMode.None;

                version_uuid = version.UUID;
            }
        }

        #endregion
    }

}
