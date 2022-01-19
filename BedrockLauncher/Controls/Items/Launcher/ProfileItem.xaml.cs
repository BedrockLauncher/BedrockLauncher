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
using BedrockLauncher.Extensions;
using BedrockLauncher.Classes;
using BedrockLauncher.Controls.Toolbar;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Controls.Items.Launcher
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
            this.Tag = profile.Value;
            this.ProfileName.Text = profile.Key;
            _ProfileName = profile.Key;
            if (Properties.LauncherSettings.Default.CurrentProfile == profile.Key) SelectedMark.Visibility = Visibility.Visible;
        }

        private void SwitchProfile()
        {
            MainViewModel.Default.Config.Profile_Switch(_ProfileName);
            SelectorParent.ProfileContextMenu.IsOpen = false;
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
