using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BedrockLauncher.Core;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;

namespace BedrockLauncher.Classes
{
    public class MCInstallation
    {
        public string DisplayName { get; set; }
        public string VersionUUID { get; set; }
        public string IconPath { get; set; }
        public bool IsCustomIcon { get; set; }
        public string DirectoryName { get; set; }
        public bool ReadOnly { get; set; }

        public bool UseLatestVersion { get; set; }
        public bool UseLatestBeta { get; set; }

        [JsonIgnore]
        public MCVersion Version
        {
            get
            {

                if (UseLatestVersion)
                {
                    var latest_beta = ConfigManager.Versions.ToList().FirstOrDefault(x => x.IsBeta == true);
                    var latest_release = ConfigManager.Versions.ToList().FirstOrDefault(x => x.IsBeta == false);

                    if (UseLatestBeta && latest_beta != null) return latest_beta;
                    else if (latest_release != null) return latest_release;
                }
                else if (ConfigManager.Versions.ToList().Exists(x => x.UUID == VersionUUID))
                {
                    return ConfigManager.Versions.ToList().Where(x => x.UUID == VersionUUID).FirstOrDefault();
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
        public string VersionName => Version?.Name ?? string.Empty;

    }
}
