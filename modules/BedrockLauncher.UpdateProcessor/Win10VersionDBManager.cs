using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using Semver;
using static BedrockLauncher.UpdateProcessor.Win10VersionDBManager.Win10VersionTextDb;

namespace BedrockLauncher.UpdateProcessor
{
    public class Win10VersionDBManager
    {
        public class Win10VersionTextDb
        {
            public struct VersionInfo
            {
                public string uuid;
                public string version;
                public string serverId;

                public VersionInfo(string _uuid, string _fileName, string _serverId)
                {
                    uuid = _uuid;
                    version = _fileName;
                    serverId = _serverId;
                }
            };

            public List<VersionInfo> releaseList { get; private set; } = new List<VersionInfo>();
            public List<VersionInfo> betaList { get; private set; } = new List<VersionInfo>();

            public void AddVersion(List<Win10StoreNetwork.UpdateInfo> u, bool isBeta)
            {
                if (u == null || u.Count == 0) return;

                foreach (var v in u)
                {
                    var info = new Win10VersionTextDb.VersionInfo(v.updateId, v.packageMoniker, v.serverId);
                    if (isBeta) this.betaList.Add(info);
                    else this.releaseList.Add(info);
                }
            }
            public void Read(string filePath)
            {
                bool isBeta = false;
                using (var streamReader = File.OpenText(filePath))
                {
                    var lines = streamReader.ReadToEnd().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line == "Releases") isBeta = false;
                        else if (line == string.Empty) continue;
                        else if (line == "Beta") isBeta = true;
                        else
                        {
                            var line_split = line.Split(' ');
                            string serverId = line_split[2];
                            string fileName = line_split[1];
                            string uuid = line_split[0];
                            var version = new Win10VersionTextDb.VersionInfo(uuid, fileName, serverId);
                            if (isBeta) this.betaList.Add(version);
                            else this.releaseList.Add(version);
                        }
                    }
                }
            }
            public void Write(string filePath)
            {
                var lines = new List<string>();
                lines.Add("Releases");
                foreach (var v in this.releaseList) lines.Add(string.Format("{0} {1} {2}", v.uuid, v.version, (string.IsNullOrEmpty(v.serverId) ? "" : " " + v.serverId)));
                lines.Add("");
                lines.Add("Beta");
                foreach (var v in this.betaList) lines.Add(string.Format("{0} {1} {2}", v.uuid, v.version, (string.IsNullOrEmpty(v.serverId) ? "" : " " + v.serverId)));
                lines.Add("");
                File.WriteAllLines(filePath, lines);
            }
        }
        public class Win10VersionJsonDb
        {
            public class VersionComparator : IComparer<VersionInfoMin>
            {
                public int Compare(VersionInfoMin x, VersionInfoMin y)
                {
                    var a = new Version(x.version);
                    var b = new Version(y.version);
                    return a.CompareTo(b);
                }
            }

            public struct VersionInfoMin
            {
                public string version;
                public string uuid;
                public bool isBeta;

                public VersionInfoMin(string _version, string _uuid, bool _isBeta)
                {
                    uuid = _uuid;
                    version = _version;
                    isBeta = _isBeta;
                }
            };
            public List<VersionInfoMin> list { get; private set; } = new List<VersionInfoMin>();


            private void SortVersions()
            {
                list.Sort(new VersionComparator());
                list.Reverse();
            }

            public void AddVersion(List<Win10StoreNetwork.UpdateInfo> u, bool isBeta)
            {
                if (u == null || u.Count == 0) return;

                foreach (var v in u)
                {
                    string version = ConvertVersion(v.packageMoniker).ToString();
                    if (v.packageMoniker.Contains(".0_x64__") && !list.Exists(x => x.uuid == v.updateId || x.version == version))
                    {
                        var info = new VersionInfoMin(version, v.updateId, isBeta);
                        list.Add(info);
                    }
                }
            }
            public void ReadJson(string filePath)
            {
                using (var reader = File.OpenText(filePath))
                {
                    var data = reader.ReadToEnd();
                    PraseJson(data);
                }
            }
            public void WriteJson(string filePath)
            {
                SortVersions();
                var valuesList = JArray.FromObject(list).Select(x => x.Values().ToList()).ToList();
                string json = JsonConvert.SerializeObject(valuesList, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            public void PraseJson(string json)
            {
                JArray data = JArray.Parse(json);
                var list = data.ToList();
                list.Reverse();
                foreach (JArray o in list)
                {
                    string name = o[0].Value<string>();
                    string uuid = o[1].Value<string>();
                    bool isBeta = o[2].Value<int>() == 1;

                    this.list.Add(new VersionInfoMin(name, uuid, isBeta));
                }
                SortVersions();
            }
        }

        public static Version ConvertVersion(string ver)
        {
            Regex regex = new Regex(@"(Microsoft\.MinecraftUWP_([0-9]+)\.([0-9]+)\.([0-9]+)\..*__8wekyb3d8bbwe.*)");
            Match match;
            match = regex.Match(ver);
            if (match == null) return new Version(0, 0, 0, 0);

            string major_s = match.Groups[2].Value;
            string minor_s = match.Groups[3].Value;
            string patch_s = match.Groups[4].Value;

            if (int.TryParse(major_s, out int major_i) && int.TryParse(minor_s, out int minor_i) && int.TryParse(patch_s, out int patch_i))
            {
                int major;
                int minor;
                int patch;
                int revision;

                if (major_i == 0 && minor_i < 1000)
                {
                    major = major_i;
                    minor = minor_i / 10;
                    patch = minor_i % 10;
                    revision = patch_i;
                }
                else if (major_i == 0)
                {
                    major = major_i;
                    minor = minor_i / 100;
                    patch = minor_i % 100;
                    revision = patch_i;
                }
                else
                {
                    major = major_i;
                    minor = minor_i;
                    patch = patch_i / 100;
                    revision = patch_i % 100;
                }
                return new Version(major, minor, patch, revision);
            }
            else return new Version(0, 0, 0, 0);
        }

    }
}
