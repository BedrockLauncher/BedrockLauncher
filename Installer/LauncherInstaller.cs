using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Win32;
using IWshRuntimeLibrary;
using System.Xml;

namespace Installer
{
    class LauncherInstaller
    {
        private const string LATEST_BUILD_LINK = "https://github.com/XlynxX/BedrockLauncher/releases/latest/download/build_v0.1.1.zip";
        private string path;
        private string build_version;
        private InstallationProgressPage InstallationProgressPage;

        public LauncherInstaller(string installationPath, InstallationProgressPage installationProgressPage)
        {
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
            foreach (string file in Directory.GetFiles(path)) 
            {
                System.IO.File.Delete(file);
            }
            Directory.CreateDirectory(path);
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
                ((MainWindow)Application.Current.MainWindow).FinishBtn.Visibility = Visibility.Visible;
                ((MainWindow)Application.Current.MainWindow).FinishBtn.Click += FinishInstall;
                ((MainWindow)Application.Current.MainWindow).CancelBtn.Visibility = Visibility.Hidden;
            }
        }
        bool getBuildVersion()
        {
            foreach (string file in Directory.GetFiles(path)) { Console.WriteLine(file); }
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(Path.Combine(path, "BedrockLauncher.exe.config"));
            XmlNodeList nodeList = xmldoc.GetElementsByTagName("setting");
            Console.WriteLine("build version: " + nodeList);
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
            if ((bool)InstallationProgressPage.desktopIconCheckBox.IsChecked) 
            {
                Console.WriteLine("added to desktop");
                System.IO.File.Copy(Path.Combine(path, "Minecraft Bedrock Launcher.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Minecraft Bedrock Launcher.lnk"), true); 
            }

            // add to start menu if needed
            if ((bool)InstallationProgressPage.startMenuCheckBox.IsChecked)
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
