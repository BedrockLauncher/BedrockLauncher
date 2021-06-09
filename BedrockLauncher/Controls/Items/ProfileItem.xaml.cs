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
using BedrockLauncher.Classes;
using BedrockLauncher.Controls.Toolbar;
using BL_Core.Classes;

namespace BedrockLauncher.Controls.Items
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
            ConfigManager.Default.SwitchProfile(_ProfileName);
            SelectorParent.ProfileContextMenu.IsOpen = false;
            ConfigManager.Default.OnConfigStateChanged(this, Events.ConfigStateArgs.Empty);
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
