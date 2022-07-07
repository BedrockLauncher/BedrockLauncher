using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for ProfileContextMenu.xaml
    /// </summary>
    public partial class Toolbar_ProfileButton : Toolbar_ButtonBase
    {
        private List<ProfileItem> OtherAccountControls { get; set; } = new List<ProfileItem>();

        public Toolbar_ProfileButton()
        {
            InitializeComponent();
        }

        private void UpdateProfileList()
        {
            foreach (var entry in OtherAccountControls)
            {
                if (ProfileContextMenu.Items.Contains(entry)) ProfileContextMenu.Items.Remove(entry);
            }
            OtherAccountControls.Clear();

            int profileCount = MainViewModel.Default.Config.profiles.Count;

            foreach (var entry in MainViewModel.Default.Config.profiles)
            {
                ProfileItem profile = new ProfileItem(entry, this);
                if (!(profileCount <= 1))
                {
                    OtherAccountControls.Add(profile);
                    ProfileContextMenu.Items.Add(profile);
                }
            }
        }

        private void OpenContextMenu()
        {
            if (OtherAccountControls.Count <= 1)
            {
                RemoveProfileButton.IsEnabled = false;
                OtherAccountsHeader.Visibility = Visibility.Collapsed;
                OtherAccountsSeperator.Visibility = Visibility.Collapsed;
            }
            else
            {
                RemoveProfileButton.IsEnabled = true;
                OtherAccountsHeader.Visibility = Visibility.Visible;
                OtherAccountsSeperator.Visibility = Visibility.Visible;
            }

            ProfileContextMenu.Placement = PlacementMode.Right;
            ProfileContextMenu.PlacementRectangle = new Rect(SourceButton.ActualWidth, SourceButton.ActualHeight, 0, 0);
            ProfileContextMenu.PlacementTarget = SourceButton;
            ProfileContextMenu.IsOpen = true;
        }

        private void SourceButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateProfileList();
            OpenContextMenu();
        }

        private void ManageProfilesButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditProfileButton_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                MainViewModel.Default.SetOverlayFrame(new EditProfileScreen(MainViewModel.Default.Config.CurrentProfile));
            });
        }

        private void AddProfileButton_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                MainViewModel.Default.SetOverlayFrame(new EditProfileScreen());
            });
        }

        private async void RemoveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            string uuid = Properties.LauncherSettings.Default.CurrentProfileUUID;
            if (MainViewModel.Default.Config.profiles.ContainsKey(uuid))
            {
                var profile = MainViewModel.Default.Config.profiles[uuid].Name;

                var title = this.FindResource("Dialog_DeleteItem_Title") as string;
                var content = this.FindResource("Dialog_DeleteItem_Text") as string;
                var item = this.FindResource("Dialog_Item_Profile_Text") as string;

                var result = await DialogPrompt.ShowDialog_YesNo(title, content, item, profile);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    MainViewModel.Default.Config.Profile_Remove(uuid);
                }
            }
        }

        private void ProfileContextMenu_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {

        }

        private void SourceButton_Checked(object sender, RoutedEventArgs e)
        {
            SourceButton.IsChecked = false;
        }
    }
}
