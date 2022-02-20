using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using BedrockLauncher.Components;
using JemExtensions;
using Newtonsoft.Json;
using BedrockLauncher.Enums;
using PostSharp.Patterns.Model;

namespace BedrockLauncher.Classes
{
    public class MCProfile
    {
        public string Name { get; set; }
        public string ProfilePath { get; set; }
        public ObservableCollection<BLInstallation> Installations { get; set; } = new ObservableCollection<BLInstallation>();

        public MCProfile() { }
        public MCProfile(string name, string path)
        {
            Name = name;
            ProfilePath = path;
        }
    }

}
