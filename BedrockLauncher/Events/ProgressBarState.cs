using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Events
{
    public class ProgressBarState : EventArgs
    {
        public static new ProgressBarState Empty => new ProgressBarState();
        public bool isVisible = false;

        public ProgressBarState(bool _isVisible = false)
        {
            isVisible = _isVisible;
        }
    }
}
