using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace BedrockLauncher.UI.Components
{
    public static class ButtonBehaviors
    {
        public static RelayCommand ToolbarButton_LoseFocus { get; set; } = new RelayCommand(ToolbarButton_LoseFocusExecute);

        public static void ToolbarButton_LoseFocusExecute(object args)
        {
            //var e = (args as RoutedEventArgs);
            //var source = (e.Source as ToggleButton);
            //Keyboard.ClearFocus();
        }
    }
}
