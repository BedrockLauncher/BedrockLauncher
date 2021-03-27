using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms.Design;
using Newtonsoft.Json;

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Data;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Core;
using Windows.Management.Deployment;
using Windows.System;

using BedrockLauncher.Interfaces;

namespace BedrockLauncher.Classes
{
    public class Version : NotifyPropertyChangedBase
    {
        public Version(string uuid, string name, bool isBeta, ICommonVersionCommands commands)
        {
            this.UUID = uuid;
            this.Name = name;
            this.IsBeta = isBeta;
            this.DownloadCommand = commands.DownloadCommand;
            this.LaunchCommand = commands.LaunchCommand;
            this.RemoveCommand = commands.RemoveCommand;
        }

        public string UUID { get; set; }
        public string Name { get; set; }
        public bool IsBeta { get; set; }

        public string GameDirectory => "versions/Minecraft-" + Name;

        public bool IsInstalled => Directory.Exists(GameDirectory);

        public string DisplayName
        {
            get
            {
                return (IsBeta ? "(Beta) " : "") + Name;
            }
        }

        public string IconPath
        {
            get
            {
                return IsBeta ? @"/BedrockLauncher;component/Resources/images/crafting_table_block_icon.ico" : @"/BedrockLauncher;component/Resources/images/grass_block_icon.ico";
            }
        }

        public string DisplayInstallStatus
        {
            get
            {
                return IsInstalled ? "Installed" : "Not installed";
            }
        }

        public ICommand LaunchCommand { get; set; }
        public ICommand DownloadCommand { get; set; }
        public ICommand RemoveCommand { get; set; }

        private VersionStateChangeInfo _stateChangeInfo;
        public VersionStateChangeInfo StateChangeInfo
        {
            get { return _stateChangeInfo; }
            set { _stateChangeInfo = value; OnPropertyChanged("StateChangeInfo"); OnPropertyChanged("IsStateChanging"); }
        }

        public bool IsStateChanging => StateChangeInfo != null;

        public void UpdateInstallStatus()
        {
            OnPropertyChanged("IsInstalled");
        }

    }
}
