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
using BedrockLauncher.Methods;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for ProfileContextMenu.xaml
    /// </summary>
    public partial class ProfileButton : Grid
    {
        public ProfileButton()
        {
            InitializeComponent();
        }

        private void ButtonManager(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ButtonManager(sender, e);
        }

        private void ComboBoxItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // To not move combobox list on mouse hover
            e.Handled = true;
        }
    }
}
