using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using BedrockLauncher.Classes;

namespace BedrockLauncher.Classes
{
    public class MCVersionList : ObservableCollection<MCVersion>
    {

        private readonly string _cacheFile;
        private readonly Interfaces.ICommonVersionCommands _commands;
        private readonly HttpClient _client = new HttpClient();

        public MCVersionList(string cacheFile, Interfaces.ICommonVersionCommands commands)
        {
            _cacheFile = cacheFile;
            _commands = commands;
        }
        public string GetLastRelease(JArray data)
        {
            Clear();

            foreach (JArray o in data.AsEnumerable().Reverse())
            {
                if (o[2].AsEnumerable().ToString() == "0")
                {
                    string lastrelease = o[0].ToString();
                    return lastrelease;
                    //Add(new WPFDataTypes.GetLastRelease(lastrelease));
                }
            } return null;
        }
        private void ParseList(JArray data)
        {
            Clear();
            // ([name, uuid, isBeta])[]
            foreach (JArray o in data.AsEnumerable().Reverse())
            {
                Add(new MCVersion(o[1].Value<string>(), o[0].Value<string>(), o[2].Value<int>() == 1, _commands));
            }
        }

        public async Task LoadFromCache()
        {
            try
            {
                using (var reader = File.OpenText(_cacheFile))
                {
                    var data = await reader.ReadToEndAsync();
                    ParseList(JArray.Parse(data));
                }
            }
            catch (FileNotFoundException)
            { // ignore
            }
        }

        public async Task DownloadList()
        {
            var resp = await _client.GetAsync("https://mrarm.io/r/w10-vdb");
            resp.EnsureSuccessStatusCode();
            var data = await resp.Content.ReadAsStringAsync();
            File.WriteAllText(_cacheFile, data);
            ParseList(JArray.Parse(data));
            //BedrockLauncher.GetLastRelease.LastRelease() = GetLastRelease(JArray.Parse(data));
        }

    }
}
