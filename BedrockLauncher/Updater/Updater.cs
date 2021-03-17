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

namespace BedrockLauncher
{
    public class Updater
    {
        private const string LATEST_BUILD_LINK = "https://github.com/XlynxX/BedrockLauncher/releases/latest";
        private string latestTag;
        private string latestTagDescription;
        public Updater()
        {
            Debug.WriteLine("Checking for updates");
            try
            {
                CheckUpdates();
            }
            catch (Exception err)
            {
                Debug.WriteLine("Check for updates failed\nError:" + err.Message);
            }
        }

        private async void CheckUpdates()
        {
            var html = await Task.Run(getHtml);
            lookForLatestTag(html);
            lookForDescription(html);
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
                Debug.WriteLine("Current tag: " + Properties.Settings.Default.Version);
                Debug.WriteLine("Latest tag: " + latestTag);
                // if current tag < than latest tag
                if (int.Parse(Properties.Settings.Default.Version.Replace(".", "")) < int.Parse(latestTag.Replace(".", "")))
                {
                    Debug.WriteLine("New version available!");
                    ShowUpdateButton(5000);
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
                Debug.WriteLine("Installer launch failed\nError: " + err);
            }
        }
        async private void ShowUpdateButton(int time) 
        {
            ((MainWindow)Application.Current.MainWindow).updateButton.Click += UpdateButton_Click;
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
            Storyboard.SetTarget(animation2, ((MainWindow)Application.Current.MainWindow).updateButton);
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
            Storyboard.SetTarget(animation, ((MainWindow)Application.Current.MainWindow).updateButton);
            storyboard.Begin();
        }
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Trying to update");
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
