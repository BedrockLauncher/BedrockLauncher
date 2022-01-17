using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BedrockLauncher.Classes;
using Newtonsoft.Json;
using BedrockLauncher.ViewModels;
using System.ComponentModel;
using System.IO;
using BedrockLauncher.Components;
using System.Diagnostics;
using BedrockLauncher.Enums;
using PostSharp.Patterns.Model;

namespace BedrockLauncher.Classes
{

    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]    //88 Lines
    public class BLInstallation
    {
        public string DisplayName { get; set; }
        public string VersionUUID { get; set; }
        public string IconPath { get; set; }
        public bool IsCustomIcon { get; set; } = false;
        public string DirectoryName { get; set; }
        public bool ReadOnly { get; set; }
        public VersioningMode VersioningMode { get; set; } = VersioningMode.None;
        public DateTime LastPlayed { get; set; }
        public string InstallationUUID { get; set; } = Guid.NewGuid().ToString();

        #region Runtime Values

        [JsonIgnore]
        public string IconPath_Full
        {
            get
            {
                Depends.On(IsCustomIcon, IconPath);
                if (IsCustomIcon) return Path.Combine(MainViewModel.Default.FilePaths.GetCacheFolderPath(), IconPath);
                else return @"/BedrockLauncher;component/Resources/images/installation_icons/" + IconPath;
            }
        }
        [JsonIgnore]
        public string DisplayName_Full
        {
            get
            {
                Depends.On(VersionUUID, DisplayName);
                if (VersionUUID == Constants.LATEST_RELEASE_UUID && ReadOnly) return Application.Current.FindResource("VersionEntries_LatestRelease").ToString();
                else if (VersionUUID == Constants.LATEST_BETA_UUID && ReadOnly) return Application.Current.FindResource("VersionEntries_LatestSnapshot").ToString();
                else if (string.IsNullOrWhiteSpace(DisplayName)) return Application.Current.FindResource("VersionEntries_UnnamedInstallation").ToString();
                else return DisplayName;
            }
        }
        [JsonIgnore]
        public string DirectoryName_Full
        {
            get
            {
                Depends.On(DirectoryName, DisplayName);
                if (string.IsNullOrEmpty(DirectoryName))
                {
                    char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
                    string result = new string(DisplayName.Where(ch => !invalidFileNameChars.Contains(ch)).ToArray());
                    return result;
                }
                else return DirectoryName;
            }
        }
        [JsonIgnore, SafeForDependencyAnalysis]
        public BLVersion Version
        {
            get
            {
                Depends.On(VersioningMode, VersionUUID);
                if (VersioningMode != VersioningMode.None)
                {
                    var latest_beta = MainViewModel.Default.Versions.ToList().FirstOrDefault(x => x.IsBeta == true && x.UUID != Constants.LATEST_BETA_UUID);
                    var latest_release = MainViewModel.Default.Versions.ToList().FirstOrDefault(x => x.IsBeta == false && x.UUID != Constants.LATEST_RELEASE_UUID) ;

                    if (VersioningMode == VersioningMode.LatestBeta && latest_beta != null) return BLVersion.Convert(latest_beta);
                    else if (VersioningMode == VersioningMode.LatestRelease && latest_release != null) return BLVersion.Convert(latest_release);
                    else return null;
                }
                else if (MainViewModel.Default.Versions.ToList().Exists(x => x.UUID == VersionUUID))
                {
                    return BLVersion.Convert(MainViewModel.Default.Versions.ToList().Where(x => x.UUID == VersionUUID).FirstOrDefault());
                }
                return null;
            }
        }
        [JsonIgnore]
        public bool IsBeta
        {
            get
            {
                Depends.On(VersionUUID, VersioningMode);
                if (VersioningMode == VersioningMode.LatestBeta) return true;
                else return Version?.IsBeta ?? false;
            }
        }
        [JsonIgnore]
        public string VersionName
        {
            get
            {
                Depends.On(VersionUUID, VersioningMode);
                string version = Version?.Name ?? "???";
                if (VersioningMode == VersioningMode.LatestBeta) return Application.Current.FindResource("VersionEntries_LatestSnapshot").ToString();
                else if (VersioningMode == VersioningMode.LatestRelease) return Application.Current.FindResource("VersionEntries_LatestRelease").ToString();
                else return version;
            }
        }
        [JsonIgnore]
        public string LastPlayedT
        {
            get
            {
                Depends.On(LastPlayed);
                return LastPlayed.ToString("s");
            }
        }

        #endregion

        public BLInstallation Clone(string newName = null)
        {
            var clone = (BLInstallation)this.MemberwiseClone();
            clone.InstallationUUID = Guid.NewGuid().ToString();
            if (!string.IsNullOrEmpty(newName)) clone.DisplayName = newName;
            return clone;
        }

        public void OpenDirectory()
        {
            string Directory = MainViewModel.Default.FilePaths.GetInstallationsFolderPath(MainViewModel.Default.Config.CurrentProfileUUID, DirectoryName_Full);
            if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);
            Process.Start("explorer.exe", Directory);
        }

    }

}
