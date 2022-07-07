using System.Windows;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for ServersButton.xaml
    /// </summary>
    public partial class Toolbar_CommunityButton : Toolbar_ButtonBase
    {

        public Toolbar_CommunityButton()
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
