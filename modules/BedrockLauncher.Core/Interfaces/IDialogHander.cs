using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Core.Interfaces
{
    public interface IDialogHander
    {
        void SetDialogFrame(object content);

        void SetOverlayFrame_Strict(object content);

        void SetOverlayFrame(object content);
    }
}
