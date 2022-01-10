using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BedrockLauncher.Classes;
using BedrockLauncher.ViewModels;
using Newtonsoft.Json;
using PostSharp.Patterns.Model;

namespace BedrockLauncher.Classes
{

    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.ExcludeExplicitProperties)]    //84 Lines
    public class BLVersion : MCVersion
    {
        public BLVersion(string uuid, string name, bool isBeta) : base(uuid, name, isBeta) { }

        public static BLVersion Convert(MCVersion obj)
        {
            return JsonConvert.DeserializeObject<BLVersion>(JsonConvert.SerializeObject(obj));
        }

        public string GameDirectory
        {
            get
            {
                Depends.On(UUID);
                return Path.GetFullPath(MainViewModel.Default.FilePaths.CurrentLocation + "\\versions\\Minecraft-" + UUID);
            }
        }
        public bool IsInstalled
        {
            get
            {
                Depends.On(GameDirectory);
                return Directory.Exists(GameDirectory);
            }
        }
        public string DisplayName
        {
            get
            {
                Depends.On(IsBeta, Name);
                return Name + (IsBeta ? " (Beta)" : "");
            }
        }
        public string InstallationSize
        {
            get
            {
                Depends.On(RequireSizeRecalculation);
                Task.Run(GetInstallSize);
                return _StoredInstallationSize;
            }
        }

        private string _StoredInstallationSize = "N/A";
        private bool RequireSizeRecalculation { get; set; } = true;

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
        }

        public string IconPath
        {
            get
            {
                Depends.On(IsBeta);
                return IsBeta ? @"/BedrockLauncher;component/Resources/images/icons/ico/crafting_table_block_icon.ico" : @"/BedrockLauncher;component/Resources/images/icons/ico/grass_block_icon.ico";
            }
        }

        public string DisplayInstallStatus
        {
            get
            {
                Depends.On(IsInstalled);
                return IsInstalled ? "Installed" : "Not installed";
            }
        }

        public string ManifestPath
        {
            get
            {
                Depends.On(GameDirectory);
                return Path.Combine(GameDirectory, "AppxManifest.xml");
            }
        }

        public string GetPackageNameFromMainifest()
        {
            try
            {
                string manifestXml = File.ReadAllText(ManifestPath);
                XDocument XMLDoc = XDocument.Parse(manifestXml);
                var Descendants = XMLDoc.Descendants();
                XElement Identity = Descendants.Where(x => x.Name.LocalName == "Identity").FirstOrDefault();
                string Name = Identity.Attribute("Name").Value;
                string Version = Identity.Attribute("Version").Value;
                string ProcessorArchitecture = Identity.Attribute("ProcessorArchitecture").Value;
                return String.Join("_", Name, Version, ProcessorArchitecture);
            }
            catch
            {
                return "???";
            }
        }

        public void OpenDirectory()
        {
            string Directory = Path.GetFullPath(GameDirectory);
            if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);
            Process.Start("explorer.exe", Directory);
        }



        public void UpdateInstallStatus()
        {
            RequireSizeRecalculation = true;
        }
    }
}
