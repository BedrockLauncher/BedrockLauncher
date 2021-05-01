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

namespace Installer
{
    class LauncherInstaller
    {
        private string LATEST_BUILD_LINK { get => BL_Core.Properties.Settings.Default.GithubPage + "/releases/latest/download/build.zip"; }
        private string path;
        private string build_version;
        private bool silent;
        private InstallationProgressPage InstallationProgressPage;

        public static bool MakeDesktopIcon { get; set; } = false;
        public static bool MakeStartMenuIcon { get; set; } = false;

        public LauncherInstaller(string installationPath, InstallationProgressPage installationProgressPage, bool silent = false)
        {
            this.silent = silent;
            this.InstallationProgressPage = installationProgressPage;
            this.path = installationPath;

            if (!Directory.Exists(installationPath))
            {
                try
                {
                    Directory.CreateDirectory(installationPath);
                    download();
                    return;
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.ToString());
                    return;
                }
            }
            cleanCurrent(installationPath);
            download();

        }

        // this will delete old version if exists
        private void cleanCurrent(string path)
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
            if (System.IO.File.Exists(Path.Combine(path, "Installer.exe.old")))
            {
                System.IO.File.Delete(Path.Combine(path, "Installer.exe.old"));
            }

            foreach (string file in Directory.GetFiles(path)) 
            {
                // renaming currently running installer to replace with new later
                if (file.EndsWith("Installer.exe"))
                {
                    System.IO.File.Move(file, Path.Combine(path, "Installer.exe.old"));
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
        // will download latest build and start installation upon downloaded
        private void download()
        {
            ((MainWindow)Application.Current.MainWindow).NextBtn.IsEnabled = false;
            ((MainWindow)Application.Current.MainWindow).BackBtn.IsEnabled = false;
            ((MainWindow)Application.Current.MainWindow).MainFrame.Navigate(this.InstallationProgressPage);

            // actually start downloading
            using (WebClient wc = new WebClient())
            {
                wc.DownloadFileAsync(new Uri(LATEST_BUILD_LINK), Path.Combine(this.path, "build.zip"));
                wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                wc.DownloadFileCompleted += ExtractAndInstall;
            }
        }

        // Event to track the progress
        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.InstallationProgressPage.progressBar.Value = e.ProgressPercentage;
        }

        void ExtractAndInstall(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
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

            // return buttons to life
            ((MainWindow)Application.Current.MainWindow).CancelBtn.Content = "Finish";
            ((MainWindow)Application.Current.MainWindow).NextBtn.Visibility = Visibility.Hidden;
            ((MainWindow)Application.Current.MainWindow).BackBtn.Visibility = Visibility.Hidden;

            // unpack build archive
            ZipFile.ExtractToDirectory(Path.Combine(this.path, "build.zip"), this.path);
            System.IO.File.Delete(Path.Combine(this.path, "build.zip"));

            // async add to registry
            RunAsyncTasks();
        }

        private async void RunAsyncTasks()
        {
            await Task.Run(getBuildVersion);
            if (await Task.Run(RegisterApp))
            {
                CreateShortcut();
                if (this.silent)
                {
                    Process.Start(new ProcessStartInfo(Path.Combine(path, "BedrockLauncher.exe")));
                    Application.Current.Shutdown();
                }
                ((MainWindow)Application.Current.MainWindow).FinishBtn.Visibility = Visibility.Visible;
                ((MainWindow)Application.Current.MainWindow).FinishBtn.Click += FinishInstall;
                ((MainWindow)Application.Current.MainWindow).CancelBtn.Visibility = Visibility.Hidden;
            }
        }
        bool getBuildVersion()
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(Path.Combine(path, "BedrockLauncher.exe.config"));
            XmlNodeList nodeList = xmldoc.GetElementsByTagName("setting");
            foreach (XmlNode node in nodeList)
            {
                if (node.OuterXml.Contains("<setting name=\"Version\" serializeAs=\"String\">"))
                {
                    Console.WriteLine("build version: " + node.InnerText);
                    this.build_version = node.InnerText;
                }
            }
            return true;
        }
        bool RegisterApp()
        {
            try
            {
                // Определяем ветку реестра, в которую будем вносить изменения
                string registryLocation = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
                // Открываем указанный подраздел в разделе реестра HKEY_LOCAL_MACHINE для записи
                RegistryKey regKey = (Registry.LocalMachine).OpenSubKey(registryLocation, true);
                // Создаём новый вложенный раздел с информацией по нашей программе
                RegistryKey progKey = regKey.CreateSubKey("Minecraft Bedrock Launcher");
                // Отображаемое имя
                progKey.SetValue("DisplayName", "Minecraft Bedrock Launcher", RegistryValueKind.String);
                // Папка с файлами
                progKey.SetValue("InstallLocation", path, RegistryValueKind.ExpandString);
                // Иконка
                progKey.SetValue("DisplayIcon", Path.Combine(path, "BedrockLauncher.exe"), RegistryValueKind.String);
                // Строка удаления
                progKey.SetValue("UninstallString", Path.Combine(path, "Uninstaller.exe"), RegistryValueKind.ExpandString);
                // Отображаемая версия
                progKey.SetValue("DisplayVersion", build_version, RegistryValueKind.String);
                // Издатель
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

        private void FinishInstall(object sender, RoutedEventArgs e)
        {
            // create desktop shortcut if needed
            if ((bool)MakeDesktopIcon) 
            {
                Console.WriteLine("added to desktop");
                System.IO.File.Copy(Path.Combine(path, "Minecraft Bedrock Launcher.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Minecraft Bedrock Launcher.lnk"), true); 
            }

            // add to start menu if needed
            if ((bool)MakeStartMenuIcon)
            {
                Console.WriteLine("added to start menu");
                string commonStartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
                string appStartMenuPath = Path.Combine(commonStartMenuPath, "Programs", "Minecraft Launcher");
                if (!Directory.Exists(appStartMenuPath)) { Directory.CreateDirectory(appStartMenuPath); }
                System.IO.File.Copy(Path.Combine(path, "Minecraft Bedrock Launcher.lnk"), Path.Combine(appStartMenuPath, "Minecraft Bedrock Launcher.lnk"), true);
            }

            Application.Current.Shutdown();
        }
        private void CreateShortcut()
        {
            string shortcutPath = Path.Combine(path, "Minecraft Bedrock Launcher.lnk");

            WshShell wshShell = new WshShell(); //создаем объект wsh shell
            IWshShortcut Shortcut = (IWshShortcut) wshShell.CreateShortcut(shortcutPath);
            Shortcut.TargetPath = Path.Combine(path, "BedrockLauncher.exe"); //путь к целевому файлу
            Shortcut.WorkingDirectory = path;
            Shortcut.Save();
            Console.WriteLine("shortcut created");
        }
    }
}
