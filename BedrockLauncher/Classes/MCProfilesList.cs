using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using JemExtensions;
using Newtonsoft.Json;
using BedrockLauncher.Enums;
using PostSharp.Patterns.Model;
using System.ComponentModel;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Classes
{

    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]    //224 Lines
    public class MCProfilesList : JemExtensions.WPF.NotifyPropertyChangedBase
    {
        public int Version = 2;



        public Dictionary<string, MCProfile> profiles { get; set; } = new Dictionary<string, MCProfile>();

        #region Runtime Values
        [JsonIgnore]
        public string FilePath { get; private set; } = string.Empty;

        [JsonIgnore]
        public string CurrentInstallationUUID
        {
            get
            {
                Depends.On(Properties.LauncherSettings.Default.CurrentInstallationUUID);
                return Properties.LauncherSettings.Default.CurrentInstallationUUID;
            }
            set
            {
                Properties.LauncherSettings.Default.CurrentInstallationUUID = value;
                Properties.LauncherSettings.Default.Save();
            }
        }
        [JsonIgnore] 
        public MCProfile CurrentProfile
        {
            get
            {
                Depends.On(Properties.LauncherSettings.Default.CurrentProfileUUID);
                if (profiles.ContainsKey(Properties.LauncherSettings.Default.CurrentProfileUUID)) return profiles[Properties.LauncherSettings.Default.CurrentProfileUUID];
                else return null;
            }
            set
            {
                if (profiles.ContainsKey(Properties.LauncherSettings.Default.CurrentProfileUUID)) profiles[Properties.LauncherSettings.Default.CurrentProfileUUID] = value;
            }
        }

        [JsonIgnore]
        public string CurrentProfileImagePath
        {
            get
            {
                Depends.On(Properties.LauncherSettings.Default.CurrentProfileUUID);
                if (profiles.ContainsKey(Properties.LauncherSettings.Default.CurrentProfileUUID)) return profiles[Properties.LauncherSettings.Default.CurrentProfileUUID].ImagePath;
                return string.Empty;
            }
        }
        [JsonIgnore] 
        public BLInstallation CurrentInstallation
        {
            get
            {
                Depends.On(CurrentInstallationUUID, CurrentInstallations);
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
            foreach(var profile in profiles) profile.Value.UUID = profile.Key;

            if (profiles.ContainsKey(Properties.LauncherSettings.Default.CurrentProfileUUID)) Properties.LauncherSettings.Default.CurrentProfileUUID = lastProfile;
            else if (profiles.Count != 0) Properties.LauncherSettings.Default.CurrentProfileUUID = profiles.First().Key;

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
                DisplayName = "Latest Release", //TODO: Localize Display Names
                DirectoryName = "Latest Release",  //TODO: Localize Directory Names?
                VersionUUID = Constants.LATEST_RELEASE_UUID,
                VersioningMode = VersioningMode.LatestRelease,
                IconPath = Constants.INSTALLATIONS_LATEST_RELEASE_ICONPATH,
                IsCustomIcon = false,
                ReadOnly = true,
                InstallationUUID = Constants.LATEST_RELEASE_UUID
            };
            BLInstallation latest_beta = new BLInstallation()
            {
                DisplayName = "Latest Beta",  //TODO: Localize Display Names
                DirectoryName = "Latest Beta",  //TODO: Localize Directory Names?
                VersionUUID = Constants.LATEST_BETA_UUID,
                VersioningMode = VersioningMode.LatestBeta,
                IconPath = Constants.INSTALLATIONS_LATEST_PREVIEW_ICONPATH,
                IsCustomIcon = false,
                ReadOnly = true,
                InstallationUUID = Constants.LATEST_BETA_UUID
            };
            BLInstallation latest_preview = new BLInstallation()
            {
                DisplayName = "Latest Preview",  //TODO: Localize Display Names
                DirectoryName = "Latest Preview",  //TODO: Localize Directory Names?
                VersionUUID = Constants.LATEST_PREVIEW_UUID,
                VersioningMode = VersioningMode.LatestPreview,
                IconPath = Constants.INSTALLATIONS_LATEST_PREVIEW_ICONPATH, 
                IsCustomIcon = false,
                ReadOnly = true,
                InstallationUUID = Constants.LATEST_PREVIEW_UUID
            };


            foreach (var profile in profiles.Values)
            {
                if (!profile.Installations.Any(x => x.InstallationUUID == latest_release.InstallationUUID && x.ReadOnly))
                    Installation_Add(latest_release);
                if (!profile.Installations.Any(x => x.InstallationUUID == latest_beta.InstallationUUID && x.ReadOnly))
                    Installation_Add(latest_beta);
                if (!profile.Installations.Any(x => x.InstallationUUID == latest_preview.InstallationUUID && x.ReadOnly))
                    Installation_Add(latest_preview);

                foreach (var installation in profile.Installations.Where(x => x.VersionUUID == latest_release.VersionUUID))
                    installation.VersioningMode = VersioningMode.LatestRelease;

                foreach (var installation in profile.Installations.Where(x => x.VersionUUID == latest_beta.VersionUUID))
                    installation.VersioningMode = VersioningMode.LatestBeta;

                foreach (var installation in profile.Installations.Where(x => x.VersionUUID == latest_preview.VersionUUID))
                    installation.VersioningMode = VersioningMode.LatestPreview;
            }

            Save();
        }

        private void GenerateProfileImage(string img, string uuid)
        {
            string path = MainViewModel.Default.FilePaths.GetProfilePath(uuid);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            string new_img = Path.Combine(path, Constants.PROFILE_CUSTOM_IMG_NAME);
            File.Copy(img, new_img, true);
        }

        #endregion

        #region Management Methods

        public bool Profile_Add(string name, string uuid, string directory, string img)
        {
            var real_directory = ValidatePathName(directory);
            MCProfile profileSettings = new MCProfile(name, real_directory, uuid);
            

            if (profiles.ContainsKey(uuid)) return false;
            else
            {
                profiles.Add(uuid, profileSettings);
                GenerateProfileImage(img, uuid);

                Profile_Switch(uuid);
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
        public bool Profile_Edit(string name, string uuid, string directory, string img)
        {
            var real_directory = ValidatePathName(directory);

            if (!profiles.ContainsKey(uuid)) return false;
            else
            {
                profiles[uuid].Name = name;
                profiles[uuid].ProfilePath = name;
                GenerateProfileImage(img, uuid);

                Profile_Switch(uuid);
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
        public void Profile_Remove(string profileUUID)
        {
            if (profiles.ContainsKey(profileUUID) && profiles.Count > 1)
            {
                profiles.Remove(profileUUID);
                Save();
                Profile_Switch(profiles.FirstOrDefault().Key);
            }

        }
        public void Profile_Switch(string profileUUID)
        {
            if (profiles.ContainsKey(profileUUID))
            {
                Properties.LauncherSettings.Default.CurrentProfileUUID = profileUUID;      
                Properties.LauncherSettings.Default.Save();

                OnPropertyChanged(nameof(CurrentProfile));
                OnPropertyChanged(nameof(CurrentInstallations));
                OnPropertyChanged(nameof(CurrentInstallation));
                OnPropertyChanged(nameof(CurrentProfileImagePath));
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

        public void Installation_Move(BLInstallation installation, bool moveUp)
        {
            if (CurrentProfile == null) return;
            if (CurrentInstallations == null) return;
            if (CurrentInstallations.Any(x => x.InstallationUUID == installation.InstallationUUID))
            {
                int oldIndex = CurrentInstallations.FindIndex(x => x.InstallationUUID == installation.InstallationUUID);
                int count = CurrentInstallations.Count() - 1;
                int newIndex = oldIndex + (moveUp ? -1 : 1);
                if (newIndex >= 0 && newIndex <= count) CurrentInstallations.Move(oldIndex, newIndex);
                Save();
            }
        }

        public void Installation_MoveDown(BLInstallation installation)
        {
            Installation_Move(installation, false);
        }

        public void Installation_MoveUp(BLInstallation installation)
        {
            Installation_Move(installation, true);
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
                IconPath = (iconPath == null ? Constants.INSTALLATIONS_FALLBACK_ICONPATH : iconPath),
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
                IconPath = (iconPath == null ? Constants.INSTALLATIONS_FALLBACK_ICONPATH : iconPath),
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
        public void Installation_Delete(BLInstallation installation, bool deleteData = true)
        {
            if (CurrentProfile == null) return;
            if (CurrentInstallations == null) return;
            if (deleteData)
            {
                try { installation.DeleteUserData(); }
                catch (Exception ex) { _ = ErrorScreenShow.exceptionmsg(ex); }
            }
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

        public static void GetVersionParams(MCVersion version, out VersioningMode versioningMode, out string version_id)
        {
            version_id = Constants.LATEST_RELEASE_UUID;
            versioningMode = VersioningMode.LatestRelease;

            if (version != null)
            {
                if (version.UUID == Constants.LATEST_BETA_UUID) versioningMode = VersioningMode.LatestBeta;
                else if (version.UUID == Constants.LATEST_RELEASE_UUID) versioningMode = VersioningMode.LatestRelease;
                else if (version.UUID == Constants.LATEST_PREVIEW_UUID) versioningMode = VersioningMode.LatestPreview;
                else versioningMode = VersioningMode.None;

                version_id = version.UUID;
            }
        }

        #endregion
    }

}
