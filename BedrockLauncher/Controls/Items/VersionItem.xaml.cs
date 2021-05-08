using BedrockLauncher.Methods;
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

namespace BedrockLauncher.Controls.Items
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

        public static readonly DependencyProperty dependencyProperty = DependencyProperty.Register("ButtonPanelVisibility", typeof(Visibility), typeof(VersionItem), new PropertyMetadata(Visibility.Collapsed, new PropertyChangedCallback(ChangePanelVisibility)));

        private static void ChangePanelVisibility(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as VersionItem).ButtonPanelVisibility = (Visibility)e.NewValue;
        }

        private Pages.VersionsSettingsPage GetParent()
        {
            return this.Tag as Pages.VersionsSettingsPage;
        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var version = button.DataContext as Classes.MCVersion;
            ConfigManager.GameManager.OpenFolder(version);
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = sender as MenuItem;
            var version = button.DataContext as Classes.MCVersion;

            var title = this.FindResource("Dialog_DeleteItem_Title") as string;
            var content = this.FindResource("Dialog_DeleteItem_Text") as string;
            var item = this.FindResource("Dialog_Item_Version_Text") as string;

            var result = await DialogPrompt.ShowDialog_YesNo(title, content, item, version.DisplayName);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                ConfigManager.GameManager.Remove(version);
                GetParent().RefreshVersionsList();
            }
        }

        private void More_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var version = button.DataContext as Classes.MCVersion;
            ConfigManager.MainThread.SetVersionPageSelection(version);
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.DataContext = version;
            button.ContextMenu.IsOpen = true;
        }

        private void Repair_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var version = button.DataContext as Classes.MCVersion;
            ConfigManager.GameManager.Repair(version);
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {

        }
    }
}
