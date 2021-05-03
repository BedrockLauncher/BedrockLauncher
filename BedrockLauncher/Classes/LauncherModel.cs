using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Classes
{
    public class LauncherModel : NotifyPropertyChangedBase
    {
        private VersionStateChangeInfo _stateChangeInfo;
        public VersionStateChangeInfo StateChangeInfo
        {
            get { return _stateChangeInfo; }
            set { _stateChangeInfo = value; OnPropertyChanged("StateChangeInfo"); OnPropertyChanged("IsStateChanging"); }
        }
        public bool IsStateChanging => StateChangeInfo != null;
    }
}
