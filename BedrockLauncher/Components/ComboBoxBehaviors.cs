using BedrockLauncher.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BL_Core.Components;

namespace BedrockLauncher.Components
{
    public static class ComboBoxBehaviors
    {
        public static RelayCommand ComboBox_RequestBringIntoView { get; set; } = new RelayCommand(ComboBox_RequestBringIntoViewExecute);
        public static RelayCommand ComboBox_PreviewMouseWheel { get; set; } = new RelayCommand(ComboBox_PreviewMouseWheelExecute);
        public static void ComboBox_RequestBringIntoViewExecute(object args)
        {

        }
        public static void ComboBox_PreviewMouseWheelExecute(object args)
        {
            // To prevent scrolling when mouseover
            var e = (args as MouseEventArgs);
            var sender = e.Source;
            if (sender is System.Windows.Controls.ComboBox)
            {
                e.Handled = !((System.Windows.Controls.ComboBox)sender).IsDropDownOpen;
            }

        }
    }
}
