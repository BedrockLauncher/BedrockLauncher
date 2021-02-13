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
    }
    public class ConfigManager 
    {
        // creates empty profile
        public void CreateProfile(string profile) 
        {
            ProfileList profileList = new ProfileList();
            profileList.profiles = new Dictionary<string, List<ProfileSettings>>();

            List<ProfileSettings> ProfileSettingsList = new List<ProfileSettings>();
            ProfileSettings profileSettings = new ProfileSettings();
            
            // default settings
            profileSettings.Name = profile;
            profileSettings.SkinPath = null;
            ProfileSettingsList.Add(profileSettings);

            profileList.profiles.Add(profile, ProfileSettingsList);

            //names.name = new Dictionary<string, string>();
            //names.name.Add(profile, "value1");
            //product.profiles.Add(names);
            string json = JsonConvert.SerializeObject(profileList, Formatting.Indented);
            File.WriteAllText("user_profile.json", json);
            //Console.WriteLine(json);
        }
        public void ReadProfile() 
        {
            string json = File.ReadAllText("user_profile.json");
            ProfileList profileList = JsonConvert.DeserializeObject<ProfileList>(json);
            ProfileSettings profileSettings = JsonConvert.DeserializeObject<ProfileSettings>(json);

            Console.WriteLine("Profile count: " + profileList.profiles.Count);
            //foreach (string key in profileList.profiles.Keys)
            //{
            //    Console.WriteLine("Profile found: " + key);
            //}
            foreach (List<ProfileSettings> settings in profileList.profiles.Values)
            {
                foreach (ProfileSettings setting in settings)
                {
                    Console.WriteLine("\nProfile found!: ");
                    Console.WriteLine("Profile name: " + setting.Name);
                    Console.WriteLine("Profile skin: " + setting.SkinPath);
                }
            }
            //Console.WriteLine("Profile found: " + myDeserializedClass.profiles[0].Name);


        }
    }
}
