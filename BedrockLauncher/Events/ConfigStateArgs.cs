using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Events
{
    public class ConfigStateArgs : EventArgs
    {
        public static new ConfigStateArgs Empty => new ConfigStateArgs();
    }
}
