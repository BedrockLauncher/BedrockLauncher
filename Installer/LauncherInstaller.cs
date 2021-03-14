using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Installer
{
    class LauncherInstaller
    {
        private const string LATEST_BUILD_LINK = "https://github.com/XlynxX/BedrockLauncher/releases/latest/download/build.zip";
        private string path;
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
            download();

        }

        // will download latest build and start installation upon downloaded
        private void download() 
        {
            ((MainWindow)Application.Current.MainWindow).NextBtn.Content = "Finish";
            ((MainWindow)Application.Current.MainWindow).NextBtn.IsEnabled = false ;
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
            File.Delete(Path.Combine(this.path, "build.zip"));
        }
    }
}
