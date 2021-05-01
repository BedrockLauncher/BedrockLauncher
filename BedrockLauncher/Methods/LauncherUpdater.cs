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
using BedrockLauncher.Core;

namespace BedrockLauncher.Methods
{
    public class LauncherUpdater
    {
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
                var html = await Task.Run(getHtml);
                lookForLatestTag(html);
                lookForDescription(html);
            }
            catch (Exception err)
            {
                Program.Log("Check for updates failed\nError:" + err.Message);
                latestTag = "";
                latestTagDescription = "";
            }
        }

        private HtmlAgilityPack.HtmlDocument getHtml()
        {
            using (WebClient wc = new WebClient())
            {
                var web = new HtmlWeb();
                var doc = web.Load(LATEST_BUILD_LINK);
                return doc;
            }
        }
        private void lookForLatestTag(HtmlDocument html)
        {
            foreach (HtmlNode node in html.DocumentNode.SelectNodes("//head/meta[@property='og:url']"))
            {
                string[] urlParts = node.Attributes["content"].Value.Split('/');
                this.latestTag = urlParts[urlParts.Length - 1]; // return latest part, for example 0.1.2 from full link
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
        }
        private void lookForDescription(HtmlDocument html)
        {
            foreach (HtmlNode node in html.DocumentNode.SelectNodes("//div[@class='markdown-body']"))
            {
                this.latestTagDescription = node.InnerText.Trim();
            }
        }
        private void startUpdate()
        {
            try
            {
                string installerPath = Path.Combine(Directory.GetCurrentDirectory(), "Installer.exe");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = @"--silent",
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
