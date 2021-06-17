using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BedrockLauncher.Core.Classes;
using Newtonsoft.Json;
using BedrockLauncher.ViewModels;
using System.ComponentModel;
using System.IO;

namespace BedrockLauncher.Classes
{
    public class BLInstallation : MCInstallation
    {

        public static BLInstallation Convert(MCInstallation obj)
        {
            return JsonConvert.DeserializeObject<BLInstallation>(JsonConvert.SerializeObject(obj));
        }

        [JsonIgnore]
        public string IconPath_Full
        {
            get
            {
                if (IsCustomIcon) return Path.Combine(LauncherModel.Default.FilepathManager.GetCacheFolderPath(), IconPath);
                else return @"/BedrockLauncher;component/Resources/images/installation_icons/" + IconPath;
            }
        }
        [JsonIgnore]
        public string DisplayName_Full
        {
            get
            {
                if (VersionUUID == "latest_release" && ReadOnly) return Application.Current.FindResource("VersionEntries_LatestRelease").ToString();
                else if (VersionUUID == "latest_beta" && ReadOnly) return Application.Current.FindResource("VersionEntries_LatestSnapshot").ToString();
                else if (string.IsNullOrWhiteSpace(DisplayName)) return Application.Current.FindResource("VersionEntries_UnnamedInstallation").ToString();
                else return DisplayName;
            }
        }
        [JsonIgnore]
        public string DirectoryName_Full
        {
            get
            {
                if (string.IsNullOrEmpty(DirectoryName))
                {
                    char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
                    string result = new string(DisplayName.Where(ch => !invalidFileNameChars.Contains(ch)).ToArray());
                    return result;
                }
                else return DirectoryName;
            }
        }


        [JsonIgnore]
        public BLVersion Version
        {
            get
            {

                if (UseLatestVersion)
                {
                    var latest_beta = LauncherModel.Default.ConfigManager.Versions.ToList().FirstOrDefault(x => x.IsBeta == true);
                    var latest_release = LauncherModel.Default.ConfigManager.Versions.ToList().FirstOrDefault(x => x.IsBeta == false);

                    if (UseLatestBeta && latest_beta != null) return BLVersion.Convert(latest_beta);
                    else if (latest_release != null) return BLVersion.Convert(latest_release);
                }
                else if (LauncherModel.Default.ConfigManager.Versions.ToList().Exists(x => x.UUID == VersionUUID))
                {
                    return BLVersion.Convert(LauncherModel.Default.ConfigManager.Versions.ToList().Where(x => x.UUID == VersionUUID).FirstOrDefault());
                }
                return null;
            }
        }

        [JsonIgnore]
        public bool IsBeta
        {
            get
            {
                if (UseLatestVersion && UseLatestBeta) return true;
                else return Version?.IsBeta ?? false;
            }
        }

        [JsonIgnore]
        public string VersionName
        {
            get
            {
                string version = Version?.Name ?? "???";
                if (UseLatestBeta) return Application.Current.FindResource("VersionEntries_LatestSnapshot").ToString();
                else if (UseLatestVersion) return Application.Current.FindResource("VersionEntries_LatestRelease").ToString();
                else return version;
            }
        }


        public void Update()
        {
            OnPropertyChanged(nameof(DisplayName_Full));
            OnPropertyChanged(nameof(DirectoryName_Full));
            OnPropertyChanged(nameof(IconPath_Full));
            OnPropertyChanged(nameof(VersionName));
        }
    }
}
