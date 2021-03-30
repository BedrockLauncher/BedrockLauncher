using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using System.IO;
using BedrockLauncher.Classes;

namespace BedrockLauncher.Methods
{
    
    public class ConfigManager 
    {

        private ProfileList GetProfileList()
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

            if (profileList.profiles == null) profileList.profiles = new Dictionary<string, List<ProfileSettings>>();
            return profileList;
        }

        // creates empty profile
        public bool CreateProfile(string profile) 
        {
            ProfileList profileList = GetProfileList();

            List<ProfileSettings> ProfileSettingsList = new List<ProfileSettings>();
            ProfileSettings profileSettings = new ProfileSettings();

            if (profileList.profiles.ContainsKey(profile)) return false;
            else
            {
                // default settings
                profileSettings.Name = profile;
                profileSettings.SkinPath = null;
                profileSettings.ProfilePath = profile;
                ProfileSettingsList.Add(profileSettings);

                profileList.profiles.Add(profile, ProfileSettingsList);

                Properties.Settings.Default.CurrentProfile = profile;
                Properties.Settings.Default.Save();

                string json = JsonConvert.SerializeObject(profileList, Formatting.Indented);
                File.WriteAllText("user_profile.json", json);
                return true;
            }           
        }

        public void RemoveProfile(string profile)
        {
            ProfileList profileList = GetProfileList();

            if (profileList.profiles.ContainsKey(profile) && profileList.profiles.Count > 1)
            {
                profileList.profiles.Remove(profile);
                string json = JsonConvert.SerializeObject(profileList, Formatting.Indented);
                File.WriteAllText("user_profile.json", json);

                var first_profile = profileList.profiles.FirstOrDefault();

                SwitchProfile(first_profile.Key);
            }

        }

        public void SwitchProfile(string profile)
        {
            ProfileList profileList = GetProfileList();

            if (profileList.profiles.ContainsKey(profile))
            {
                ((MainWindow)Application.Current.MainWindow).ProfileButton.ProfileName.Text = profile;
                Properties.Settings.Default.CurrentProfile = profile;
                Properties.Settings.Default.Save();
            }

        }

        public ProfileList ReadProfile() 
        {
            string json = File.ReadAllText("user_profile.json");
            ProfileList profileList = JsonConvert.DeserializeObject<ProfileList>(json);

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

            return profileList;
        }
    }
}
