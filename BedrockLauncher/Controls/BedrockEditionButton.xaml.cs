using System.Windows;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for BedrockEditionButton.xaml
    /// </summary>
    public partial class BedrockEditionButton : ToolbarButtonBase
    {

        public BedrockEditionButton()
        {
            InitializeComponent();
            this.DataContext = ViewModels.MainViewModel.Default;
        }

        private void SideBarButton_Click(object sender, RoutedEventArgs e)
        {
            ToolbarButtonBase_Click(this, e);
        }

        private void Button_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (Button.IsChecked.Value) Progressbar.Visibility = Visibility.Collapsed;
            else Progressbar.Visibility = Visibility.Visible;
        }
    }
}
