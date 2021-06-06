using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BL_Core.Components;

namespace BL_Core.Components
{
    public static class ButtonBehaviors
    {
        public static RelayCommand ToolbarButton_LoseFocus { get; set; } = new RelayCommand(ToolbarButton_LoseFocusExecute);

        public static void ToolbarButton_LoseFocusExecute(object args)
        {
            Keyboard.ClearFocus();
        }
    }
}
