
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
using BedrockLauncher.Methods;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.Core.Pages.Common;
using BedrockLauncher.Core.Classes;
using BedrockLauncher.ViewModels;
using BedrockLauncher.Classes;

namespace BedrockLauncher.Controls.Items
{
    /// <summary>
    /// Interaction logic for InstallationItem.xaml
    /// </summary>
    public partial class InstallationItem : UserControl
    {
        public InstallationItem()
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

        public static readonly DependencyProperty ButtonPanelVisibilityProperty = DependencyProperty.Register("ButtonPanelVisibility", typeof(Visibility), typeof(InstallationItem), new PropertyMetadata(Visibility.Collapsed, new PropertyChangedCallback(ChangePanelVisibility)));

        private static void ChangePanelVisibility(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as InstallationItem).ButtonPanelVisibility = (Visibility)e.NewValue;
        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var installation = button.DataContext as BLInstallation;
            LauncherModel.Default.GameManager.OpenFolder(installation);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var installation = button.DataContext as BLInstallation;
            bool KeepLauncherOpen = Properties.LauncherSettings.Default.KeepLauncherOpen;
            LauncherModel.Default.GameManager.Play(LauncherModel.Default.ConfigManager.CurrentProfile, installation, KeepLauncherOpen);
        }

        private async void DeleteInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            var title = this.FindResource("Dialog_DeleteItem_Title") as string;
            var content = this.FindResource("Dialog_DeleteItem_Text") as string;
            var item = this.FindResource("Dialog_Item_Installation_Text") as string;

            MenuItem button = sender as MenuItem;
            var installation = button.DataContext as BLInstallation;
            var result = await DialogPrompt.ShowDialog_YesNo(title, content, item, installation.DisplayName_Full);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {

                LauncherModel.Default.ConfigManager.DeleteInstallation(installation);
            }
        }

        private void More_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var installation = button.DataContext as BLInstallation;
            (this.Tag as Pages.Play.InstallationsScreen).InstallationsList.SelectedItem = installation;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.DataContext = installation;
            button.ContextMenu.IsOpen = true;
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            (this.Tag as Pages.Play.InstallationsScreen).InstallationsList.SelectedItem = null;
        }

        private void EditInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = sender as MenuItem;
            var installation = button.DataContext as BLInstallation;
            int index = LauncherModel.Default.ConfigManager.CurrentInstallations.IndexOf(installation);
            ViewModels.LauncherModel.Default.SetOverlayFrame(new EditInstallationScreen(index, installation));
        }

        private void DuplicateInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = sender as MenuItem;
            var installation = button.DataContext as BLInstallation;
            LauncherModel.Default.ConfigManager.DuplicateInstallation(installation);
        }
    }
}
