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

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for AccountDropdown.xaml
    /// </summary>
    public partial class AccountDropdown : Grid
    {
        public AccountDropdown()
        {
            InitializeComponent();
        }

        public void RefreshProfileContextMenuItems()
        {
            var _userAccountsFetch = new Task(() =>
            {
                WUTokenHelper.GetWUUsers();
            });
            Task.Run(async () =>
            {
                _userAccountsFetch.Start();
                await _userAccountsFetch;
                await Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                {
                    AccountsList.ItemsSource = null;
                    AccountsList.ItemsSource = WUTokenHelper.CurrentAccounts;

                    if (WUTokenHelper.CurrentAccounts.Count < Properties.Settings.Default.CurrentMSAccount)
                    {
                        AccountsList.SelectedIndex = 0;
                    }
                    else AccountsList.SelectedIndex = Properties.Settings.Default.CurrentMSAccount;
                }));
            });
        }

        private void AccountsList_DropDownClosed(object sender, EventArgs e)
        {
            if (AccountsList.SelectedIndex == -1) AccountsList.SelectedIndex = 0;
            else if (WUTokenHelper.CurrentAccounts.Count < AccountsList.SelectedIndex) AccountsList.SelectedIndex = 0;
            Properties.Settings.Default.CurrentMSAccount = AccountsList.SelectedIndex;
            Properties.Settings.Default.Save();
            RefreshProfileContextMenuItems();
        }
    }
}
