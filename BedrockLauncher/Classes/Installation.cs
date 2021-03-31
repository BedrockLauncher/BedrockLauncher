using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BedrockLauncher.Classes
{
    public class Installation
    {
        public string DisplayName { get; set; }
        public string VersionUUID { get; set; }
        public bool UseLatestVersion { get; set; }
        public bool UseLatestBeta { get; set; }
        public string IconPath { get; set; }

        public Version Version
        {
            get
            {

                if (UseLatestVersion)
                {
                    var latest_beta = MainWindow.AvaliableVersions.ToList().First(x => x.IsBeta == true);
                    var latest_release = MainWindow.AvaliableVersions.ToList().First(x => x.IsBeta == false);

                    if (UseLatestBeta && latest_beta != null) return latest_beta;
                    else if (latest_release != null) return latest_release;
                }
                else if (MainWindow.AvaliableVersions.ToList().Exists(x => x.UUID == VersionUUID))
                {
                    return MainWindow.AvaliableVersions.ToList().Where(x => x.UUID == VersionUUID).FirstOrDefault();
                }
                return null;
            }
        }
        public bool IsBeta
        {
            get
            {
                if (UseLatestVersion && UseLatestBeta) return true;
                else return Version?.IsBeta ?? false;
            }
        }
        public string VersionName => Version?.Name ?? string.Empty;
    }
}
