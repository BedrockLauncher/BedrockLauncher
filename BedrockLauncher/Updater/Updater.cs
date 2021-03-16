using System;
using System.Windows;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.Windows.Controls;

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
            var nodes = html.DocumentNode.SelectNodes("//head/meta");
            foreach (HtmlNode node in nodes)
            {
                try
                { 
                    if (node.Attributes["content"].Value.StartsWith("/XlynxX/BedrockLauncher/releases/tag/"))
                    {
                        this.latestTag = node.Attributes["content"].Value.Replace("/XlynxX/BedrockLauncher/releases/tag/", "");
                        this.latestTagDescription = node.NextSibling.Attributes["content"].Value;
                        Debug.WriteLine("Current tag: " + Properties.Settings.Default.Version);
                        Debug.WriteLine("Latest tag: " + latestTag);
                        Debug.WriteLine("Latest tag description: " + latestTagDescription);
                        // if current tag < than latest tag
                        if (int.Parse(Properties.Settings.Default.Version.Replace(".", "")) < int.Parse(latestTag.Replace(".", "")))
                        {
                            Debug.WriteLine("New version available!");
                            ShowUpdateButton(5000);
                        }
                    }
                }
                catch { }
            }
            //Console.WriteLine("Node Name: " + node.Name + "\n" + node.OuterHtml);
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
        private void startUpdate()
        {
            try
            {
                string installerPath = Path.Combine(Directory.GetCurrentDirectory(), "Installer.exe");
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = installerPath;
                startInfo.Arguments = @"--silent";
                startInfo.UseShellExecute = true;
                startInfo.Verb = "runas";
                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
            catch (Exception err)
            {
                Debug.WriteLine("installer launch failed\nError: " + err);
            }
        }
        async private void ShowUpdateButton(int time) 
        {
            ((MainWindow)Application.Current.MainWindow).updateButton.Click += updateButton_Click;
            showAdvancementButton();
            await Task.Delay(time);
            hideAdvancementButton();
        }
        void hideAdvancementButton()
        {
            // hide update 'advancement'
            Storyboard storyboard2 = new Storyboard();
            ThicknessAnimation animation2 = new ThicknessAnimation();
            animation2.From = new Thickness(0, 5, 5, 0);
            animation2.To = new Thickness(0, -75, 5, 0);
            animation2.BeginTime = TimeSpan.FromSeconds(8);
            animation2.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            storyboard2.Children.Add(animation2);
            Storyboard.SetTargetProperty(animation2, new PropertyPath(Border.MarginProperty));
            Storyboard.SetTarget(animation2, ((MainWindow)Application.Current.MainWindow).updateButton);
            storyboard2.Begin();
        }

        void showAdvancementButton()
        {
            // show update 'advancement'
            Storyboard storyboard = new Storyboard();
            ThicknessAnimation animation = new ThicknessAnimation();
            animation.From = new Thickness(0, -75, 5, 0);
            animation.To = new Thickness(0, 5, 5, 0);
            animation.BeginTime = TimeSpan.FromSeconds(2);
            animation.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            storyboard.Children.Add(animation);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Border.MarginProperty));
            Storyboard.SetTarget(animation, ((MainWindow)Application.Current.MainWindow).updateButton);
            storyboard.Begin();
        }
        private void updateButton_Click(object sender, RoutedEventArgs e)
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
