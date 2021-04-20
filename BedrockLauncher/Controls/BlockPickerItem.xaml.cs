using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for BlockPickerItem.xaml
    /// </summary>
    public partial class BlockPickerItem : Grid
    {
        public bool IsCustomImage { get; set; }

        public BlockPickerItem()
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
    }
}
