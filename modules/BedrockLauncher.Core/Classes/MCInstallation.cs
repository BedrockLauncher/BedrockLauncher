using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using BedrockLauncher.Core.Components;
using BedrockLauncher.Core.Interfaces;

namespace BedrockLauncher.Core.Classes
{
    public class MCInstallation : NotifyPropertyChangedBase
    {
        public string DisplayName { get; set; }
        public string VersionUUID { get; set; }
        public string IconPath { get; set; }
        public bool IsCustomIcon { get; set; } = false;
        public string DirectoryName { get; set; }
        public bool ReadOnly { get; set; }

        public bool UseLatestVersion { get; set; }
        public bool UseLatestBeta { get; set; }

        public DateTime LastPlayed { get; set; }
        public string InstallationUUID { get; set; } = Guid.NewGuid().ToString();
    }
}
