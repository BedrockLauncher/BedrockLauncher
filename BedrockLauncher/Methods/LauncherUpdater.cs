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

namespace BedrockLauncher.Methods
{
    public class LauncherUpdater
    {
        #region Definitions

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
            Program.Log("Checking for updates");
            CheckForUpdatesAsync();
        }
        private async void CheckForUpdatesAsync()
        {
            try
            {
                await Task.Run(Beta_GetJSON);
                await Task.Run(Release_GetJSON);
                CompareUpdate();
            }
            catch (Exception err)
            {
                Program.Log("Check for updates failed\nError:" + err.Message);
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
            Console.WriteLine(httpResponse.StatusCode);
            var note = JsonConvert.DeserializeObject<UpdateNote>(json);
            this.Release_LatestTag = note.tag_name;
            this.Release_LatestTagBody = note.body;
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
            Console.WriteLine(httpResponse.StatusCode);
            var note = JsonConvert.DeserializeObject<UpdateNote>(json);
            this.Beta_LatestTag = note.tag_name;
            this.Beta_LatestTagBody = note.body;
        }


        #endregion

        #region Button
        public void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            Program.Log("Trying to update");
            StartUpdate();
        }

        #endregion

        private void CompareUpdate()
        {
            string LatestTag = (Properties.LauncherSettings.Default.UseBetaBuilds ? Beta_LatestTag : Release_LatestTag);
            string CurrentTag = BL_Core.Properties.Settings.Default.Version;
            Program.Log("Current tag: " + CurrentTag);
            Program.Log("Latest tag: " + LatestTag);

            try
            {
                // if current tag < than latest tag
                if (int.Parse(CurrentTag.Replace(".", "")) < int.Parse(LatestTag.Replace(".", "")))
                {
                    Program.Log("New version available!");
                    ConfigManager.MainThread.updateButton.ShowUpdateButton();
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
                string installerPath = Path.Combine(Directory.GetCurrentDirectory(), "Installer.exe");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = GetArgs(),
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
            catch (Exception err)
            {
                Program.Log("Installer launch failed\nError: " + err);
            }


            string GetArgs()
            {
                string silent = (Properties.LauncherSettings.Default.UseSilentUpdates ? "--silent" : "");
                string beta = (Properties.LauncherSettings.Default.UseBetaBuilds ? "--beta" : "");

                return string.Join(" ", silent, beta);
            }
        }
    }
}
