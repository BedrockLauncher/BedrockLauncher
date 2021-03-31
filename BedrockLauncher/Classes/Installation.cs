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

        public string VersionName
        {
            get
            {
                if (MainWindow.AvaliableVersions.ToList().Exists(x => x.UUID == VersionUUID))
                {
                    return MainWindow.AvaliableVersions.ToList().Where(x => x.UUID == VersionUUID).FirstOrDefault().Name;
                }
                return string.Empty;
            }
        }

        public bool IsBeta
        {
            get
            {
                if (MainWindow.AvaliableVersions.ToList().Exists(x => x.UUID == VersionUUID))
                {
                    return MainWindow.AvaliableVersions.ToList().Where(x => x.UUID == VersionUUID).FirstOrDefault().IsBeta;
                }
                return false;
            }
        }

        public string IconPath { get; set; }
    }
}
