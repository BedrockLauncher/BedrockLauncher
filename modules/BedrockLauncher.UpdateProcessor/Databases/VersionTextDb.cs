using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.UpdateProcessor.Extensions;
using BedrockLauncher.UpdateProcessor.Handlers;
using BedrockLauncher.UpdateProcessor.Interfaces;
using JemExtensions;

namespace BedrockLauncher.UpdateProcessor.Databases
{
    public class VersionTextDb : IVersionDb
    {

        public List<VersionInfoTxt> releaseList { get; private set; } = new List<VersionInfoTxt>();
        public List<VersionInfoTxt> betaList { get; private set; } = new List<VersionInfoTxt>();
        public List<VersionInfoTxt> previewList { get; private set; } = new List<VersionInfoTxt>();

        #region Read / Write

        public void Read(StreamReader streamReader)
        {
            VersionType type = VersionType.Release;
            var lines = streamReader.ReadToEnd().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line == "Releases") type = VersionType.Release;
                else if (line == "Preview") type = VersionType.Preview;
                else if (line == "Beta") type = VersionType.Beta;
                else if (line == string.Empty) continue;
                else
                {
                    var line_split = line.Split(' ');
                    string serverId = line_split.Length >= 3 ? line_split[2] : "null";
                    string fileName = line_split[1];
                    string uuid = line_split[0];
                    string arch = VersionDbExtensions.GetVersionArch(fileName, type);
                    var v = new VersionInfoTxt(uuid, fileName, serverId, arch, type);

                    if (!betaList.Exists(x => x.uuid == v.uuid) && !releaseList.Exists(x => x.uuid == v.uuid) && !previewList.Exists(x => x.uuid == v.uuid))
                    {
                        if (type == VersionType.Beta) this.betaList.Add(v);
                        else if (type == VersionType.Preview) this.previewList.Add(v);
                        else this.releaseList.Add(v);
                    }
                }
            }
        }
        public void Read(string data)
        {
            using (var streamReader = new StreamReader(data.ToStream())) Read(streamReader);
        }
        public void ReadFile(string filePath)
        {
            using (var streamReader = File.OpenText(filePath)) Read(streamReader);
        }
        public void Write(string filePath)
        {
            var lines = new List<string>();
            lines.Add("Releases");
            foreach (var v in this.releaseList) lines.Add(string.Format("{0} {1} {2}", v.uuid, v.packageMoniker, (string.IsNullOrEmpty(v.serverId) ? "" : " " + v.serverId)));
            lines.Add("");
            lines.Add("Beta");
            foreach (var v in this.betaList) lines.Add(string.Format("{0} {1} {2}", v.uuid, v.packageMoniker, (string.IsNullOrEmpty(v.serverId) ? "" : " " + v.serverId)));
            lines.Add("");
            lines.Add("Preview");
            foreach (var v in this.previewList) lines.Add(string.Format("{0} {1} {2}", v.uuid, v.packageMoniker, (string.IsNullOrEmpty(v.serverId) ? "" : " " + v.serverId)));
            lines.Add("");
            File.WriteAllLines(filePath, lines);
        }

        #endregion

        #region IVersionDb Implements

        public void AddVersion(List<UpdateInfo> list, VersionType type)
        {
            if (list == null || list.Count == 0) return;

            foreach (var update in list)
            {
                var v = new VersionInfoTxt(update.updateId, update.packageMoniker, update.serverId, VersionDbExtensions.GetVersionArch(update.packageMoniker, type), type);
                if (!betaList.Exists(x => x.uuid == v.uuid) && !releaseList.Exists(x => x.uuid == v.uuid))
                {
                    if (type == VersionType.Beta) this.betaList.Add(v);
                    else if (type == VersionType.Preview) this.previewList.Add(v);
                    else this.releaseList.Add(v);
                }
            }
        }

        public void Save(string filePath)
        {
            Write(filePath);
        }

        public List<IVersionInfo> GetVersions()
        {
            var releases = this.releaseList.Cast<IVersionInfo>().ToList();
            var betas = this.betaList.Cast<IVersionInfo>().ToList();
            var previews = this.previewList.Cast<IVersionInfo>().ToList();
            return releases.Concat(betas).Concat(previews).ToList();
        }

        public void PraseRaw(string data, Dictionary<Guid, string> architectures)
        {
            Read(data);
        }


        #endregion
    }
}
