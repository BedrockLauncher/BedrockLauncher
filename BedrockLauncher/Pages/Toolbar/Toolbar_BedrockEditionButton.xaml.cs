using System.Windows;

namespace BedrockLauncher.Pages.Toolbar
{
    /// <summary>
    /// Interaction logic for BedrockEditionButton.xaml
    /// </summary>
    public partial class Toolbar_BedrockEditionButton : Toolbar_ButtonBase
    {

        public Toolbar_BedrockEditionButton()
        {
            InitializeComponent();
            this.DataContext = ViewModels.MainDataModel.Default;
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
