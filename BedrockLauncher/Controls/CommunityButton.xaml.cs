using System.Windows;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for ServersButton.xaml
    /// </summary>
    public partial class CommunityButton : ToolbarButtonBase
    {

        public CommunityButton()
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
