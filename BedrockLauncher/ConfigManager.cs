using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace BedrockLauncher
{
    class ProfileList
    {
        public Dictionary<string, List<ProfileSettings>> profiles { get; set; }
    }
    class ProfileSettings
    {
        public string Name { get; set; }
        public string SkinPath { get; set; }
        public string ProfilePath { get; set; }
        
    }
    public class ConfigManager 
    {
        // creates empty profile
        public void CreateProfile(string profile) 
        {
            string json;
            ProfileList profileList;
            if (File.Exists("user_profile.json"))
            {
                json = File.ReadAllText("user_profile.json");
                profileList = JsonConvert.DeserializeObject<ProfileList>(json);
            }
            else
            {
                profileList = new ProfileList();
            }

            //ProfileList profileList = new ProfileList();
            profileList.profiles = new Dictionary<string, List<ProfileSettings>>();

            List<ProfileSettings> ProfileSettingsList = new List<ProfileSettings>();
            ProfileSettings profileSettings = new ProfileSettings();
            
            // default settings
            profileSettings.Name = profile;
            profileSettings.SkinPath = null;
            profileSettings.ProfilePath = profile;
            ProfileSettingsList.Add(profileSettings);

            profileList.profiles.Add(profile, ProfileSettingsList);

            Properties.Settings.Default.CurrentProfile = profile;
            Properties.Settings.Default.Save();

            json = JsonConvert.SerializeObject(profileList, Formatting.Indented);
            File.WriteAllText("user_profile.json", json);
            //Console.WriteLine(json);
        }
        public void ReadProfile() 
        {
            string json = File.ReadAllText("user_profile.json");
            ProfileList profileList = JsonConvert.DeserializeObject<ProfileList>(json);
            //ProfileSettings profileSettings = JsonConvert.DeserializeObject<ProfileSettings>(json);

            Console.WriteLine("Profile count: " + profileList.profiles.Count);
            foreach (List<ProfileSettings> settings in profileList.profiles.Values)
            {
                foreach (ProfileSettings setting in settings)
                {
                    Console.WriteLine("\nProfile found!: ");
                    Console.WriteLine("Name: " + setting.Name);
                    Console.WriteLine("Path: " + setting.ProfilePath);
                    Console.WriteLine("Skin: " + setting.SkinPath);
                }
            }


        }
    }
}
