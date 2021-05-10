using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Events;
using BedrockLauncher.Classes;
using BedrockLauncher.Components;
using System.Windows;
using System.Windows.Input;
using BedrockLauncher.Methods;
using System.Windows.Controls;

namespace BedrockLauncher.ViewModels
{
    public class LauncherModel : NotifyPropertyChangedBase
    {
        #region Events

        public event EventHandler ProgressBarStateChanged;

        protected virtual void OnProgressBarStateChanged(ProgressBarState e)
        {
            EventHandler handler = ProgressBarStateChanged;
            if (handler != null) handler(this, e);
        }

        #endregion

        #region One Way Bindings
        public string PlayButtonString
        {
            get
            {         
                if (IsGameRunning) return App.Current.FindResource("GameTab_PlayButton_Kill_Text").ToString();
                else return App.Current.FindResource("GameTab_PlayButton_Text").ToString();
            }
        }
        public bool AllowEditing
        {
            get
            {
                return AllowPlaying && !_IsGameRunning && !ShowProgressBar;
            }
        }
        public bool AllowPlaying
        {
            get
            {
                return CurrentState == StateChange.None;
            }
        }

        #endregion

        #region Bindings

        private bool _IsGameRunning = false;
        private bool _ShowProgressBar = false;

        public bool IsGameRunning
        {
            get
            {
                return _IsGameRunning;
            }
            set
            {
                _IsGameRunning = value;
                OnPropertyChanged(nameof(IsGameRunning));
                UpdateUI();
            }
        }

        public bool ShowProgressBar
        {
            get
            {
                return _ShowProgressBar;
            }
            set
            {
                _ShowProgressBar = value;
                OnProgressBarStateChanged(new ProgressBarState(value));
                OnPropertyChanged(nameof(ShowProgressBar));
                UpdateUI();
            }
        }

        #endregion

        public void UpdateUI()
        {
            OnPropertyChanged(nameof(IsGameRunning));
            OnPropertyChanged(nameof(ShowProgressBar));
            OnPropertyChanged(nameof(PlayButtonString));
            OnPropertyChanged(nameof(AllowPlaying));
            OnPropertyChanged(nameof(AllowEditing));
        }

        #region VersionState

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
                OnPropertyChanged(nameof(IsProgressIndeterminate));
                OnPropertyChanged(nameof(DisplayStatus));
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
            get 
            { 
                return _CurrentProgress; 
            }
            set
            {
                _CurrentProgress = value;
                ProgressBarUpdate(_CurrentProgress, _TotalProgress);
                OnPropertyChanged(nameof(CurrentProgress));
                OnPropertyChanged(nameof(DisplayStatus));
                UpdateUI();
            }
        }

        public long TotalProgress
        {
            get 
            { 
                return _TotalProgress; 
            }
            set 
            { 
                _TotalProgress = value;
                OnPropertyChanged(nameof(TotalProgress));
                OnPropertyChanged(nameof(DisplayStatus));
                UpdateUI();
            }
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
                if (ConfigManager.MainThread != null) ConfigManager.MainThread.progressSizeHack.Value = downloadedBytes;
                if (ConfigManager.MainThread != null) ConfigManager.MainThread.progressSizeHack.Maximum = totalSize;

                if (ConfigManager.MainThread != null) ConfigManager.MainThread.BedrockEditionButton.progressSizeHack.Value = downloadedBytes;
                if (ConfigManager.MainThread != null) ConfigManager.MainThread.BedrockEditionButton.progressSizeHack.Maximum = totalSize;

                if (ConfigManager.MainThread != null) ConfigManager.MainThread.ProgressBarText.Text = DisplayStatus;
            });
        }

        public ICommand CancelCommand { get; set; }

        #endregion
    }
}
