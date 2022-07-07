using System;
using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.UpdateProcessor.Authentication;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for InsiderSelector.xaml
    /// </summary>
    public partial class Setting_InsiderCombobox : Grid
    {
        public Setting_InsiderCombobox()
        {
            InitializeComponent();
        }

        private void ReloadList(bool save = false)
        {
            if (save)
            {
                int newIndex = AccountsList.SelectedIndex;
                if (newIndex <= -1) newIndex = 0;
                Properties.LauncherSettings.Default.CurrentInsiderAccountIndex = newIndex;
                Properties.LauncherSettings.Default.Save();
            }

            AuthenticationManager.Default.GetWUUsers();
            AccountsList.SelectedIndex = Properties.LauncherSettings.Default.CurrentInsiderAccountIndex;
        }

        private void AccountsList_DropDownClosed(object sender, EventArgs e)
        {
            ReloadList(true);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadList();
        }
    }
}
