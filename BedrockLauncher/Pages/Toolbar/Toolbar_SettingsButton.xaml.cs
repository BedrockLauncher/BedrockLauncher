using System.Windows;

namespace BedrockLauncher.Pages.Toolbar
{
    /// <summary>
    /// Interaction logic for SideBarButton.xaml
    /// </summary>
    public partial class Toolbar_SettingsButton : Toolbar_ButtonBase
    {

        public Toolbar_SettingsButton()
        {
            InitializeComponent();
        }

        private void SideBarButton_Click(object sender, RoutedEventArgs e)
        {
            ToolbarButtonBase_Click(this, e);
        }
    }
}
