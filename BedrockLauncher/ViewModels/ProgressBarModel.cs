using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using PostSharp.Patterns.Model;
using BedrockLauncher.Enums;
using JemExtensions;
using S = JemExtensions.SpecialExtensions;
using System.ComponentModel;

namespace BedrockLauncher.ViewModels
{

    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]
    public class ProgressBarModel
    {
        #region Init

        public ProgressBarModel()
        {
            ((INotifyPropertyChanged)this).PropertyChanged += ProgressBarModel_PropertyChanged;
        }
        private void ProgressBarModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Show)) GetProgressBarAnim();
        }

        #endregion

        #region Various Properties

        public ICommand CancelCommand { get; set; }

        public bool AllowCancel { get; set; }
        public bool IsGameRunning { get; set; }
        public string PlayButtonString
        {
            get
            {
                Depends.On(IsGameRunning);
                if (IsGameRunning) return App.Current.FindResource("GameTab_PlayButton_Kill_Text").ToString();
                else return App.Current.FindResource("GameTab_PlayButton_Text").ToString();
            }
        }
        public bool AllowEditing
        {
            get
            {
                Depends.On(AllowPlaying, IsGameRunning, Show);
                return AllowPlaying && !IsGameRunning && !Show;
            }
        }
        public bool AllowPlaying
        {
            get
            {
                Depends.On(CurrentState, Show);
                return CurrentState == LauncherState.None && !Show;
            }
        }

        #endregion

        #region Common Properties

        public bool Show { get; set; } = false;
        public LauncherState CurrentState { get; set; }
        public long CurrentProgress { get; set; }
        public long TotalProgress => 100;
        public long ActualCurrentProgress { get; set; }
        public long ActualTotalProgress { get; set; }
        public bool IsIndeterminate { get; set; } = true;

        #endregion

        #region Animation

        public Visibility Anim_MiniVisibility { get; set; } = Visibility.Collapsed;
        public Visibility Anim_Visibility { get; set; } = Visibility.Collapsed;
        public Visibility Anim_TextVisibility { get; set; } = Visibility.Collapsed;
        private async void GetProgressBarAnim()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ProgressBarSetContent(Show, true);

                Storyboard storyboard = new Storyboard();
                DoubleAnimation animation = new DoubleAnimation
                {
                    From = (Show ? 0 : 72),
                    To = (Show ? 72 : 0),
                    Duration = new Duration(TimeSpan.FromMilliseconds(350))
                };
                storyboard.Children.Add(animation);
                Storyboard.SetTargetProperty(animation, new System.Windows.PropertyPath(ProgressBar.HeightProperty));
                Storyboard.SetTarget(animation, MainViewModel.Default.ProgressBarGrid);
                storyboard.Completed += new EventHandler((s, e) => ProgressBarSetContent(Show, false));
                storyboard.Begin();
            });

            void ProgressBarSetContent(bool isShown, bool isInit)
            {
                Anim_MiniVisibility = isShown ? Visibility.Visible : Visibility.Collapsed;
                Anim_Visibility = isShown || isInit ? Visibility.Visible : Visibility.Collapsed;
                Anim_TextVisibility = isShown || isInit ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion

        #region Text

        public object Description { get { Depends.On(CurrentState); return GetProgressBarDescription(); } }
        public string TextualProgress { get { Depends.On(CurrentState, CurrentProgress, ActualCurrentProgress, ActualTotalProgress); return GetProgressBarTextualProgress(); } }
        public string Information { get; set; }

        public bool ShowInformation { get { Depends.On(Information); return !string.IsNullOrEmpty(Information); } }
        public bool ShowTextualProgress { get { Depends.On(CurrentState); return !string.IsNullOrEmpty(TextualProgress); } }

        private string GetProgressBarDescription()
        {
            switch (CurrentState)
            {
                case LauncherState.isInitializing:
                    return Application.Current.TryFindResource("ProgressBar_Downloading").ToString();
                case LauncherState.isDownloading:
                    return Application.Current.TryFindResource("ProgressBar_Downloading").ToString();
                case LauncherState.isExtracting:
                    return Application.Current.TryFindResource("ProgressBar_Extracting").ToString();
                case LauncherState.isRegisteringPackage:
                    return Application.Current.TryFindResource("ProgressBar_RegisteringPackage").ToString();
                case LauncherState.isRemovingPackage:
                    return Application.Current.TryFindResource("ProgressBar_RemovingPackage").ToString();
                case LauncherState.isUninstalling:
                    return Application.Current.TryFindResource("ProgressBar_Uninstalling").ToString();
                case LauncherState.isLaunching:
                    return Application.Current.TryFindResource("ProgressBar_Launching").ToString();
                case LauncherState.isBackingUp:
                    return Application.Current.TryFindResource("ProgressBar_BackingUp").ToString();
                default:
                    return null;
            }
        }
        private string GetProgressBarTextualProgress()
        {
            if (CurrentState == LauncherState.isDownloading)
            {
                var current = Math.Round((double)ActualCurrentProgress / 1024 / 1024, 2).ToString("0.00");
                var total = Math.Round((double)ActualTotalProgress / 1024 / 1024, 2).ToString("0.00");
                return $"{current} MB / {total} MB";
            }
            else if (S.IfAny(CurrentState, LauncherState.isRemovingPackage, LauncherState.isRegisteringPackage, LauncherState.isExtracting)) return $"{CurrentProgress}%";
            else if (CurrentState == LauncherState.isBackingUp) return $"{CurrentProgress} / {TotalProgress}";
            else return string.Empty;

        }

        #endregion

        #region Public Methods

        public void SetProgressBarVisibility(bool show)
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
        public void SetProgressBarProgress(long currentProgress, long totalProgress)
        {
            int currentPercent = 0;
            if (totalProgress != 0 && currentProgress != 0)
                currentPercent = (int)Math.Round((double)(100 * currentProgress) / totalProgress);

            CurrentProgress = currentPercent;
            ActualCurrentProgress = currentProgress;
            ActualTotalProgress = totalProgress;

            if (IsIndeterminate != false) IsIndeterminate = false;
        }
        public void SetGameRunningStatus(bool isRunning)
        {
            IsGameRunning = isRunning;
        }
        public void SetProgressBarText(string text = null)
        {
            Information = text;
        }
        public void SetProgressBarState(LauncherState? state = null)
        {
            CurrentState = state == null ? LauncherState.None : state.Value;
        }

        #endregion
    }
}
