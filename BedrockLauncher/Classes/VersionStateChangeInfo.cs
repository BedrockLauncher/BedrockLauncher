using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms.Design;
using Newtonsoft.Json;

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Data;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Core;
using Windows.Management.Deployment;
using Windows.System;
using BedrockLauncher.Methods;

namespace BedrockLauncher.Classes
{
    public class VersionStateChangeInfo : NotifyPropertyChangedBase
    {

        private const long _deploymentTotal = 100;

        private bool _isInitializing;
        private bool _isExtracting;
        private bool _isUninstalling;
        private bool _isLaunching;
        private bool _isDownloading;


        private bool _isRemovingPackage;
        private bool _isRegisteringPackage;

        public long _downloadedBytes_downloading;
        private long _totalSize_downloading;

        private long _deploymentProgress;

        private int _extractedBytes_extracting;
        private int _totalBytes_extracting;


        public string DeploymentPackageName { get; set; }

        public bool IsInitializing
        {
            get { return _isInitializing; }
            set { 
                _isInitializing = value; 
                Application.Current.Dispatcher.Invoke(() => { 
                    ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Downloading");
                });
                ProgressBarIsIndeterminate(IsProgressIndeterminate);
                ProgressBarUpdate();
                OnPropertyChanged("IsProgressIndeterminate"); 
                OnPropertyChanged("DisplayStatus"); 
            }
        }

        public bool IsDownloading
        {
            get { return _isDownloading; }
            set
            {
                _isDownloading = value;
                Application.Current.Dispatcher.Invoke(() => {
                    ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Downloading");
                });
                ProgressBarIsIndeterminate(IsProgressIndeterminate);
                ProgressBarUpdate();
                OnPropertyChanged("IsProgressIndeterminate");
                OnPropertyChanged("DisplayStatus");
            }
        }

        public bool IsExtracting
        {
            get { return _isExtracting; }
            set
            {
                _isExtracting = value;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Extracting");
                });
                ProgressBarIsIndeterminate(IsProgressIndeterminate);
                ProgressBarUpdate();
                OnPropertyChanged("IsProgressIndeterminate");
                OnPropertyChanged("DisplayStatus");
            }
        }

        public bool IsRegisteringPackage
        {
            get { return _isRegisteringPackage; }
            set
            {
                _isRegisteringPackage = value;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_RegisteringPackage");
                });
                ProgressBarIsIndeterminate(IsProgressIndeterminate);
                ProgressBarUpdate();
                OnPropertyChanged("IsProgressIndeterminate");
                OnPropertyChanged("DisplayStatus");

                if (value == false) DeploymentProgress = 0;
            }
        }

        public bool IsRemovingPackage
        {
            get { return _isRemovingPackage; }
            set
            {
                _isRemovingPackage = value;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_RemovingPackage");
                });
                ProgressBarIsIndeterminate(IsProgressIndeterminate);
                ProgressBarUpdate();
                OnPropertyChanged("IsProgressIndeterminate");
                OnPropertyChanged("DisplayStatus");

                if (value == false) DeploymentProgress = 0;
            }
        }

        public bool IsUninstalling
        {
            get { return _isUninstalling; }
            set { 
                _isUninstalling = value; 
                Application.Current.Dispatcher.Invoke(() => 
                { 
                    ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Uninstalling");
                });
                ProgressBarIsIndeterminate(IsProgressIndeterminate);
                ProgressBarUpdate();
                OnPropertyChanged("IsProgressIndeterminate");
                OnPropertyChanged("DisplayStatus"); }
        }

        public bool IsLaunching
        {
            get { return _isLaunching; }
            set { 
                _isLaunching = value; 
                Application.Current.Dispatcher.Invoke(() => 
                { 
                    ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Launching");
                });
                ProgressBarIsIndeterminate(IsProgressIndeterminate);
                ProgressBarUpdate();
                OnPropertyChanged("IsProgressIndeterminate"); 
                OnPropertyChanged("DisplayStatus");
            }
        }

        public bool IsProgressIndeterminate
        {
            get 
            { 
                return IsInitializing || IsUninstalling || IsLaunching; 
            }
        }

        public long DownloadedBytes_Downloading
        {
            get { return _downloadedBytes_downloading; }
            set
            {
                _downloadedBytes_downloading = value;
                ProgressBarUpdate(_downloadedBytes_downloading, _totalSize_downloading);
                OnPropertyChanged("DownloadedBytes_Downloading");
                OnPropertyChanged("DisplayStatus");
            }
        }

        public long TotalSize_Downloading
        {
            get { return _totalSize_downloading; }
            set { _totalSize_downloading = value; OnPropertyChanged("TotalSize_Downloading"); OnPropertyChanged("DisplayStatus"); }
        }

        public long DeploymentProgress
        {
            get { return _deploymentProgress; }
            set
            {
                _deploymentProgress = value;
                ProgressBarUpdate(_deploymentProgress, _deploymentTotal);
                OnPropertyChanged("DeploymentProgress");
                OnPropertyChanged("DisplayStatus");
            }
        }

        public int ExtractedBytes_Extracting
        {
            get { return _extractedBytes_extracting; }
            set
            {
                _extractedBytes_extracting = value;
                ProgressBarUpdate(_extractedBytes_extracting, _totalBytes_extracting);
                OnPropertyChanged("ExtractedBytes_Extracting");
                OnPropertyChanged("DisplayStatus");
            }
        }

        public int TotalBytes_Extracting
        {
            get { return _totalBytes_extracting; }
            set { _totalBytes_extracting = value; OnPropertyChanged("TotalBytes_Extracting"); OnPropertyChanged("DisplayStatus"); }
        }

        public string DisplayStatus
        {
            get
            {

                if (IsInitializing) return "";
                else if (IsExtracting) return ExtractingStatus;
                else if (IsUninstalling) return "";
                else if (IsLaunching) return "";
                else if (IsRegisteringPackage) return DeploymentStatus;
                else if (IsRemovingPackage) return DeploymentStatus;
                else if (IsDownloading) return DownloadStatus;
                else return "";
            }
        }

        private string DeploymentStatus
        {
            get
            {
                return DeploymentProgress + "%" + string.Format(" [ {0} ]", DeploymentPackageName);
            }
        }

        private string ExtractingStatus
        {
            get
            {
                int percent = 0;
                if (TotalBytes_Extracting != 0) percent = (100 * ExtractedBytes_Extracting) / TotalBytes_Extracting;
                return percent + "%";
            }
        }

        private string DownloadStatus
        {
            get 
            {
                return (Math.Round((double)DownloadedBytes_Downloading / 1024 / 1024, 2)).ToString() + " MB / " + (Math.Round((double)TotalSize_Downloading / 1024 / 1024, 2)).ToString() + " MB";
            }
        }

        public void ProgressBarIsIndeterminate(bool isIndeterminate)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ConfigManager.MainThread.progressSizeHack.IsIndeterminate = isIndeterminate;
            });
        }

        public void ProgressBarUpdate()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ConfigManager.MainThread.ProgressBarText.Text = DisplayStatus;
            });
        }

        public void ProgressBarUpdate(double downloadedBytes, double totalSize)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ConfigManager.MainThread.progressSizeHack.Value = downloadedBytes;
                ConfigManager.MainThread.progressSizeHack.Maximum = totalSize;
                ConfigManager.MainThread.ProgressBarText.Text = DisplayStatus;
            });
        }

        public ICommand CancelCommand { get; set; }
    }
}
