using System;
using System.Windows;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Collections.Generic;
using Newtonsoft.Json;
using BedrockLauncher;
using System.Runtime.InteropServices;
using BedrockLauncher.ViewModels;
using System.Linq;
using System.Threading;
using BedrockLauncher.Core;
using System.Net.Http;

namespace BedrockLauncher.Handlers
{
    public class UpdateHandler
    {
        #region Definitions


        public List<GithubReleaseInfo> Notes
        {
            get
            {
                var list = new List<GithubReleaseInfo>();
                list.AddRange(ReleaseNotes);
                list.AddRange(PrereleaseNotes);
                list.AddRange(BetaNotes);
                list.Sort((x, y) => y.published_at.CompareTo(x.published_at));
                return list;
            }
        }
        private List<GithubReleaseInfo> ReleaseNotes { get; set; } = new List<GithubReleaseInfo>();
        private List<GithubReleaseInfo> PrereleaseNotes { get; set; } = new List<GithubReleaseInfo>();
        private List<GithubReleaseInfo> BetaNotes { get; set; } = new List<GithubReleaseInfo>();



        #endregion

        #region Accessors

        public bool isLatestBeta()
        {
            var list = Notes;
            if (list.Count == 0) return false;
            if (Properties.LauncherSettings.Default.UseBetaBuilds) return list[0].prerelease;
            else return false;
        }

        public string GetLatestTag()
        {
            var list = Notes;
            if (list.Count == 0) return string.Empty;

            if (Properties.LauncherSettings.Default.UseBetaBuilds) return list[0].tag_name;
            else if (list.Exists(x => !x.prerelease)) return list.First(x => !x.prerelease).tag_name;
            else return string.Empty;
        }

        public string GetLatestTagBody()
        {
            var list = Notes;
            if (list.Count == 0) return string.Empty;

            if (Properties.LauncherSettings.Default.UseBetaBuilds) return list[0].body;
            else if (list.Exists(x => !x.prerelease)) return list.First(x => x.prerelease == false).body;
            else return string.Empty;
        }

        #endregion

        #region Init

        public UpdateHandler()
        {

        }

        #endregion

        #region Update Checking

        public void CheckForUpdates()
        {
            Task.Run(async () =>
            {
                await CheckForUpdatesAsync();
            });

        }
        public async Task<bool> CheckForUpdatesAsync(bool onLoad = false)
        {
            if (onLoad && Debugger.IsAttached && !Constants.Debugging.CheckForUpdatesOnLoad) return false;
            System.Diagnostics.Trace.WriteLine("Checking for updates");
            try
            {
                ReleaseNotes.Clear();
                BetaNotes.Clear();
                PrereleaseNotes.Clear();

                await Beta_GetJSON();
                await Release_GetJSON();
                return CompareUpdate();
            }
            catch (Exception err)
            {
                System.Diagnostics.Trace.WriteLine("Check for updates failed\nError:" + err.Message);
                return false;
            }
        }
        private async Task Release_GetJSON()
        {
            var url = GithubAPI.RELEASE_URL;
            var notes = await GetUpdateNotes(url);

            foreach (var note in notes)
            {
                if (note.prerelease) PrereleaseNotes.Add(note);
                else ReleaseNotes.Add(note);
            }

            foreach (var entry in ReleaseNotes) entry.isBeta = false;
            foreach (var entry in PrereleaseNotes) entry.isBeta = false;

        }
        private async Task Beta_GetJSON()
        {
            var url = GithubAPI.BETA_URL;
            BetaNotes = await GetUpdateNotes(url);
            foreach (var entry in BetaNotes) entry.isBeta = true;
        }


        #endregion

        #region Button
        public void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (isLatestBeta()) JemExtensions.WebExtensions.LaunchWebLink(Constants.UPDATES_BETA_PAGE);
            else JemExtensions.WebExtensions.LaunchWebLink(Constants.UPDATES_RELEASE_PAGE);
        }

        #endregion

        private bool CompareUpdate()
        {
            string OnlineTag = GetLatestTag();
            string LocalTag = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
            System.Diagnostics.Trace.WriteLine("Current tag: " + LocalTag);
            System.Diagnostics.Trace.WriteLine("Latest tag: " + OnlineTag);

            try
            {
                // if current tag < than latest tag
                if (IsVersionNewer(LocalTag, OnlineTag))
                {
                    System.Diagnostics.Trace.WriteLine("New version available!");
                    return true;
                }
                else return false;
            }
            catch
            {
                return false;
            }
        }
        public bool IsVersionNewer(string localVersionStr, string remoteVersionStr)
        {
            int CheckGroup(string[] local, string[] remote, int index)
            {
                var requiredLength = index + 1;
                if (local.Length >= requiredLength && remote.Length >= requiredLength)
                {
                    if (int.TryParse(local[index], out int localInt) && int.TryParse(remote[index], out int remoteInt))
                    {
                        //Debugging Only
                        //Console.WriteLine(string.Format("Local Number {0}: {1}", index, localInt));
                        //Console.WriteLine(string.Format("Local Number {0}: {1}", index, remoteInt));
                        if (localInt < remoteInt)
                        {
                            return 1;
                        }
                        else if (localInt == remoteInt)
                        {
                            return 0;
                        }
                        else if (localInt > remoteInt)
                        {
                            return -1;
                        }

                    }
                }
                return -2;
            }

            string[] localGroups = localVersionStr.Split('.');
            string[] remoteGroups = remoteVersionStr.Split('.');

            var yearResult = CheckGroup(localGroups, remoteGroups, 0);
            if (yearResult == -2 || yearResult == -1) return false;
            else if (yearResult == 1) return true;
            else if (yearResult == 0)
            {
                var monthResult = CheckGroup(localGroups, remoteGroups, 1);
                if (monthResult == -2 || monthResult == -1) return false;
                else if (monthResult == 1) return true;
                else if (monthResult == 0)
                {
                    var dayResult = CheckGroup(localGroups, remoteGroups, 2);
                    if (dayResult == -2 || dayResult == -1) return false;
                    else if (dayResult == 1) return true;
                    else if (dayResult == 0)
                    {
                        var buildResult = CheckGroup(localGroups, remoteGroups, 3);
                        if (buildResult == -2 || buildResult == -1) return false;
                        else if (buildResult == 1) return true;
                        else if (buildResult == 0)
                        {
                            return false;
                        }
                    }
                }
            }



            return false;
        }

        private async Task<List<GithubReleaseInfo>> GetUpdateNotes(string url)
        {
            HttpClient client = new HttpClient();
            string json = string.Empty;
            client.DefaultRequestHeaders.UserAgent.TryParseAdd(@"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36");
            var httpResponse = await client.GetStreamAsync(url);
            using (var streamReader = new StreamReader(httpResponse)) json = streamReader.ReadToEnd();
            return JsonConvert.DeserializeObject<List<GithubReleaseInfo>>(json);
        }
    }
}
