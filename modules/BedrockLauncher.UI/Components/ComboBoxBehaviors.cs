using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace BedrockLauncher.UI.Components
{
    public static class ComboBoxBehaviors
    {
        public static RelayCommand ComboBox_RequestBringIntoView { get; set; } = new RelayCommand(ComboBox_RequestBringIntoViewExecute);
        public static RelayCommand ComboBox_PreviewMouseWheel { get; set; } = new RelayCommand(ComboBox_PreviewMouseWheelExecute);
        public static RelayCommand ComboBox_ForgetNavigation { get; set; } = new RelayCommand(ComboBox_ForgetNavigationExecute);

        public static void ComboBox_RequestBringIntoViewExecute(object args)
        {
            var e = (args as RoutedEventArgs);
            e.Handled = true;
        }

        public static void ComboBox_ForgetNavigationExecute(object args)
        {
            // To prevent scrolling when mouseover
            var e = (args as KeyEventArgs);
            var sender = e.Source;
            if (sender is System.Windows.Controls.ComboBox)
            {
                if (!((System.Windows.Controls.ComboBox)sender).IsDropDownOpen)
                {
                    if (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Left || e.Key == Key.Right)
                    {
                        e.Handled = true;

                        if (e.Key == Key.Down || e.Key == Key.Up) ((System.Windows.Controls.ComboBox)sender).IsDropDownOpen = true;
                        else
                        {
                            var direction = (e.Key == Key.Left ? FocusNavigationDirection.Left : FocusNavigationDirection.Right);
                            DependencyObject candidate = ((System.Windows.Controls.ComboBox)sender).PredictFocus(direction);
                            if (candidate != null && candidate is Control) (candidate as Control).Focus();
                        }

                        return;
                    }
                }

            }

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
