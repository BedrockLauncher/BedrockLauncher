using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Classes;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for InstallationItem.xaml
    /// </summary>
    public partial class Component_InstallationItem : UserControl
    {
        public Component_InstallationItem()
        {
            InitializeComponent();
        }

        public Visibility ButtonPanelVisibility
        {
            get
            {
                return ButtonPanel != null ? ButtonPanel.Visibility : Visibility.Hidden;
            }
            set
            {
                if (ButtonPanel != null) ButtonPanel.Visibility = value;
            }
        }

        public static readonly DependencyProperty ButtonPanelVisibilityProperty = DependencyProperty.Register("ButtonPanelVisibility", typeof(Visibility), typeof(Component_InstallationItem), new PropertyMetadata(Visibility.Collapsed, new PropertyChangedCallback(ChangePanelVisibility)));

        private static void ChangePanelVisibility(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as Component_InstallationItem).ButtonPanelVisibility = (Visibility)e.NewValue;
        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var installation = button.DataContext as BLInstallation;
            installation.OpenDirectory();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var installation = button.DataContext as BLInstallation;
            bool KeepLauncherOpen = Properties.LauncherSettings.Default.KeepLauncherOpen;
            MainDataModel.Default.Play(MainDataModel.Default.Config.CurrentProfile, installation, KeepLauncherOpen);
        }

        private async void DeleteInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Improve Localization Handling
            var title = this.FindResource("Dialog_DeleteItem_Title") as string;
            var content = this.FindResource("Dialog_DeleteItem_Text") as string;
            var item = this.FindResource("Dialog_Item_Installation_Text") as string;
            var optional = this.FindResource("Dialog_Item_Optional_DeleteInstallationData") as string;

            MenuItem button = sender as MenuItem;
            var installation = button.DataContext as BLInstallation;
            var result = await DialogPrompt.ShowDialog_YesNo_Optional(title, content, optional, true, item, installation.DisplayName_Full);

            if (result.Item1 == System.Windows.Forms.DialogResult.Yes)
            {
                MainDataModel.Default.Config.Installation_Delete(installation, result.Item2);
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
            ViewModels.MainViewModel.Default.SetOverlayFrame(new EditInstallationScreen(installation));
        }

        private void DuplicateInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = sender as MenuItem;
            var installation = button.DataContext as BLInstallation;
            MainDataModel.Default.Config.Installation_Clone(installation);
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var installation = button.DataContext as BLInstallation;
            MainDataModel.Default.Config.Installation_MoveUp(installation);
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var installation = button.DataContext as BLInstallation;
            MainDataModel.Default.Config.Installation_MoveDown(installation);
        }

        private void UpdateButtonVisibility()
        {
            if (Properties.LauncherSettings.Default.InstallationsSortMode == Enums.InstallationSort.None)
            {
                MoveUp.Visibility = Visibility.Visible;
                MoveDown.Visibility = Visibility.Visible;
            }
            else
            {
                MoveUp.Visibility = Visibility.Collapsed;
                MoveDown.Visibility = Visibility.Collapsed;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateButtonVisibility();
        }

        private void InstallInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = sender as MenuItem;
            var installation = button.DataContext as BLInstallation;
            MainDataModel.Default.Install(MainDataModel.Default.Config.CurrentProfile, installation);
        }
    }
}
