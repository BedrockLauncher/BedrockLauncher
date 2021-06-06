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
using System.Windows.Controls.Primitives;
using BedrockLauncher.Classes;

namespace BedrockLauncher.Controls.Toolbar
{
    /// <summary>
    /// Interaction logic for DungeonsButton.xaml
    /// </summary>
    public partial class DungeonsButton : Grid
    {

        public DungeonsButton()
        {
            InitializeComponent();
            this.DataContext = ViewModels.LauncherModel.Default;
        }

        private void SideBarButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.LauncherModel.MainThread.ButtonManager_Base(this.Name);
        }

        private void Button_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (Button.IsChecked.Value) Progressbar.Visibility = Visibility.Collapsed;
            else Progressbar.Visibility = Visibility.Visible;
        }
    }
}
