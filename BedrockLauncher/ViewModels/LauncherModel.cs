using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Events;
using BedrockLauncher.Classes;

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

        public bool IsStateChanging => StateChangeInfo != null;
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
                return AllowPlaying && !IsGameRunning;
            }
        }

        public bool AllowPlaying
        {
            get
            {
                var state = StateChangeInfo?.CurrentState ?? VersionStateChangeInfo.StateChange.None;
                return state == VersionStateChangeInfo.StateChange.None;
            }
        }

        #endregion

        #region Bindings

        private VersionStateChangeInfo _stateChangeInfo;
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
                RefreshUI();
            }
        }
        public VersionStateChangeInfo StateChangeInfo
        {
            get 
            { 
                return _stateChangeInfo; 
            }
            set 
            { 
                _stateChangeInfo = value; 
                OnPropertyChanged(nameof(StateChangeInfo)); 
                OnPropertyChanged(nameof(IsStateChanging));
                RefreshUI();
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
                RefreshUI();
            }
        }

        #endregion

        private void RefreshUI()
        {
            OnPropertyChanged(nameof(AllowPlaying));
            OnPropertyChanged(nameof(AllowEditing));
            OnPropertyChanged(nameof(PlayButtonString));
            OnPropertyChanged(nameof(IsGameRunning));
            OnPropertyChanged(nameof(ShowProgressBar));
        }
    }
}
