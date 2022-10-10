using System.Windows;

namespace BedrockLauncher.Pages.Toolbar
{
    /// <summary>
    /// Interaction logic for NewsButton.xaml
    /// </summary>
    public partial class Toolbar_NewsButton : Toolbar_ButtonBase
    {

        public Toolbar_NewsButton()
        {
            InitializeComponent();
        }

        private void SideBarButton_Click(object sender, RoutedEventArgs e)
        {
            ToolbarButtonBase_Click(this, e);
        }
    }
}
