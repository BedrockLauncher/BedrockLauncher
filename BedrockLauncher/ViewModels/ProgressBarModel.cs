using BedrockLauncher.Classes;
using BedrockLauncher.Enums;
using JemExtensions;
using PostSharp.Patterns.Model;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using S = JemExtensions.SpecialExtensions;

namespace BedrockLauncher.ViewModels
{
    public class ProgressBarModel : INotifyPropertyChanged
    {
        #region Init

        public ProgressBarModel()
        {
            PropertyChanged += ProgressBarModel_PropertyChanged;
        }

        private void ProgressBarModel_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Show))
                GetProgressBarAnim();
        }

        #endregion

        #region Fields

        private ICommand _cancelCommand;
        private bool _allowCancel;
        private bool _isGameRunning;
        private bool _playButtonLanguageChanged;
        private bool _show;
        private LauncherState _currentState;
        private long _currentProgress;
        private long _actualCurrentProgress;
        private long _actualTotalProgress;
        private bool _isIndeterminate = true;
        private Visibility _animMiniVisibility =
            Visibility.Collapsed;
        private Visibility _animVisibility =
            Visibility.Collapsed;
        private Visibility _animTextVisibility =
            Visibility.Collapsed;
        private string _information;

        #endregion

        #region Various Properties

        public ICommand CancelCommand
        {
            get => _cancelCommand;
            set
            {
                if (_cancelCommand == value)
                    return;

                _cancelCommand = value;
                OnPropertyChanged(nameof(CancelCommand));
            }
        }

        public bool AllowCancel
        {
            get => _allowCancel;
            set
            {
                if (_allowCancel == value)
                    return;

                _allowCancel = value;
                OnPropertyChanged(nameof(AllowCancel));
            }
        }

        public bool IsGameRunning
        {
            get => _isGameRunning;
            set
            {
                if (_isGameRunning == value)
                    return;

                _isGameRunning = value;

                OnPropertyChanged(nameof(IsGameRunning));
                OnPropertyChanged(nameof(PlayButtonString));
                OnPropertyChanged(nameof(PlayEditorButtonString));
                OnPropertyChanged(nameof(AllowEditing));
            }
        }

        public bool PlayButtonLanguageChanged
        {
            get => _playButtonLanguageChanged;
            set
            {
                if (_playButtonLanguageChanged == value)
                    return;

                _playButtonLanguageChanged = value;

                OnPropertyChanged(
                    nameof(PlayButtonLanguageChanged));

                OnPropertyChanged(
                    nameof(PlayButtonString));

                OnPropertyChanged(
                    nameof(PlayEditorButtonString));
            }
        }

        public string PlayButtonString
        {
            get
            {
                Depends.On(
                    IsGameRunning,
                    PlayButtonLanguageChanged);

                if (Application.Current == null)
                    return string.Empty;

                if (IsGameRunning)
                {
                    return Application.Current
                        .TryFindResource(
                            "GameTab_PlayButton_Kill_Text")
                        ?.ToString()
                        ?? string.Empty;
                }

                return Application.Current
                    .TryFindResource(
                        "InstallationsPage_PlayButton")
                    ?.ToString()
                    ?? string.Empty;
            }
        }

        public string PlayEditorButtonString
        {
            get
            {
                Depends.On(
                    IsGameRunning,
                    PlayButtonLanguageChanged);

                if (Application.Current == null)
                    return string.Empty;

                if (IsGameRunning)
                {
                    return Application.Current
                        .TryFindResource(
                            "GameTab_PlayButton_Kill_Text")
                        ?.ToString()
                        ?? string.Empty;
                }

                return Application.Current
                    .TryFindResource(
                        "CreatorToolsPage_PlayEditorButton")
                    ?.ToString()
                    ?? string.Empty;
            }
        }

        public bool AllowEditing
        {
            get
            {
                Depends.On(
                    AllowPlaying,
                    IsGameRunning,
                    Show);

                return AllowPlaying &&
                       !IsGameRunning &&
                       !Show;
            }
        }

        public bool AllowPlaying
        {
            get
            {
                Depends.On(
                    CurrentState,
                    Show);

                return CurrentState ==
                       LauncherState.None &&
                       !Show;
            }
        }

        #endregion

        #region Common Properties

        public bool Show
        {
            get => _show;
            set
            {
                if (_show == value)
                    return;

                _show = value;

                OnPropertyChanged(nameof(Show));
                OnPropertyChanged(nameof(AllowPlaying));
                OnPropertyChanged(nameof(AllowEditing));
            }
        }

        public LauncherState CurrentState
        {
            get => _currentState;
            set
            {
                if (_currentState == value)
                    return;

                _currentState = value;

                OnPropertyChanged(nameof(CurrentState));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(TextualProgress));
                OnPropertyChanged(nameof(ShowTextualProgress));
                OnPropertyChanged(nameof(AllowPlaying));
                OnPropertyChanged(nameof(AllowEditing));
            }
        }

        public long CurrentProgress
        {
            get => _currentProgress;
            set
            {
                if (_currentProgress == value)
                    return;

                _currentProgress = value;

                OnPropertyChanged(nameof(CurrentProgress));
                OnPropertyChanged(nameof(TextualProgress));
            }
        }

        public long TotalProgress => 100;

        public long ActualCurrentProgress
        {
            get => _actualCurrentProgress;
            set
            {
                if (_actualCurrentProgress == value)
                    return;

                _actualCurrentProgress = value;

                OnPropertyChanged(
                    nameof(ActualCurrentProgress));

                OnPropertyChanged(
                    nameof(TextualProgress));
            }
        }

        public long ActualTotalProgress
        {
            get => _actualTotalProgress;
            set
            {
                if (_actualTotalProgress == value)
                    return;

                _actualTotalProgress = value;

                OnPropertyChanged(
                    nameof(ActualTotalProgress));

                OnPropertyChanged(
                    nameof(TextualProgress));
            }
        }

        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set
            {
                if (_isIndeterminate == value)
                    return;

                _isIndeterminate = value;

                OnPropertyChanged(nameof(IsIndeterminate));
            }
        }

        #endregion

        #region Animation

        public Visibility Anim_MiniVisibility
        {
            get => _animMiniVisibility;
            set
            {
                if (_animMiniVisibility == value)
                    return;

                _animMiniVisibility = value;

                OnPropertyChanged(
                    nameof(Anim_MiniVisibility));
            }
        }

        public Visibility Anim_Visibility
        {
            get => _animVisibility;
            set
            {
                if (_animVisibility == value)
                    return;

                _animVisibility = value;

                OnPropertyChanged(
                    nameof(Anim_Visibility));
            }
        }

        public Visibility Anim_TextVisibility
        {
            get => _animTextVisibility;
            set
            {
                if (_animTextVisibility == value)
                    return;

                _animTextVisibility = value;

                OnPropertyChanged(
                    nameof(Anim_TextVisibility));
            }
        }

        private async void GetProgressBarAnim()
        {
            if (Application.Current == null)
                return;

            await Application.Current.Dispatcher
                .InvokeAsync(() =>
                {
                    var progressBar =
                        MainDataModel
                            .BackwardsCommunicationHost
                            ?.ProgressBarGrid;

                    var progressBarElement =
                        progressBar as FrameworkElement;

                    if (progressBarElement == null)
                        return;

                    ProgressBarSetContent(
                        Show,
                        true);

                    var animation =
                        new DoubleAnimation
                        {
                            From = Show ? 0 : 72,
                            To = Show ? 72 : 0,
                            Duration =
                                new Duration(
                                    TimeSpan.FromMilliseconds(
                                        350))
                        };

                    animation.Completed +=
                        (s, e) =>
                        {
                            ProgressBarSetContent(
                                Show,
                                false);
                        };

                    /*
                     * HeightProperty appartiene a FrameworkElement,
                     * non a ProgressBar.
                     */
                    progressBarElement.BeginAnimation(
                        FrameworkElement.HeightProperty,
                        animation);
                });
        }

        private void ProgressBarSetContent(
            bool isShown,
            bool isInit)
        {
            Anim_MiniVisibility =
                isShown
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            Anim_Visibility =
                isShown || isInit
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            Anim_TextVisibility =
                isShown || isInit
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        #endregion

        #region Text

        public object Description
        {
            get
            {
                Depends.On(CurrentState);

                return GetProgressBarDescription();
            }
        }

        public string TextualProgress
        {
            get
            {
                Depends.On(
                    CurrentState,
                    CurrentProgress,
                    ActualCurrentProgress,
                    ActualTotalProgress);

                return GetProgressBarTextualProgress();
            }
        }

        public string Information
        {
            get => _information;
            set
            {
                if (_information == value)
                    return;

                _information = value;

                OnPropertyChanged(nameof(Information));
                OnPropertyChanged(nameof(ShowInformation));
            }
        }

        public bool ShowInformation
        {
            get
            {
                Depends.On(Information);

                return !string.IsNullOrEmpty(
                    Information);
            }
        }

        public bool ShowTextualProgress
        {
            get
            {
                Depends.On(
                    CurrentState,
                    TextualProgress);

                return !string.IsNullOrEmpty(
                    TextualProgress);
            }
        }

        private string GetProgressBarDescription()
        {
            if (Application.Current == null)
                return string.Empty;

            string resourceKey = CurrentState switch
            {
                LauncherState.isInitializing =>
                    "ProgressBar_Downloading",

                LauncherState.isDownloading =>
                    "ProgressBar_Downloading",

                LauncherState.isExtracting =>
                    "ProgressBar_Extracting",

                LauncherState.isRegisteringPackage =>
                    "ProgressBar_RegisteringPackage",

                LauncherState.isRemovingPackage =>
                    "ProgressBar_RemovingPackage",

                LauncherState.isUninstalling =>
                    "ProgressBar_Uninstalling",

                LauncherState.isLaunching =>
                    "ProgressBar_Launching",

                LauncherState.isBackingUp =>
                    "ProgressBar_BackingUp",

                _ => null
            };

            if (string.IsNullOrEmpty(resourceKey))
                return string.Empty;

            return Application.Current
                .TryFindResource(resourceKey)
                ?.ToString()
                ?? string.Empty;
        }

        private string GetProgressBarTextualProgress()
        {
            if (CurrentState ==
                LauncherState.isDownloading)
            {
                var current =
                    Math.Round(
                        (double)ActualCurrentProgress /
                        1024 /
                        1024,
                        2)
                    .ToString("0.00");

                var total =
                    Math.Round(
                        (double)ActualTotalProgress /
                        1024 /
                        1024,
                        2)
                    .ToString("0.00");

                return $"{current} MB / {total} MB";
            }

            if (S.IfAny(
                    CurrentState,
                    LauncherState.isRemovingPackage,
                    LauncherState.isRegisteringPackage,
                    LauncherState.isExtracting))
            {
                return $"{CurrentProgress}%";
            }

            if (S.IfAny(
                    CurrentState,
                    LauncherState.isBackingUp,
                    LauncherState.isUninstalling))
            {
                return
                    $"{CurrentProgress} / {TotalProgress}";
            }

            return string.Empty;
        }

        #endregion

        #region Public Methods

        public void SetProgressBarVisibility(
            bool show)
        {
            Show = show;
        }

        public void ResetProgressBarProgress()
        {
            CurrentProgress = 0;
            ActualCurrentProgress = 0;
            ActualTotalProgress = 0;

            IsIndeterminate = true;
        }

        public void SetProgressBarProgress(
            long currentProgress,
            long totalProgress)
        {
            int currentPercent = 0;

            if (totalProgress > 0 &&
                currentProgress >= 0)
            {
                currentPercent =
                    (int)Math.Round(
                        (double)(
                            100 *
                            currentProgress) /
                        totalProgress);
            }

            currentPercent =
                Math.Clamp(
                    currentPercent,
                    0,
                    100);

            CurrentProgress =
                currentPercent;

            ActualCurrentProgress =
                currentProgress;

            ActualTotalProgress =
                totalProgress;

            IsIndeterminate = false;
        }

        public void SetGameRunningStatus(
            bool isRunning)
        {
            IsGameRunning = isRunning;
        }

        public void SetProgressBarText(
            string text = null)
        {
            Information = text;
        }

        public void SetProgressBarState(
            LauncherState? state = null)
        {
            CurrentState =
                state == null
                    ? LauncherState.None
                    : state.Value;
        }

        #endregion

        #region PropertyChanged

        public event PropertyChangedEventHandler
            PropertyChanged;

        protected void OnPropertyChanged(
            string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    propertyName));
        }

        #endregion
    }
}