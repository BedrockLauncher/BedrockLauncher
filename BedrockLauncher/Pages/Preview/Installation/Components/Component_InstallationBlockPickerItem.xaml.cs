using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace BedrockLauncher.Pages.Preview.Installation.Components
{
    /// <summary>
    /// Interaction logic for InstallationBlockPickerItem.xaml
    /// </summary>
    public partial class Component_InstallationBlockPickerItem : ListViewItem
    {
        public bool IsCustomImage { get; set; }

        public event EventHandler SelectItem;

        public Component_InstallationBlockPickerItem()
        {
            InitializeComponent();
        }

        private void ShowCrossButton()
        {
            if (IsCustomImage)
            {
                CrossButton.Visibility = Visibility.Visible;
            }
        }

        private void HideCrossButton()
        {
            if (IsCustomImage)
            {
                CrossButton.Visibility = Visibility.Collapsed;
            }
        }

        private void MainButton_GotFocus(object sender, RoutedEventArgs e)
        {
            ShowCrossButton();
        }

        private void MainButton_LostFocus(object sender, RoutedEventArgs e)
        {
            HideCrossButton();
        }

        private void MainButton_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ShowCrossButton();
        }

        private void MainButton_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            HideCrossButton();
        }

        private void MainButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ShowCrossButton();
        }

        private void MainButton_MouseLeave(object sender, MouseEventArgs e)
        {
            HideCrossButton();
        }

        private void CrossButton_MouseEnter(object sender, MouseEventArgs e)
        {

        }

        private void CrossButton_MouseLeave(object sender, MouseEventArgs e)
        {

        }

        private void ListViewItem_PreviewMouseEvent(object sender, MouseButtonEventArgs e)
        {
            if (CrossButton.IsMouseOver)
            {
                CrossButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                e.Handled = true;
            }
            else SelectItem?.Invoke(this, EventArgs.Empty);
        }
    }


    public partial class BlockPickerBlankItem : ListViewItem
    {
        public BlockPickerBlankItem() : base()
        {
            this.Focusable = false;
            this.IsHitTestVisible = false;
            KeyboardNavigation.SetAcceptsReturn(this, false);
            KeyboardNavigation.SetIsTabStop(this, false);
            KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.None);
            KeyboardNavigation.SetControlTabNavigation(this, KeyboardNavigationMode.None);
            KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.None);
            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.None);

        }
    }
}
