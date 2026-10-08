using BedrockLauncher;
using BedrockLauncher.Core;
using BedrockLauncher.ViewModels;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

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

        public void CheckForUpdates() => Task.Run(async () =>
        {
            await CheckForUpdatesAsync();
        });
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
            JemExtensions.WebExtensions.LaunchWebLink(Constants.UPDATES_RELEASE_PAGE);
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
            if (Version.TryParse(localVersionStr, out Version localVersion) && Version.TryParse(remoteVersionStr, out Version remoteVersion))
            {
                return localVersion < remoteVersion;
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
