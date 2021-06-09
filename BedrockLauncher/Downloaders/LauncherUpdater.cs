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
using BedrockLauncher.Methods;
using Newtonsoft.Json;
using BL_Core;
using System.Runtime.InteropServices;

namespace BedrockLauncher.Downloaders
{
    public class LauncherUpdater
    {
        #region Definitions


        public List<UpdateNote> Notes
        {
            get
            {
                var list = new List<UpdateNote>();
                list.AddRange(ReleaseNotes);
                list.AddRange(BetaNotes);
                list.Sort((x, y) => y.published_at.CompareTo(x.published_at));
                return list;
            }
        }
        private List<UpdateNote> ReleaseNotes { get; set; } = new List<UpdateNote>();
        private List<UpdateNote> BetaNotes { get; set; } = new List<UpdateNote>();

        public string LatestTag
        {
            get
            {
                var list = Notes;
                if (list.Count == 0) return string.Empty;
                return list[0].tag_name;
            }
        }
        public string LatestTagBody
        {
            get
            {
                var list = Notes;
                if (list.Count == 0) return string.Empty;
                return list[0].body;
            }
        }
        public string Release_LatestTag { get; private set; } = string.Empty;
        public string Release_LatestTagBody { get; private set; } = string.Empty;
        public string Beta_LatestTag { get; private set; } = string.Empty;
        public string Beta_LatestTagBody { get; private set; } = string.Empty;

        #endregion

        #region Accessors

        public string GetLatestTag()
        {
            if (Properties.LauncherSettings.Default.UseBetaBuilds) return Beta_LatestTag;
            else return Release_LatestTag;
        }

        public string GetLatestTagBody()
        {
            if (Properties.LauncherSettings.Default.UseBetaBuilds) return Beta_LatestTagBody;
            else return Release_LatestTagBody;
        }

        #endregion

        #region Init

        public LauncherUpdater()
        {
            CheckForUpdates();
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
        public async Task<bool> CheckForUpdatesAsync()
        {
            System.Diagnostics.Debug.WriteLine("Checking for updates");
            try
            {
                ReleaseNotes.Clear();
                BetaNotes.Clear();
                await Task.Run(Beta_GetJSON);
                await Task.Run(Release_GetJSON);
                CompareUpdate();
                return true;
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine("Check for updates failed\nError:" + err.Message);
                return false;
            }
        }
        private void Release_GetJSON()
        {
            string json = string.Empty;
            var url = GithubAPI.RELEASE_URL;
            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.Headers["Authorization"] = "Bearer " + GithubAPI.ACCESS_TOKEN;
            httpRequest.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) json = streamReader.ReadToEnd();
            System.Diagnostics.Debug.WriteLine(httpResponse.StatusCode);
            ReleaseNotes = JsonConvert.DeserializeObject<List<UpdateNote>>(json);
            if (ReleaseNotes.Count != 0)
            {
                var note = ReleaseNotes[0];
                this.Release_LatestTag = note.tag_name;
                this.Release_LatestTagBody = note.body;
            }

        }
        private void Beta_GetJSON()
        {
            string json = string.Empty;
            var url = GithubAPI.BETA_URL;
            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.Headers["Authorization"] = "Bearer " + GithubAPI.ACCESS_TOKEN;
            httpRequest.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) json = streamReader.ReadToEnd();
            System.Diagnostics.Debug.WriteLine(httpResponse.StatusCode);
            BetaNotes.AddRange(JsonConvert.DeserializeObject<List<UpdateNote>>(json));
            if (BetaNotes.Count != 0)
            {
                var note = BetaNotes[0];
                this.Beta_LatestTag = note.tag_name;
                this.Beta_LatestTagBody = note.body;
            }
        }


        #endregion

        #region Button
        public void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Trying to update");
            StartUpdate();
        }

        #endregion

        private void CompareUpdate()
        {
            string LatestTag = (Properties.LauncherSettings.Default.UseBetaBuilds ? Beta_LatestTag : Release_LatestTag);
            string CurrentTag = BL_Core.Properties.Settings.Default.Version;
            System.Diagnostics.Debug.WriteLine("Current tag: " + CurrentTag);
            System.Diagnostics.Debug.WriteLine("Latest tag: " + LatestTag);

            try
            {
                // if current tag < than latest tag
                if (int.Parse(CurrentTag.Replace(".", "")) < int.Parse(LatestTag.Replace(".", "")))
                {
                    System.Diagnostics.Debug.WriteLine("New version available!");
                    ViewModels.LauncherModel.MainThread.updateButton.ShowUpdateButton();
                }
            }
            catch
            {

            }
        }
        private void StartUpdate()
        {
            try
            {
                string installerPath = Path.Combine(FilepathManager.Default.ExecutableDirectory, "Installer.exe");
                string tempPath = Path.Combine(Path.GetTempPath(), "Installer.exe");

                File.Copy(installerPath, tempPath, true);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = tempPath,
                    Arguments = GetArgs(),
                    UseShellExecute = true,
                    Verb = "runas",
                };
                System.Diagnostics.Process.Start(startInfo);
                Application.Current.Shutdown();
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine("Installer launch failed\nError: " + err);
            }


            string GetArgs()
            {
                string silent = "--silent";
                string beta = (Properties.LauncherSettings.Default.UseBetaBuilds ? "--beta" : "");
                string path = "--path=\"" + FilepathManager.Default.ExecutableDirectory + "\"";

                return string.Join(" ", silent, beta, path);
            }
        }

    }
}
