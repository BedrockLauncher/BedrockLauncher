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
            ICancel.Token.Register(Install_Cancel);
            Task.Run(Install, ICancel.Token);
        }

        #endregion

        #region Install Steps

        private async void Install()
        {
            try
            {
                CanCancel = false;

                #region Validation

                UpdateUI(UpdateParam.InstallStart);

                try
                {
                    Directory.CreateDirectory(Path);
                }
                catch (Exception err)
                {
                    MessageBox.Show(GetResourceString("Installer_Dialog_ErrorOccured_Text") + "\n" + err.ToString());
                    Install_Cancel();
                    return;
                }

                Install_Validate(Path);

                Install_Backup(Path, TempFolder);

                #endregion

                CanCancel = true;

                #region Download Build

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

                #endregion

                CanCancel = false;

                #region Extract Build

                // unpack build archive
                ZipFile.ExtractToDirectory(System.IO.Path.Combine(this.Path, "build.zip"), this.Path);
                System.IO.File.Delete(System.IO.Path.Combine(this.Path, "build.zip"));

                #endregion

                #region Register Program

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
                        Console.WriteLine("An error has occured! Error: {0}", e.Message);
                    }

                    string shortcutPath = System.IO.Path.Combine(Path, "Minecraft Bedrock Launcher.lnk");

                    WshShell wshShell = new WshShell();
                    IWshShortcut Shortcut = (IWshShortcut)wshShell.CreateShortcut(shortcutPath);
                    Shortcut.TargetPath = System.IO.Path.Combine(Path, "BedrockLauncher.exe");
                    Shortcut.WorkingDirectory = Path;
                    Shortcut.Save();
                    Console.WriteLine("shortcut created");

                }

                #endregion

                #region Create Optional Shortcuts

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

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(GetResourceString("Installer_Dialog_ErrorOccured_Text") + "\n" + ex.ToString());
                Install_Cancel();
            }

            UpdateUI(UpdateParam.InstallComplete);
            if (Silent) Install_FinishSilent();
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
        private void Install_Validate(string path)
        {
            List<string> allfiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToList();
            foreach (var file in allfiles) CheckFileLock(file);
        }
        private void Install_Cancel()
        {
            Thread.Sleep(4000);
            while (!DownloadTask.IsCompleted || !DownloadTask.IsCanceled) Thread.Sleep(4000);

            if (Silent) Install_Restore(TempFolder, Path);
            else Directory.Delete(Path, true);
            Environment.Exit(0);
        }
        private void Install_Backup(string from, string to)
        {
            var dir = Directory.CreateDirectory(to);
            EmptyDir(dir);
            Directory.CreateDirectory(to);

            foreach (var file in new DirectoryInfo(from).GetFiles()) MoveFile(file, from, to);
            foreach (var folder in new DirectoryInfo(from).GetDirectories()) if ((folder.Name != "data")) MoveDirectory(folder, from, to);
        }
        private void Install_Restore(string from, string to)
        {
            var dir = Directory.CreateDirectory(to);
            EmptyDir(dir, "data");
            Directory.CreateDirectory(to);

            foreach (var file in new DirectoryInfo(from).GetFiles()) MoveFile(file, from, to);
            foreach (var folder in new DirectoryInfo(from).GetDirectories()) MoveDirectory(folder, from, to);
        }

        #endregion

        #region File Management

        private void EmptyDir(DirectoryInfo directory, string ignoreFolder = "")
        {
            foreach (FileInfo file in directory.GetFiles()) DeleteFile(file);
            foreach (DirectoryInfo subdir in directory.GetDirectories()) DeleteDirectory(subdir);

            void DeleteFile(FileInfo file, bool hasRetried = false)
            {
                try
                {
                    file.Delete();
                }
                catch (Exception ex)
                {
                    if (!hasRetried)
                    {
                        Task.Delay(5000);
                        DeleteFile(file, true);
                    }
                    else
                    {
                        MessageBoxResult result = MessageBox.Show(ex.Message + Environment.NewLine + GetResourceString("Installer_Dialog_TryAgain_Text"), "", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes) DeleteFile(file);
                    }
                }
            }
            void DeleteDirectory(DirectoryInfo _dir, bool hasRetried = false)
            {
                try
                {
                    if (_dir.Name != ignoreFolder) _dir.Delete(true);
                }
                catch (Exception ex)
                {

                    if (!hasRetried)
                    {
                        Task.Delay(5000);
                        DeleteDirectory(_dir, true);
                    }
                    else
                    {
                        MessageBoxResult result = MessageBox.Show(ex.Message + Environment.NewLine + GetResourceString("Installer_Dialog_TryAgain_Text"), "", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes) DeleteDirectory(_dir);
                    }
                }
            }
        }
        private void MoveDirectory(DirectoryInfo directory, string _from, string _to, bool hasRetried = false)
        {
            try
            {
                if (!System.IO.Directory.Exists($@"{_to}\{directory.Name}")) directory.CopyTo(Directory.CreateDirectory($@"{_to}\{directory.Name}"));
                if (directory.Exists) directory.Delete(true);
            }
            catch (Exception ex)
            {
                if (!hasRetried)
                {
                    Task.Delay(5000);
                    MoveDirectory(directory, _from, _to, true);
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show(ex.Message + Environment.NewLine + GetResourceString("Installer_Dialog_TryAgain_Text"), GetResourceString("Installer_Dialog_MovingDirectoryFailed_Text"), MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Yes) MoveDirectory(directory, _from, _to);
                    else if (result == MessageBoxResult.Cancel) Install_Cancel();
                }
            }
        }
        private void MoveFile(FileInfo file, string _from, string _to, bool hasRetried = false)
        {
            try
            {
                if (!System.IO.File.Exists($@"{_to}\{file.Name}")) file.CopyTo($@"{_to}\{file.Name}");
                if (file.Exists) file.Delete();
            }
            catch (Exception ex)
            {
                if (!hasRetried)
                {
                    Task.Delay(5000);
                    MoveFile(file, _from, _to, true);
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show(ex.Message + Environment.NewLine + GetResourceString("Installer_Dialog_TryAgain_Text"), GetResourceString("Installer_Dialog_MovingFileFailed_Text"), MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Yes) MoveFile(file, _from, _to);
                    else if (result == MessageBoxResult.Cancel) Install_Cancel();
                }
            }
        }
        private void CheckFileLock(string file, bool hasRetried = false)
        {
            if (FileExtentions.IsFileInUse(file, false))
            {
                if (!hasRetried)
                {
                    Task.Delay(5000);
                    CheckFileLock(file, true);
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("Some of the files are currently in use. " + GetResourceString("Installer_Dialog_TryAgain_Text"), GetResourceString("Installer_Dialog_FileLocked_Text"), MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Yes) CheckFileLock(file);
                    else if (result == MessageBoxResult.Cancel) Install_Cancel();
                }
            }
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
