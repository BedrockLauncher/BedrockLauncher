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
using BedrockLauncher.Downloaders;
using BedrockLauncher.UpdateProcessor;
using BedrockLauncher.UpdateProcessor.Authentication;

namespace BedrockLauncher.Controls.Config
{
    /// <summary>
    /// Interaction logic for InsiderSelector.xaml
    /// </summary>
    public partial class InsiderCombobox : Grid
    {
        public InsiderCombobox()
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
