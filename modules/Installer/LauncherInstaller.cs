using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Win32;
using IWshRuntimeLibrary;
using System.Xml;
using System.Diagnostics;
using Installer.Pages;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BL_Core;

namespace Installer
{
    class LauncherInstaller
    {
        private string Path;
        private string Build_Version;
        private bool Silent;
        private InstallationProgressPage InstallationProgressPage;

        public static bool MakeDesktopIcon { get; set; } = false;
        public static bool MakeStartMenuIcon { get; set; } = false;
        public static bool IsBeta { get; set; } = false;
        public static bool RegisterAsProgram { get; set; } = false;
        public static bool RunOnExit { get; set; } = true;

        public LauncherInstaller(string installationPath, InstallationProgressPage installationProgressPage, bool silent = false)
        {
            this.Silent = silent;
            this.InstallationProgressPage = installationProgressPage;
            this.Path = installationPath;
            Install_Start();
        }

        #region Install Steps

        private void Install_Start()
        {
            UpdateUI(UpdateParam.InstallStart);
            if (!Directory.Exists(Path))
            {
                try
                {
                    Directory.CreateDirectory(Path);
                    Task.Run(() => Install_Download(IsBeta));
                    return;
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.ToString());
                    return;
                }
            }
            else
            {
                Install_CleanFolder(Path);
                Task.Run(() => Install_Download(IsBeta));
            }

        }
        private void Install_CleanFolder(string path)
        {
            // kill launcher
            Process[] prs = Process.GetProcesses();
            foreach (Process pr in prs)
            {
                if (pr.ProcessName == "BedrockLauncher")
                {
                    pr.Kill();
                }

            }
            // delete old installer if exists
            if (System.IO.File.Exists(System.IO.Path.Combine(path, "Installer.exe.old")))
            {
                System.IO.File.Delete(System.IO.Path.Combine(path, "Installer.exe.old"));
            }

            foreach (string file in Directory.GetFiles(path))
            {
                // renaming currently running installer to replace with new later
                if (file.EndsWith("Installer.exe"))
                {
                    System.IO.File.Move(file, System.IO.Path.Combine(path, "Installer.exe.old"));
                }
                else
                {
                    // delete other files
                    try
                    {
                        System.IO.File.Delete(file);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("error file: " + file + "error: " + err.Message);
                    }
                }
            }
        }
        private async void Install_Download(bool isBeta)
        {
            // actually start downloading
            string downloadUrl = string.Empty;
            int fileSize = 0;


            var release_page_url = (isBeta ? GithubAPI.BETA_URL : GithubAPI.RELEASE_URL);
            var httpRequest = (HttpWebRequest)WebRequest.Create(release_page_url);
            httpRequest.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
            httpRequest.Headers["Authorization"] = "Bearer " + GithubAPI.ACCESS_TOKEN;
            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var contentsJson = streamReader.ReadToEnd();
                var contents = JsonConvert.DeserializeObject<UpdateNote>(contentsJson);
                downloadUrl = contents.assets[0].url;
                fileSize = contents.assets[0].size;
            }

            var file = System.IO.Path.Combine(this.Path, "build.zip");
            var httpRequest2 = (HttpWebRequest)WebRequest.Create(downloadUrl);
            httpRequest2.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
            httpRequest2.Headers["Authorization"] = "Bearer " + GithubAPI.ACCESS_TOKEN;
            httpRequest2.Accept = "application/octet-stream";


            Exception ex = null;

            UpdateUI(UpdateParam.ProgressIndeterminate);

            try
            {
                using (Stream output = System.IO.File.OpenWrite(file))
                    using (WebResponse response = httpRequest2.GetResponse())
                        using (Stream stream = response.GetResponseStream())
                            await stream.CopyToAsync(output);
            }
            catch (Exception e) { ex = e; }

            Install_Extract(null, new System.ComponentModel.AsyncCompletedEventArgs(ex, false, null));
        }
        private void Install_Extract(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("File download cancelled.");
                MessageBox.Show("File download cancelled.");
                return;
            }

            if (e.Error != null)
            {
                Console.WriteLine(e.Error.ToString());
                MessageBox.Show("An error has occured.\n" + e.Error.ToString());
                return;
            }

            // unpack build archive
            ZipFile.ExtractToDirectory(System.IO.Path.Combine(this.Path, "build.zip"), this.Path);
            System.IO.File.Delete(System.IO.Path.Combine(this.Path, "build.zip"));

            UpdateUI(UpdateParam.ExtractionComplete);

            // async add to registry
            Install_RunAsyncTasks();
        }
        private async void Install_RunAsyncTasks()
        {
            if (RegisterAsProgram)
            {
                await Task.Run(RegisterApp);
                CreateShortcut();
            }
            CreateOptionalShortcuts();


            if (this.Silent)
            {
                LaunchApp();
                Application.Current.Shutdown();
            }
            else UpdateUI(UpdateParam.InstallComplete);
        }
        private void Install_Finish(object sender, RoutedEventArgs e)
        {
            if (LauncherInstaller.RunOnExit) LaunchApp();
            Application.Current.Shutdown();
        }

        #endregion

        #region Actions


        private void LaunchApp()
        {
            Process.Start(new ProcessStartInfo(System.IO.Path.Combine(Path, "BedrockLauncher.exe")));
        }
        private bool GetBuildVersion()
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(System.IO.Path.Combine(Path, "BedrockLauncher.exe.config"));
            XmlNodeList nodeList = xmldoc.GetElementsByTagName("setting");
            foreach (XmlNode node in nodeList)
            {
                if (node.OuterXml.Contains("<setting name=\"Version\" serializeAs=\"String\">"))
                {
                    Console.WriteLine("build version: " + node.InnerText);
                    this.Build_Version = node.InnerText;
                }
            }
            return true;
        }
        private bool RegisterApp()
        {
            try
            {
                string registryLocation = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
                RegistryKey regKey = (Registry.LocalMachine).OpenSubKey(registryLocation, true);
                RegistryKey progKey = regKey.CreateSubKey("Minecraft Bedrock Launcher");
                progKey.SetValue("DisplayName", "Minecraft Bedrock Launcher", RegistryValueKind.String);
                progKey.SetValue("InstallLocation", Path, RegistryValueKind.ExpandString);
                progKey.SetValue("DisplayIcon", System.IO.Path.Combine(Path, "BedrockLauncher.exe"), RegistryValueKind.String);
                progKey.SetValue("UninstallString", System.IO.Path.Combine(Path, "Uninstaller.exe"), RegistryValueKind.ExpandString);
                progKey.SetValue("DisplayVersion", Build_Version, RegistryValueKind.String);
                progKey.SetValue("Publisher", "BedrockLauncher", RegistryValueKind.String);
                Console.WriteLine("Successfully added to control panel!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured! Error: {0}", ex.Message);
                return false;
            }
        }
        private void CreateShortcut()
        {
            string shortcutPath = System.IO.Path.Combine(Path, "Minecraft Bedrock Launcher.lnk");

            WshShell wshShell = new WshShell();
            IWshShortcut Shortcut = (IWshShortcut)wshShell.CreateShortcut(shortcutPath);
            Shortcut.TargetPath = System.IO.Path.Combine(Path, "BedrockLauncher.exe");
            Shortcut.WorkingDirectory = Path;
            Shortcut.Save();
            Console.WriteLine("shortcut created");
        }
        private void CreateOptionalShortcuts()
        {
            // create desktop shortcut if needed
            if ((bool)MakeDesktopIcon)
            {
                Console.WriteLine("added to desktop");
                System.IO.File.Copy(System.IO.Path.Combine(Path, "Minecraft Bedrock Launcher.lnk"), System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Minecraft Bedrock Launcher.lnk"), true);
            }

            // add to start menu if needed
            if ((bool)MakeStartMenuIcon)
            {
                Console.WriteLine("added to start menu");
                string commonStartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
                string appStartMenuPath = System.IO.Path.Combine(commonStartMenuPath, "Programs", "Minecraft Launcher");
                if (!Directory.Exists(appStartMenuPath)) { Directory.CreateDirectory(appStartMenuPath); }
                System.IO.File.Copy(System.IO.Path.Combine(Path, "Minecraft Bedrock Launcher.lnk"), System.IO.Path.Combine(appStartMenuPath, "Minecraft Bedrock Launcher.lnk"), true);
            }
        }

        #endregion

        public void UpdateUI(UpdateParam param)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (param == UpdateParam.InstallStart)
                {
                    ((MainWindow)Application.Current.MainWindow).NextBtn.IsEnabled = false;
                    ((MainWindow)Application.Current.MainWindow).BackBtn.IsEnabled = false;
                    ((MainWindow)Application.Current.MainWindow).MainFrame.Navigate(this.InstallationProgressPage);
                }
                else if (param == UpdateParam.ExtractionComplete)
                {
                    ((MainWindow)Application.Current.MainWindow).CancelBtn.Content = "Finish";
                    ((MainWindow)Application.Current.MainWindow).NextBtn.Visibility = Visibility.Hidden;
                    ((MainWindow)Application.Current.MainWindow).BackBtn.Visibility = Visibility.Hidden;

                    this.InstallationProgressPage.progressBar.IsIndeterminate = false;
                    this.InstallationProgressPage.InstallPanel.Visibility = Visibility.Collapsed;
                }
                else if (param == UpdateParam.InstallComplete)
                {
                    ((MainWindow)Application.Current.MainWindow).FinishBtn.Visibility = Visibility.Visible;
                    ((MainWindow)Application.Current.MainWindow).FinishBtn.Click += Install_Finish;
                    ((MainWindow)Application.Current.MainWindow).CancelBtn.Visibility = Visibility.Hidden;
                }
                else if (param == UpdateParam.ProgressIndeterminate)
                {
                    this.InstallationProgressPage.progressBar.IsIndeterminate = true;
                }
            });

        }
    }
}
