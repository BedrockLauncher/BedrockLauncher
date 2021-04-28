using BedrockLauncher.Core;
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

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var version = button.DataContext as Classes.MCVersion;
            ConfigManager.GameManager.Remove(version);
            GetParent().RefreshVersionsList();
        }

        private void More_Click(object sender, RoutedEventArgs e)
        {
            /*
            Button button = sender as Button;
            var installation = button.DataContext as Classes.Installation;
            InstallationsList.SelectedItem = installation;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.DataContext = installation;
            button.ContextMenu.IsOpen = true;
            */
        }
    }
}
