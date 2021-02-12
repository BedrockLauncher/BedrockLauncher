using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace BedrockLauncher
{
    class Config
    {
        public List<ProfileNames> ProfileList { get; set; }
    }
    class ProfileNames
    {
        public string ProfileName { get; set; }
    }
    class ConfigManager
    {
        public bool FindKey(string key)
        {
            return true;
        }
        public bool RemoveKey()
        {
            return true;
        }
        public bool AddKey()
        {
            return true;
        }
        public bool CreateProfile(string profile)
        {
            // сохранение данных
            using (FileStream fs = new FileStream("user.json", FileMode.OpenOrCreate))
            {
                Config config = new Config();
                config.ProfileList = new List<ProfileNames> { (new ProfileNames { ProfileName = profile }) };
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                //write string to file
                System.IO.File.WriteAllText("config.json", json);
            }

            return true;
        }
    }
}
