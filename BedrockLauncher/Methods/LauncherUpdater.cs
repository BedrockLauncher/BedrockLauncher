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

namespace BedrockLauncher.Methods
{
    public class LauncherUpdater
    {

        private class UpdateNote
        {
            public string Tag { get; set; }
            public string Description { get; set; }
        }

        private string LATEST_BUILD_LINK { get => BL_Core.Properties.Settings.Default.GithubPage + "/releases/latest"; }
        private string latestTag;
        private string latestTagDescription;
        public LauncherUpdater()
        {
            Program.Log("Checking for updates");
            CheckUpdates();
        }

        private async void CheckUpdates()
        {
            try
            {
                if (Properties.Settings.Default.UseBetaBuilds)
                {
                    var json = await Task.Run(Beta_GetJSON);
                    SetTag(json.Tag);
                    SetTagDescription(json.Description);
                }
                else
                {
                    var html = await Task.Run(Release_GetHtml);
                    Release_LookForLatestTag(html);
                    Release_LookForDescription(html);
                }
            }
            catch (Exception err)
            {
                Program.Log("Check for updates failed\nError:" + err.Message);
                latestTag = "";
                latestTagDescription = "";
            }
        }

        #region Release Updates

        private HtmlAgilityPack.HtmlDocument Release_GetHtml()
        {
            using (WebClient wc = new WebClient())
            {
                var web = new HtmlWeb();
                var doc = web.Load(LATEST_BUILD_LINK);
                return doc;
            }
        }
        private void Release_LookForLatestTag(HtmlDocument html)
        {
            foreach (HtmlNode node in html.DocumentNode.SelectNodes("//head/meta[@property='og:url']"))
            {
                string[] urlParts = node.Attributes["content"].Value.Split('/');
                SetTag(urlParts[urlParts.Length - 1]); // return latest part, for example 0.1.2 from full link
            }
        }
        private void Release_LookForDescription(HtmlDocument html)
        {
            foreach (HtmlNode node in html.DocumentNode.SelectNodes("//div[@class='markdown-body']"))
            {
                SetTagDescription(node.InnerText.Trim());
            }
        }

        #endregion

        #region Beta Updates

        private UpdateNote Beta_GetJSON()
        {
            string json = string.Empty;
            var url = "https://api.github.com/repos/CarJem/BedrockLauncher-Beta/contents/changelog.json";
            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
            httpRequest.Accept = "application/vnd.github.v3.raw";
            httpRequest.Headers["Authorization"] = "Bearer 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";
            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) json = streamReader.ReadToEnd();
            Console.WriteLine(httpResponse.StatusCode);
            return JsonConvert.DeserializeObject<UpdateNote>(json); 
        }

        #endregion

        #region All Updates

        public void SetTag(string tag)
        {
            this.latestTag = tag;
            Program.Log("Current tag: " + BL_Core.Properties.Settings.Default.Version);
            Program.Log("Latest tag: " + latestTag);

            try
            {
                // if current tag < than latest tag
                if (int.Parse(BL_Core.Properties.Settings.Default.Version.Replace(".", "")) < int.Parse(latestTag.Replace(".", "")))
                {
                    Program.Log("New version available!");
                    ShowUpdateButton(5000);
                }
            }
            catch
            {

            }
        }

        public void SetTagDescription(string description)
        {
            this.latestTagDescription = description;
        }

        #endregion

        private void startUpdate()
        {
            try
            {
                string installerPath = Path.Combine(Directory.GetCurrentDirectory(), "Installer.exe");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = @"--silent" + (Properties.Settings.Default.UseBetaBuilds),
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
        }
        async private void ShowUpdateButton(int time) 
        {
            ConfigManager.MainThread.updateButton.Visibility = Visibility.Visible;
            ConfigManager.MainThread.updateButton.Click += UpdateButton_Click;
            showAdvancementButton();
            await Task.Delay(time);
            hideAdvancementButton();
        }
        void hideAdvancementButton()
        {
            // hide update 'advancement'
            Storyboard storyboard2 = new Storyboard();
            ThicknessAnimation animation2 = new ThicknessAnimation
            {
                From = new Thickness(0, 5, 5, 0),
                To = new Thickness(0, -75, 5, 0),
                Duration = new Duration(TimeSpan.FromMilliseconds(500))
            };
            storyboard2.Children.Add(animation2);
            Storyboard.SetTargetProperty(animation2, new PropertyPath(Border.MarginProperty));
            Storyboard.SetTarget(animation2, ConfigManager.MainThread.updateButton);
            storyboard2.Begin();
        }

        void showAdvancementButton()
        {
            // show update 'advancement'
            Storyboard storyboard = new Storyboard();
            ThicknessAnimation animation = new ThicknessAnimation
            {
                From = new Thickness(0, -75, 5, 0),
                To = new Thickness(0, 5, 5, 0),
                BeginTime = TimeSpan.FromSeconds(2),
                Duration = new Duration(TimeSpan.FromMilliseconds(500))
            };
            storyboard.Children.Add(animation);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Border.MarginProperty));
            Storyboard.SetTarget(animation, ConfigManager.MainThread.updateButton);
            storyboard.Begin();
        }
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            Program.Log("Trying to update");
            startUpdate();
        }
        public string getLatestTag()
        {
            return latestTag;
        }
        public string getLatestTagDescription()
        {
            return latestTagDescription;
        }
    }
}
