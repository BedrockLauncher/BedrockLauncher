using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Events
{
    public class OverlayChangedState : EventArgs
    {
        public static new OverlayChangedState Empty => new OverlayChangedState();
        public bool isEmpty = false;

        public OverlayChangedState(bool _isEmpty = false)
        {
            isEmpty = _isEmpty;
        }
    }
}
