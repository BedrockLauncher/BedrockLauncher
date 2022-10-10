using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Classes;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Settings.Versions
{
    /// <summary>
    /// Interaction logic for VersionItem.xaml
    /// </summary>
    public partial class Component_VersionItem : UserControl
    {
        public Component_VersionItem()
        {
            InitializeComponent();
        }

        public Visibility ButtonPanelVisibility
        {
            get
            {
                return ButtonPanel != null ? ButtonPanel.Visibility : Visibility.Collapsed;
            }
            set
            {
                if (ButtonPanel != null) ButtonPanel.Visibility = value;
            }
        }

        public static readonly DependencyProperty ButtonPanelVisibilityProperty = DependencyProperty.Register("ButtonPanelVisibility", typeof(Visibility), typeof(Component_VersionItem), new PropertyMetadata(Visibility.Collapsed, new PropertyChangedCallback(ChangePanelVisibility)));

        private static void ChangePanelVisibility(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as Component_VersionItem).ButtonPanelVisibility = (Visibility)e.NewValue;
        }

        private VersionsPage GetParent()
        {
            return this.Tag as VersionsPage;
        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var version = button.DataContext as MCVersion;
            version.OpenDirectory();
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = sender as MenuItem;
            var version = button.DataContext as MCVersion;

            var title = this.FindResource("Dialog_DeleteItem_Title") as string;
            var content = this.FindResource("Dialog_DeleteItem_Text") as string;
            var item = this.FindResource("Dialog_Item_Version_Text") as string;

            var result = await DialogPrompt.ShowDialog_YesNo(title, content, item, version.DisplayName);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                await Task.Run(() => MainDataModel.Default.RemoveVersion(version));
                //await Task.Run(Program.OnApplicationRefresh);
            }
        }

        private void More_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var version = button.DataContext as MCVersion;
            (this.Tag as VersionsPage).VersionsList.SelectedItem = version;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.DataContext = version;
            button.ContextMenu.IsOpen = true;
        }

        private void Repair_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var version = button.DataContext as MCVersion;
            MainDataModel.Default.RepairVersion(version);
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {

        }
    }
}
