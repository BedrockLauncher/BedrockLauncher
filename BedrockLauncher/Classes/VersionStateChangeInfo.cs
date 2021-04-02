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
using BedrockLauncher.Core;

namespace BedrockLauncher.Classes
{
    public class VersionStateChangeInfo : NotifyPropertyChangedBase
    {

        private bool _isInitializing;
        private bool _isExtracting;
        private bool _isUninstalling;
        private bool _isLaunching;
        public long _downloadedBytes;
        private long _totalSize;

        public bool IsInitializing
        {
            get { return _isInitializing; }
            set { _isInitializing = value; Application.Current.Dispatcher.Invoke(() => { ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "MainPage_ProgressBarDownloading"); }); ProgressBarIsIndeterminate(_isInitializing); OnPropertyChanged("IsProgressIndeterminate"); OnPropertyChanged("DisplayStatus"); }
        }

        public bool IsExtracting
        {
            get { return _isExtracting; }
            set { _isExtracting = value; Application.Current.Dispatcher.Invoke(() => { ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "MainPage_ProgressBarExtracting"); }); Application.Current.Dispatcher.Invoke(() => { ConfigManager.MainThread.ProgressBarText.Text = ""; }); ProgressBarIsIndeterminate(_isExtracting); OnPropertyChanged("IsProgressIndeterminate"); OnPropertyChanged("DisplayStatus"); }
        }

        public bool IsUninstalling
        {
            get { return _isUninstalling; }
            set { _isUninstalling = value; Application.Current.Dispatcher.Invoke(() => { ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "MainPage_ProgressBarUninstalling"); }); Application.Current.Dispatcher.Invoke(() => { ConfigManager.MainThread.ProgressBarGrid.Visibility = Visibility.Visible; }); ProgressBarIsIndeterminate(_isUninstalling); OnPropertyChanged("IsProgressIndeterminate"); OnPropertyChanged("DisplayStatus"); }
        }

        public bool IsLaunching
        {
            get { return _isLaunching; }
            set { _isLaunching = value; Application.Current.Dispatcher.Invoke(() => { ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "MainPage_ProgressBarLaunching"); }); Application.Current.Dispatcher.Invoke(() => { ConfigManager.MainThread.ProgressBarGrid.Visibility = Visibility.Visible; }); ProgressBarIsIndeterminate(_isLaunching); OnPropertyChanged("IsProgressIndeterminate"); OnPropertyChanged("DisplayStatus"); }
        }

        public bool IsProgressIndeterminate
        {
            get { return IsInitializing || IsExtracting || IsUninstalling || IsLaunching; }
        }

        public long DownloadedBytes
        {
            get { return _downloadedBytes; }
            set { _downloadedBytes = value; ProgressBarUpdate(_downloadedBytes, _totalSize); OnPropertyChanged("DownloadedBytes"); OnPropertyChanged("DisplayStatus"); }
        }

        public long TotalSize
        {
            get { return _totalSize; }
            set { _totalSize = value; OnPropertyChanged("TotalSize"); OnPropertyChanged("DisplayStatus"); }
        }

        public string DisplayStatus
        {
            get
            {
                if (IsInitializing)
                    return "Downloading...";
                if (IsExtracting)
                    return "Extracting...";
                if (IsUninstalling)
                    return "Uninstalling...";
                if (IsLaunching)
                    return "Launching...";
                return (DownloadedBytes / 1024 / 1024) + "MiB/" + (TotalSize / 1024 / 1024) + "MiB";
            }
        }

        public ICommand CancelCommand { get; set; }

    }
}
