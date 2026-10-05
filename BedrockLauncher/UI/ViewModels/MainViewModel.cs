using BedrockLauncher.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.UI.ViewModels
{
    public class MainViewModel
    {
        public static MainViewModel Default { get; set; } = new MainViewModel();
        public static IDialogHander Handler { get; private set; }

        public static void SetHandler(IDialogHander _handler) => Handler = _handler;
    }
}
