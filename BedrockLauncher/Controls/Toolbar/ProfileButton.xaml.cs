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
using System.Windows.Controls.Primitives;
using BedrockLauncher.Classes;
using BedrockLauncher.Core;
using BedrockLauncher.Controls.Items;

namespace BedrockLauncher.Controls.Toolbar
{
    /// <summary>
    /// Interaction logic for ProfileContextMenu.xaml
    /// </summary>
    public partial class ProfileButton : Grid
    {
        private List<ProfileItem> OtherAccountControls { get; set; } = new List<ProfileItem>();

        public ProfileButton()
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

            foreach (var entry in ConfigManager.ProfileList.profiles)
            {
                ProfileItem profile = new ProfileItem(entry, this);
                OtherAccountControls.Add(profile);
                ProfileContextMenu.Items.Add(profile);
            }
        }

        private void OpenContextMenu()
        {
            ProfileContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Left;
            ProfileContextMenu.PlacementRectangle = new Rect(0, SourceButton.ActualHeight, 0, 0);
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

        }

        private void AddProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.NavigateToNewProfilePage();
        }

        private void RemoveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.RemoveProfile(Properties.Settings.Default.CurrentProfile);
        }
    }
}
