﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BedrockLauncher.UpdateProcessor.Classes;
using Semver;
using System.Runtime.InteropServices;
using BedrockLauncher.UpdateProcessor.Extensions;
using BedrockLauncher.UpdateProcessor.Interfaces;
using BedrockLauncher.UpdateProcessor.Handlers;

namespace BedrockLauncher.UpdateProcessor.Databases
{
    public class VersionJsonDb : IVersionDb
    {
        public List<VersionInfoJson> list { get; private set; } = new List<VersionInfoJson>();


        private void SortVersions()
        {
            list.Sort();
            list.Reverse();
        }


        #region Read / Write


        public void ReadJson(string filePath, Dictionary<Guid, string> architectures = null)
        {
            using (var reader = File.OpenText(filePath))
            {
                var data = reader.ReadToEnd();
                PraseJson(data, architectures);
            }
        }
        public void WriteJson(string filePath)
        {
            SortVersions();
            var valuesList = JArray.FromObject(list).Select(x => x.Values().ToList()).ToList();
            string json = JsonConvert.SerializeObject(valuesList, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        public void PraseJson(string json, Dictionary<Guid, string> architectures)
        {
            JArray data = JArray.Parse(json);
            var lista = data.ToList();
            lista.Reverse();
            foreach (JArray o in lista)
            {
                string name = o[0].Value<string>();
                string uuid = o[1].Value<string>();
                bool isBeta = o[2].Value<int>() == 1;
                string arch = o.Count() >= 4 ? o[3].Value<string>() : VersionDbExtensions.FallbackArch;
                var v = new VersionInfoJson(name, uuid, isBeta, arch);

                if (arch == VersionDbExtensions.FallbackArch && architectures != null)
                {
                    if (architectures.ContainsKey(v.uuid))
                        v = new VersionInfoJson(name, uuid, isBeta, architectures[v.uuid]);
                }




                if (!list.Exists(x => x.uuid == v.uuid)) this.list.Add(v);
            }
            SortVersions();
        }

        #endregion

        #region IVersionDb Implements

        public void AddVersion(List<UpdateInfo> u, bool isBeta)
        {
            if (u == null || u.Count == 0) return;

            foreach (var v in u)
            {
                string version = MinecraftVersion.ConvertVersion(v.packageMoniker).ToString();
                string arch = VersionDbExtensions.GetVersionArch(v.packageMoniker);
                var info = new VersionInfoJson(version, v.updateId, isBeta, arch);

                if (!list.Exists(x => x.uuid == info.uuid)) list.Add(info);
            }


        }

        public void Save(string filePath)
        {

        }

        public List<IVersionInfo> GetVersions()
        {
            return this.list.Cast<IVersionInfo>().ToList();
        }

        public void PraseRaw(string data, Dictionary<Guid, string> architectures)
        {
            PraseJson(data, architectures);
        }


        #endregion

    }
}
