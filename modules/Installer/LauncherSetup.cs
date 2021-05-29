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
using BedrockLauncherSetup.Pages;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BL_Core;
using MessageBox = System.Windows.MessageBox;
using System.Threading;
using ExtensionsDotNET;

namespace BedrockLauncherSetup
{
    public class LauncherSetup
    {
        public InstallationProgressPage ProgressPage;

        private CancellationTokenSource ICancel = new CancellationTokenSource();
        private long CalculatedFileSize = 0;
        private bool CanCancel = false;
        private Task DownloadTask;
        private bool InstallStarted = false;



        private static string TempFolder
        {
            get
            {
                return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "InstallerTemp");
            }
        }
        private string Build_Version { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool Silent { get; set; } = false;
        public bool MakeDesktopIcon { get; set; } = false;
        public bool MakeStartMenuIcon { get; set; } = false;
        public bool IsBeta { get; set; } = false;
        public bool RegisterAsProgram { get; set; } = false;
        public bool RunOnExit { get; set; } = true;

        public LauncherSetup()
        {

        }

        #region General Methods

        public void CancelInstall()
        {
            if (!InstallStarted) Environment.Exit(0);
            else if (!ICancel.IsCancellationRequested && CanCancel) ICancel.Cancel();
        }
        public void StartInstall()
        {
            InstallStarted = false;
            ICancel.Token.Register(() => Install_Cancel());
            Task.Run(Install, ICancel.Token);
        }

        #endregion

        #region Install Steps

        private async void Install()
        {
            try
            {
                CanCancel = false;

                UpdateUI(UpdateParam.InstallStart);

                Install_InitalPrep(Path, TempFolder);

                Install_UnlockCheck(Path, TempFolder);

                Install_Backup(Path, TempFolder);

                await Install_Download();

                Install_Extract();

                Install_Register();

                Install_CreateShortcuts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(GetResourceString("Installer_Dialog_ErrorOccured_Text") + "\n" + ex.ToString());
                Install_Cancel();
            }

            UpdateUI(UpdateParam.InstallComplete);
            if (Silent) Install_FinishSilent();
        }
        private void Install_InitalPrep(string Path, string TempFolder)
        {
            var directories = new List<string>() { Path, TempFolder };
            foreach (var dir in directories)
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (UnauthorizedAccessException)
                {
                    var results = FileExtensions.WhoIsLocking(dir);
                    var names = String.Join(Environment.NewLine, results.ConvertAll(x => string.Format("{0} ({1})", x.ProcessName, x.Id)).ToArray());

                    MessageBoxResult result = MessageBox.Show(GetResourceString("Installer_InstallationProgress_FilesInUse") + "\r\n" + names + "\r\n" + GetResourceString("Installer_Dialog_TryAgain_Text"), GetResourceString("Installer_Dialog_FileLocked_Text"), MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes) Install_InitalPrep(Path, TempFolder);
                    else Install_Cancel(false);
                }
                catch (Exception err)
                {
                    MessageBox.Show(GetResourceString("Installer_Dialog_ErrorOccured_Text") + "\n" + err.ToString());
                    Install_Cancel(false);
                }
            }
        }
        private void Install_UnlockCheck(string Path, string TempFolder)
        {
            List<Process> LockingProcesses = new List<Process>();

            var allPossibileFiles = Directory.GetFiles(Path, "*", SearchOption.AllDirectories).ToList();
            allPossibileFiles.AddRange(Directory.GetFiles(TempFolder, "*", SearchOption.AllDirectories).ToList());

            foreach (var file in allPossibileFiles)
            {
                if (System.IO.File.Exists(file))
                {
                    bool locked = FileExtensions.IsFileInUse(file, false);
                    if (locked) LockingProcesses.AddRange(FileExtensions.WhoIsLocking(file));
                }
            }

            if (LockingProcesses.Count == 0) return;
            else
            {
                var names = String.Concat(Environment.NewLine, LockingProcesses.ConvertAll(x => string.Format("{0} ({1})", x.ProcessName, x.Id)).ToArray());

                MessageBoxResult result = MessageBox.Show(GetResourceString("Installer_InstallationProgress_FilesInUse") + "\r\n" + names + "\r\n" + GetResourceString("Installer_Dialog_TryAgain_Text"), GetResourceString("Installer_Dialog_FileLocked_Text"), MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes) Install_UnlockCheck(Path, TempFolder);
                else Install_Cancel(false);
            }
        }
        private void Install_Backup(string from, string to)
        {
            var dir = Directory.CreateDirectory(to);
            EmptyDir(dir);
            Directory.CreateDirectory(to);
            BackupDir(from, to, "data");
        }
        private async Task Install_Download()
        {
            CanCancel = true;
            try
            {
                // actually start downloading
                string downloadUrl = string.Empty;
                CalculatedFileSize = 0;
                Progress<long> progress = new System.Progress<long>();
                string progressTitle = GetResourceString("Installer_ProgressBar_Downloading");
                progress.ProgressChanged += (sender, e) => UpdateProgressBar(progressTitle, e, CalculatedFileSize, false);

                var release_page_url = (IsBeta ? GithubAPI.BETA_URL : GithubAPI.RELEASE_URL);
                var httpRequest = (HttpWebRequest)WebRequest.Create(release_page_url);
                httpRequest.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                httpRequest.Headers["Authorization"] = "Bearer " + GithubAPI.ACCESS_TOKEN;
                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var contentsJson = streamReader.ReadToEnd();
                    var contents = JsonConvert.DeserializeObject<UpdateNote>(contentsJson);
                    downloadUrl = contents.assets[0].url;
                    CalculatedFileSize = contents.assets[0].size;
                }

                var file = System.IO.Path.Combine(this.Path, "build.zip");
                var httpRequest2 = (HttpWebRequest)WebRequest.Create(downloadUrl);
                httpRequest2.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                httpRequest2.Headers["Authorization"] = "Bearer " + GithubAPI.ACCESS_TOKEN;
                httpRequest2.Accept = "application/octet-stream";

                using (Stream output = System.IO.File.OpenWrite(file))
                using (WebResponse response = httpRequest2.GetResponse())
                using (Stream stream = response.GetResponseStream())
                {
                    DownloadTask = stream.CopyToAsync(output, progress, ICancel.Token, 81920);
                    await DownloadTask;
                }
            }
            catch (Exception e)
            {
                if (ICancel.IsCancellationRequested) return;
                if (e != null)
                {
                    Console.WriteLine(e.ToString());
                    MessageBox.Show(GetResourceString("Installer_Dialog_ErrorOccured_Text") + "\n" + e.ToString());
                    Install_Cancel();
                    return;
                }
            }
            CanCancel = false;
        }
        private void Install_Extract()
        {
            // unpack build archive
            ZipFile.ExtractToDirectory(System.IO.Path.Combine(this.Path, "build.zip"), this.Path);
            System.IO.File.Delete(System.IO.Path.Combine(this.Path, "build.zip"));
        }
        private void Install_Register()
        {
            if (RegisterAsProgram)
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
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error has occurred! Error: {0}", e.Message);
                }

                string shortcutPath = System.IO.Path.Combine(Path, "Minecraft Bedrock Launcher.lnk");

                WshShell wshShell = new WshShell();
                IWshShortcut Shortcut = (IWshShortcut)wshShell.CreateShortcut(shortcutPath);
                Shortcut.TargetPath = System.IO.Path.Combine(Path, "BedrockLauncher.exe");
                Shortcut.WorkingDirectory = Path;
                Shortcut.Save();
                Console.WriteLine("shortcut created");

            }
        }
        private void Install_CreateShortcuts()
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
        private void Install_Finish()
        {
            if (this.RunOnExit) LaunchApp();
            Application.Current.Shutdown();
        }
        private void Install_FinishSilent()
        {
            LaunchApp();
            Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
        }

        private void Install_Cancel(bool UndoChanges = true)
        {
            Thread.Sleep(4000);
            while (!DownloadTask.IsCompleted || !DownloadTask.IsCanceled) Thread.Sleep(4000);

            if (UndoChanges)
            {
                if (Silent) Install_Restore(TempFolder, Path);
                else Install_Wipe(Path);
            }
            Environment.Exit(0);
        }
        private void Install_Wipe(string folder)
        {
            Directory.Delete(folder, true);
        }
        private void Install_Restore(string from, string to)
        {
            var dir = Directory.CreateDirectory(to);
            EmptyDir(dir, "data");
            Directory.CreateDirectory(to);
            BackupDir(from, to, "data");
        }

        #endregion

        #region File Management


        private void BackupDir(string from, string to, string ignoreFolder = "")
        {
            foreach (var file in new DirectoryInfo(from).GetFiles())
            {
                MoveFile(file, from, to);
            }
            foreach (var folder in new DirectoryInfo(from).GetDirectories())
            {
                if (folder.Name != "data") MoveDirectory(folder, from, to);
            }
        }
        private void EmptyDir(DirectoryInfo directory, string ignoreFolder = "")
        {
            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo subdir in directory.GetDirectories())
            {
                if (subdir.Name != ignoreFolder) subdir.Delete(true);
            }
        }
        private void MoveDirectory(DirectoryInfo directory, string _from, string _to)
        {
            if (!System.IO.Directory.Exists($@"{_to}\{directory.Name}")) directory.CopyTo(Directory.CreateDirectory($@"{_to}\{directory.Name}"));
            if (directory.Exists) directory.Delete(true);
        }
        private void MoveFile(FileInfo file, string _from, string _to)
        {
            if (!System.IO.File.Exists($@"{_to}\{file.Name}")) file.CopyTo($@"{_to}\{file.Name}");
            if (file.Exists) file.Delete();
        }

        #endregion

        #region Events

        private void OnWindowClosed(object sender, EventArgs e)
        {
            CancelInstall();
        }

        #endregion

        #region Actions

        private string GetResourceString(string resourceName)
        {
            return Application.Current.FindResource(resourceName) as string;
        }

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
        public void UpdateProgressBar(string content, long value, long max, bool IsIndeterminate)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.ProgressPage.progressBarContent.Text = content;
                this.ProgressPage.progressBar.Value = value;
                this.ProgressPage.progressBar.Maximum = max;
                this.ProgressPage.progressBar.IsIndeterminate = IsIndeterminate;
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
        public void UpdateUI(UpdateParam param)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (param == UpdateParam.InstallStart)
                {
                    ((MainWindow)Application.Current.MainWindow).Closed += OnWindowClosed;
                    if (Silent)
                    {
                        ((MainWindow)Application.Current.MainWindow).Show();
                        ProgressPage.launchOnExitCheckbox.IsEnabled = false;
                        ProgressPage.launchOnExitCheckbox.Visibility = Visibility.Hidden;
                    }
                    ((MainWindow)Application.Current.MainWindow).NextBtn.IsEnabled = false;
                    ((MainWindow)Application.Current.MainWindow).BackBtn.IsEnabled = false;
                    ((MainWindow)Application.Current.MainWindow).MainFrame.Navigate(this.ProgressPage);
                    UpdateProgressBar(GetResourceString("Installer_ProgressBar_Initalizing"), 0, 0, true);
                }
                else if (param == UpdateParam.InstallComplete)
                {

                    ((MainWindow)Application.Current.MainWindow).Closed -= OnWindowClosed;
                    if (Silent) ((MainWindow)Application.Current.MainWindow).Hide();
                    else
                    {
                        ((MainWindow)Application.Current.MainWindow).CancelBtn.Content = "Finish";
                        ((MainWindow)Application.Current.MainWindow).NextBtn.Visibility = Visibility.Hidden;
                        ((MainWindow)Application.Current.MainWindow).BackBtn.Visibility = Visibility.Hidden;
                        ((MainWindow)Application.Current.MainWindow).CancelBtn.Visibility = Visibility.Hidden;
                        ((MainWindow)Application.Current.MainWindow).FinishBtn.Visibility = Visibility.Visible;
                        ((MainWindow)Application.Current.MainWindow).FinishBtn.Click += (sender, e) => Install_Finish();

                        this.ProgressPage.InstallPanel.Visibility = Visibility.Collapsed;
                        UpdateProgressBar(GetResourceString("Installer_ProgressBar_Finalizing"), 0, 0, false);
                    }
                }
            });

        }

        #endregion

    }
}
