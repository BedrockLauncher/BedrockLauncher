using BedrockLauncher.Extensions;
using BedrockLauncher.Pages;
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
using BedrockLauncher.Pages.Common;
using BedrockLauncher.Classes;
using BedrockLauncher.ViewModels;
using BedrockLauncher.UI.Pages.Common;

namespace BedrockLauncher.Controls.Items.Launcher
{
    /// <summary>
    /// Interaction logic for VersionItem.xaml
    /// </summary>
    public partial class VersionItem : UserControl
    {
        public VersionItem()
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

        public static readonly DependencyProperty ButtonPanelVisibilityProperty = DependencyProperty.Register("ButtonPanelVisibility", typeof(Visibility), typeof(VersionItem), new PropertyMetadata(Visibility.Collapsed, new PropertyChangedCallback(ChangePanelVisibility)));

        private static void ChangePanelVisibility(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as VersionItem).ButtonPanelVisibility = (Visibility)e.NewValue;
        }

        private Pages.Settings.VersionsPage GetParent()
        {
            return this.Tag as Pages.Settings.VersionsPage;
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
                await Task.Run(() => MainViewModel.Default.RemoveVersion(version));
                await Task.Run(Program.OnApplicationRefresh);
            }
        }

        private void More_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var version = button.DataContext as MCVersion;
            (this.Tag as Pages.Settings.VersionsPage).VersionsList.SelectedItem = version;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.DataContext = version;
            button.ContextMenu.IsOpen = true;
        }

        private void Repair_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var version = button.DataContext as MCVersion;
            MainViewModel.Default.RepairVersion(version);
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {

        }
    }
}
