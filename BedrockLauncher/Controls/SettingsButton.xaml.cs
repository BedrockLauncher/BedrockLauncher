using System.Windows;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for SideBarButton.xaml
    /// </summary>
    public partial class SettingsButton : ToolbarButtonBase
    {

        public SettingsButton()
        {
            InitializeComponent();
        }

        private void SideBarButton_Click(object sender, RoutedEventArgs e)
        {
            ToolbarButtonBase_Click(this, e);
            //ViewModels.MainViewModel.MainThread.ButtonManager_Base(this.Name);
        }
    }
}
