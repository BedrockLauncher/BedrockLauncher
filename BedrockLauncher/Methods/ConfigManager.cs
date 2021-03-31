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
using Version = BedrockLauncher.Classes.Version;

namespace BedrockLauncher.Methods
{

    public class ConfigManager
    {

        private JsonSerializerSettings JsonSerializerSettings
        {
            get
            {
                var settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.MissingMemberHandling = MissingMemberHandling.Ignore;
                return settings;
            }
        }

        public void SaveConfig(ProfileList profileList)
        {
            string json = JsonConvert.SerializeObject(profileList, Formatting.Indented);
            File.WriteAllText("user_profile.json", json);
        }

        #region Profiles

        // creates empty profile
        public bool CreateProfile(string profile)
        {
            ProfileList profileList = ReadProfile();

            ProfileSettings profileSettings = new ProfileSettings();

            if (profileList.profiles.ContainsKey(profile)) return false;
            else
            {
                // default settings
                profileSettings.Name = profile;
                profileSettings.SkinPath = null;
                profileSettings.ProfilePath = profile;

                profileList.profiles.Add(profile, profileSettings);

                Properties.Settings.Default.CurrentProfile = profile;
                Properties.Settings.Default.Save();

                SaveConfig(profileList);
                return true;
            }
        }

        public void RemoveProfile(string profile)
        {
            ProfileList profileList = ReadProfile();

            if (profileList.profiles.ContainsKey(profile) && profileList.profiles.Count > 1)
            {
                profileList.profiles.Remove(profile);
                SaveConfig(profileList);

                var first_profile = profileList.profiles.FirstOrDefault();

                SwitchProfile(first_profile.Key);
            }

        }

        public void SwitchProfile(string profile)
        {
            ProfileList profileList = ReadProfile();

            if (profileList.profiles.ContainsKey(profile))
            {
                ((MainWindow)Application.Current.MainWindow).ProfileButton.ProfileName.Text = profile;
                Properties.Settings.Default.CurrentProfile = profile;
                Properties.Settings.Default.Save();
            }

        }

        public ProfileList ReadProfile()
        {
            string json;
            ProfileList profileList;
            if (File.Exists("user_profile.json"))
            {
                json = File.ReadAllText("user_profile.json");
                try
                {
                    profileList = JsonConvert.DeserializeObject<ProfileList>(json, JsonSerializerSettings);
                }
                catch
                {
                    profileList = new ProfileList();
                }

            }
            else
            {
                profileList = new ProfileList();
            }

            if (profileList.profiles == null) profileList.profiles = new Dictionary<string, ProfileSettings>();

            Console.WriteLine("Profile count: " + profileList.profiles.Count);
            foreach (ProfileSettings setting in profileList.profiles.Values)
            {
                Console.WriteLine("\nProfile found!: ");
                Console.WriteLine("Name: " + setting.Name);
                Console.WriteLine("Path: " + setting.ProfilePath);
                Console.WriteLine("Skin: " + setting.SkinPath);
                Console.WriteLine("Installations: " + setting.Installations.Count);
            }

            return profileList;
        }

        #endregion

        #region Installations

        public List<Installation> GetInstallations()
        {
            string currentProfile = Properties.Settings.Default.CurrentProfile;

            ProfileList profileList = ReadProfile();
            if (profileList.profiles.ContainsKey(currentProfile))
            {
                if (profileList.profiles[currentProfile].Installations == null) profileList.profiles[currentProfile].Installations = new List<Installation>();
                return profileList.profiles[currentProfile].Installations;
            }
            else return new List<Installation>();
        }

        public void CreateInstallation(string name, Version version)
        {
            string currentProfile = Properties.Settings.Default.CurrentProfile;

            ProfileList profileList = ReadProfile();
            if (profileList.profiles.ContainsKey(currentProfile))
            {
                Installation installation = new Installation();
                installation.DisplayName = name;
                installation.VersionUUID = version.UUID;
                //TODO: Make Customizable
                installation.IconPath = @"/BedrockLauncher;component/Resources/images/grass_block_icon.ico";

                if (profileList.profiles[currentProfile].Installations == null) profileList.profiles[currentProfile].Installations = new List<Installation>();

                profileList.profiles[currentProfile].Installations.Add(installation);
                SaveConfig(profileList);
            }
        }

        public void DeleteInstallation(Installation installation)
        {
            string currentProfile = Properties.Settings.Default.CurrentProfile;

            ProfileList profileList = ReadProfile();
            if (profileList.profiles.ContainsKey(currentProfile))
            {
                if (profileList.profiles[currentProfile].Installations == null) return;

                profileList.profiles[currentProfile].Installations.RemoveAll(x => x.DisplayName == installation.DisplayName);
                SaveConfig(profileList);
            }
        }

        #endregion
    }
}
