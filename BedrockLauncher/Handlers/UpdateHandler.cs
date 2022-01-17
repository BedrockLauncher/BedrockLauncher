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
using BedrockLauncher;
using System.Runtime.InteropServices;
using BedrockLauncher.ViewModels;
using System.Linq;
using System.Threading;
using BedrockLauncher.Core;

namespace BedrockLauncher.Handlers
{
    public class UpdateHandler
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



        #endregion

        #region Accessors

        public bool isLatestBeta()
        {
            var list = Notes;
            if (list.Count == 0) return false;
            if (Properties.LauncherSettings.Default.UseBetaBuilds) return list[0].isBeta;
            else return false;
        }

        public string GetLatestTag()
        {
            var list = Notes;
            if (list.Count == 0) return string.Empty;

            if (Properties.LauncherSettings.Default.UseBetaBuilds) return list[0].tag_name;
            else if (list.Exists(x => !x.isBeta)) return list.First(x => x.isBeta == false).tag_name;
            else return string.Empty;
        }

        public string GetLatestTagBody()
        {
            var list = Notes;
            if (list.Count == 0) return string.Empty;

            if (Properties.LauncherSettings.Default.UseBetaBuilds) return list[0].body;
            else if (list.Exists(x => !x.isBeta)) return list.First(x => x.isBeta == false).body;
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
            httpRequest.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) json = streamReader.ReadToEnd();
            ReleaseNotes = JsonConvert.DeserializeObject<List<UpdateNote>>(json);
            foreach (var entry in ReleaseNotes) entry.isBeta = false;

        }
        private void Beta_GetJSON()
        {
            string json = string.Empty;
            var url = GithubAPI.BETA_URL;
            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) json = streamReader.ReadToEnd();
            BetaNotes.AddRange(JsonConvert.DeserializeObject<List<UpdateNote>>(json));
            foreach (var entry in BetaNotes) entry.isBeta = true;
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
            string OnlineTag = GetLatestTag();
            string LocalTag = BedrockLauncher.Core.Properties.Settings.Default.Version;
            System.Diagnostics.Debug.WriteLine("Current tag: " + LocalTag);
            System.Diagnostics.Debug.WriteLine("Latest tag: " + OnlineTag);

            try
            {
                // if current tag < than latest tag
                if (int.Parse(LocalTag.Replace(".", "")) < int.Parse(OnlineTag.Replace(".", "")))
                {
                    System.Diagnostics.Debug.WriteLine("New version available!");
                    ViewModels.MainViewModel.Default.UpdateButton.ShowUpdateButton();
                }
            }
            catch
            {

            }
        }

        private async void StartUpdate()
        {
            string InstallerPath = string.Empty;

            try
            {
                WebClient client = new WebClient();
                string installerName = "BedrockLauncher.Installer.exe";
                string json = string.Empty;
                var httpRequest = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/BedrockLauncher/BedrockLauncher.Installer/releases/latest");
                httpRequest.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) json = streamReader.ReadToEnd();
                var result = JsonConvert.DeserializeObject<UpdateNote>(json);

                if (result.assets != null)
                {
                    if (result.assets.ToList().Exists(x => x.name == installerName))
                    {
                        var installer = result.assets.ToList().FirstOrDefault(x => x.name == installerName);
                        InstallerPath = Path.Combine(Path.GetTempPath(), installerName);
                        File.Delete(InstallerPath);

                        var httpRequest2 = (HttpWebRequest)WebRequest.Create(installer.url);
                        httpRequest2.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                        httpRequest2.Accept = "application/octet-stream";

                        using (Stream output = System.IO.File.OpenWrite(InstallerPath))
                        using (WebResponse response = httpRequest2.GetResponse())
                        using (Stream stream = response.GetResponseStream())
                        {
                            await stream.CopyToAsync(output);
                        }

                        
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = InstallerPath,
                            Arguments = GetArgs(),
                            UseShellExecute = true,
                            Verb = "runas",
                        };
                        System.Diagnostics.Process.Start(startInfo);
                        Application.Current.Shutdown();

                        string GetArgs()
                        {
                            string silent = "--silent";
                            string beta = isLatestBeta() ? "--beta" : "";
                            string path = "--path=\"" + MainViewModel.Default.FilePaths.ExecutableDirectory + "\"";

                            return string.Join(" ", silent, beta, path);
                        }
                        
                    }
                }
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine("Installer launch failed\nError: " + err);
            }


        }
    }
}
