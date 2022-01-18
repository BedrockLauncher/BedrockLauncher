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
using BedrockLauncher.Extensions;
using System.Windows.Controls.Primitives;
using BedrockLauncher.Classes;

namespace BedrockLauncher.Controls.Toolbar
{
    /// <summary>
    /// Interaction logic for JavaEditionButton.xaml
    /// </summary>
    public partial class JavaEditionButton : ToolbarButtonBase
    {

        public JavaEditionButton()
        {
            InitializeComponent();
        }

        private async void SideBarButton_Click(object sender, RoutedEventArgs e)
        {
            ToolbarButtonBase_Click(this, e);
            //await Task.Run(ViewModels.MainViewModel.MainThread.NavigateToJavaLauncher);
            await this.Dispatcher.InvokeAsync(() => { this.Button.IsChecked = false; });
        }
    }
}
