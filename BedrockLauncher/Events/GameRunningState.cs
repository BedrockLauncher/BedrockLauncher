using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Events
{
    public class GameRunningState : EventArgs
    {
        public static new GameRunningState Empty => new GameRunningState();
        public bool isRunning = false;

        public GameRunningState(bool _isRunning = false)
        {
            isRunning = _isRunning;
        }
    }
}
