using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BedrockLauncher.Classes;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for ProfileSelector.xaml
    /// </summary>
    public partial class ProfileItem : Grid
    {
        private string _ProfileName { get; set; }
        private ProfileButton SelectorParent { get; set; }
        public ProfileItem()
        {
            InitializeComponent();
        }

        public ProfileItem(KeyValuePair<string, MCProfile> profile, ProfileButton _selectorParent)
        {
            InitializeComponent();
            this.SelectorParent = _selectorParent;
            this.DataContext = profile.Value;
            _ProfileName = profile.Key;

            if (Properties.LauncherSettings.Default.CurrentProfileUUID == profile.Key) 
                SelectedMark.Visibility = Visibility.Visible;
        }

        private void SwitchProfile()
        {
            MainViewModel.Default.Config.Profile_Switch(_ProfileName);
            SelectorParent.ProfileContextMenu.IsOpen = false;
            SelectorParent.GetBindingExpression(Grid.DataContextProperty).UpdateTarget();
        }

        private void SourceButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchProfile();
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released) SwitchProfile();
        }
    }
}
