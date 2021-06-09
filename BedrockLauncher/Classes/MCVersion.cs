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
using System.Linq.Expressions;

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Data;

using BL_Core.Components;
using System.Text.RegularExpressions;

namespace BedrockLauncher.Classes
{
    public class MCVersion : NotifyPropertyChangedBase
    {
        public MCVersion(string uuid, string name, bool isBeta)
        {
            this.UUID = uuid.ToLower();
            this.Name = name;
            this.IsBeta = isBeta;
        }

        public string UUID { get; set; }
        public string Name { get; set; }
        public bool IsBeta { get; set; }

        public string GameDirectory
        {
            get
            {
                return Methods.FilepathManager.CurrentLocation + "\\versions\\Minecraft-" + UUID;
            }
        }

        public string ExePath
        {
            get
            {
                return GameDirectory + "\\Minecraft.Windows.exe";
            }
        }

        public bool IsInstalled
        {
            get
            {
                return Directory.Exists(GameDirectory);
            }
        }

        public string DisplayName
        {
            get
            {
                return Name + (IsBeta ? " (Beta)" : "");
            }
        }

        public string InstallationSize
        {
            get
            {
                GetInstallSize();
                return _StoredInstallationSize;
            }
        }

        private string _StoredInstallationSize = "N/A";
        private bool RequireSizeRecalculation = true;

        private async void GetInstallSize()
        {
            if (!RequireSizeRecalculation)
            {
                return;
            }

            if (Directory.Exists(Path.GetFullPath(GameDirectory)))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(Path.GetFullPath(GameDirectory));
                long dirSize = await Task.Run(() => dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length));

                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                double len = dirSize;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
                // show a single decimal place, and no space.
                _StoredInstallationSize = String.Format("{0:0.##} {1}", len, sizes[order]);
                RequireSizeRecalculation = false;
            }
            else
            {
                _StoredInstallationSize = "N/A";
                RequireSizeRecalculation = false;
            }
            OnPropertyChanged(nameof(InstallationSize));

        }

        public string IconPath
        {
            get
            {
                return IsBeta ? @"/BedrockLauncher;component/Resources/images/icons/ico/crafting_table_block_icon.ico" : @"/BedrockLauncher;component/Resources/images/icons/ico/grass_block_icon.ico";
            }
        }

        public string DisplayInstallStatus
        {
            get
            {
                return IsInstalled ? "Installed" : "Not installed";
            }
        }

        public void UpdateInstallStatus()
        {
            OnPropertyChanged(nameof(IsInstalled));
            RequireSizeRecalculation = true;
        }
    }
}
