using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.UI.Interfaces
{
    public interface IDialogHander
    {
        void SetDialogFrame(object content);
        void SetOverlayFrame(object content, bool isStrict = false);
    }
}
