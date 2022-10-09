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
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Classes
{
    public class BLProfile
    {
        public string Name { get; set; }
        public string UUID { get; set; }
        public string ProfilePath { get; set; }
        public ObservableCollection<BLInstallation> Installations { get; set; } = new ObservableCollection<BLInstallation>();


        #region Runtime Values

        [JsonIgnore]
        public string ImagePath
        {
            get
            {
                string profile_directory = MainDataModel.Default.FilePaths.GetProfilePath(UUID);
                string profile_image = Path.Combine(profile_directory, Constants.PROFILE_CUSTOM_IMG_NAME);
                if (File.Exists(profile_image)) return profile_image;
                else return Constants.PROFILE_DEFAULT_IMG;
            }
        }

        #endregion

        public BLProfile() { }
        public BLProfile(string name, string path, string uuid)
        {
            Name = name;
            ProfilePath = path;
            UUID = uuid;
        }
    }

}
