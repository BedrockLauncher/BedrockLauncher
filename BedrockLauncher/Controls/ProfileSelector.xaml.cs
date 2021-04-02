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
using BedrockLauncher.Core;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for ProfileSelector.xaml
    /// </summary>
    public partial class ProfileSelector : Grid
    {
        private string _ProfileName { get; set; }
        private ProfileButton SelectorParent { get; set; }
        public ProfileSelector()
        {
            InitializeComponent();
        }

        public ProfileSelector(KeyValuePair<string, ProfileSettings> profile, ProfileButton _selectorParent)
        {
            InitializeComponent();
            this.SelectorParent = _selectorParent;
            this.Tag = profile.Value;
            this.ProfileName.Text = profile.Key;
            _ProfileName = profile.Key;
            if (Properties.Settings.Default.CurrentProfile == profile.Key) SelectedMark.Visibility = Visibility.Visible;
        }

        private void SwitchProfile()
        {
            ConfigManager.SwitchProfile(_ProfileName);
            SelectorParent.ProfileContextMenu.IsOpen = false;
            ConfigManager.OnConfigStateChanged(this, ConfigManager.ConfigStateArgs.Empty);
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
