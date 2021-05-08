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
using BedrockLauncher.Components;

namespace BedrockLauncher.Classes
{
    public class VersionStateChangeInfo : NotifyPropertyChangedBase
    {

        public enum StateChange
        {
            None,
            isInitializing,
            isExtracting,
            isUninstalling,
            isLaunching,
            isDownloading,
            isBackingUp,
            isRemovingPackage,
            isRegisteringPackage
        }

        private const long _deploymentTotal = 100;

        private StateChange _currentState = StateChange.None;

        private long _CurrentProgress;
        private long _TotalProgress;




        public string DeploymentPackageName { get; set; }

        public StateChange CurrentState
        {
            get { return _currentState; }
            set
            {
                _currentState = value;
                UpdateProgressBarContent(value);
                ProgressBarIsIndeterminate(IsProgressIndeterminate);
                ProgressBarUpdate();
                OnPropertyChanged("IsProgressIndeterminate");
                OnPropertyChanged("DisplayStatus");
            }
        }

        private void UpdateProgressBarContent(StateChange value)
        {
            Application.Current.Dispatcher.Invoke(() => {
                switch (value)
                {
                    case StateChange.isInitializing:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Downloading");
                        break;
                    case StateChange.isDownloading:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Downloading");
                        break;
                    case StateChange.isExtracting:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Extracting");
                        break;
                    case StateChange.isRegisteringPackage:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_RegisteringPackage");
                        break;
                    case StateChange.isRemovingPackage:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_RemovingPackage");
                        break;
                    case StateChange.isUninstalling:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Uninstalling");
                        break;
                    case StateChange.isLaunching:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_Launching");
                        break;
                    case StateChange.isBackingUp:
                        ConfigManager.MainThread.progressbarcontent.SetResourceReference(TextBlock.TextProperty, "ProgressBar_BackingUp");
                        break;
                }

            });
        }

        public bool IsProgressIndeterminate
        {
            get 
            {
                return CurrentState == StateChange.isInitializing || CurrentState == StateChange.isUninstalling || CurrentState == StateChange.isLaunching;
            }
        }

        public long CurrentProgress
        {
            get { return _CurrentProgress; }
            set
            {
                _CurrentProgress = value;
                ProgressBarUpdate(_CurrentProgress, _TotalProgress);
                OnPropertyChanged(nameof(CurrentProgress));
                OnPropertyChanged(nameof(DisplayStatus));
            }
        }

        public long TotalProgress
        {
            get { return _TotalProgress; }
            set { _TotalProgress = value; OnPropertyChanged(nameof(TotalProgress)); OnPropertyChanged(nameof(DisplayStatus)); }
        }

        public string DisplayStatus
        {
            get
            {
                switch (CurrentState)
                {
                    case StateChange.isInitializing:
                        return "";
                    case StateChange.isDownloading:
                        return DownloadStatus;
                    case StateChange.isExtracting:
                        return ExtractingStatus;
                    case StateChange.isRegisteringPackage:
                        return DeploymentStatus;
                    case StateChange.isRemovingPackage:
                        return DeploymentStatus;
                    case StateChange.isUninstalling:
                        return "";
                    case StateChange.isLaunching:
                        return "";
                    case StateChange.isBackingUp:
                        return BackupStatus;
                    case StateChange.None:
                        return "";
                    default:
                        return "";
                }
            }
        }

        private string BackupStatus
        {
            get
            {
                return string.Format("{0} / {1}", CurrentProgress, TotalProgress);
            }
        }

        private string DeploymentStatus
        {
            get
            {
                return CurrentProgress + "%" + string.Format(" [ {0} ]", DeploymentPackageName);
            }
        }

        private string ExtractingStatus
        {
            get
            {
                long percent = 0;
                if (TotalProgress != 0) percent = (100 * CurrentProgress) / TotalProgress;
                return percent + "%";
            }
        }

        private string DownloadStatus
        {
            get 
            {
                return (Math.Round((double)CurrentProgress / 1024 / 1024, 2)).ToString() + " MB / " + (Math.Round((double)TotalProgress / 1024 / 1024, 2)).ToString() + " MB";
            }
        }

        public void ProgressBarIsIndeterminate(bool isIndeterminate)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ConfigManager.MainThread != null)
                {
                    ConfigManager.MainThread.progressSizeHack.IsIndeterminate = isIndeterminate;
                    ConfigManager.MainThread.BedrockEditionButton.progressSizeHack.IsIndeterminate = isIndeterminate;
                }
            });
        }

        public void ProgressBarUpdate()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ConfigManager.MainThread != null)
                {
                    ConfigManager.MainThread.ProgressBarText.Text = DisplayStatus;
                }
            });
        }

        public void ProgressBarUpdate(double downloadedBytes, double totalSize)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ConfigManager.MainThread != null)
                {
                    ConfigManager.MainThread.progressSizeHack.Value = downloadedBytes;
                    ConfigManager.MainThread.progressSizeHack.Maximum = totalSize;

                    ConfigManager.MainThread.BedrockEditionButton.progressSizeHack.Value = downloadedBytes;
                    ConfigManager.MainThread.BedrockEditionButton.progressSizeHack.Maximum = totalSize;

                    ConfigManager.MainThread.ProgressBarText.Text = DisplayStatus;
                }
            });
        }

        public ICommand CancelCommand { get; set; }
    }
}
