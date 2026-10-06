using BedrockLauncher.UpdateProcessor.Authentication;
using System;
using System.Windows;
using System.Windows.Controls;

namespace BedrockLauncher.Pages.Settings.Accounts
{
    public partial class AccountsSettingsPage : Page
    {
        public AccountsSettingsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadInsiderAccounts();
        }

        private void LoadInsiderAccounts()
        {
            try
            {
                AuthenticationManager.Default.GetWUUsers();
                AccountComboBox.ItemsSource = AuthenticationManager.Default.CurrentAccounts;

                int savedIndex = Properties.LauncherSettings.Default.CurrentInsiderAccountIndex;
                if (savedIndex >= 0 && savedIndex < AccountComboBox.Items.Count)
                {
                    AccountComboBox.SelectedIndex = savedIndex;
                }
                else
                {
                    AccountComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Error on loading the accounts: " + ex.Message);
                AccountComboBox.ItemsSource = null;
                AccountComboBox.Items.Clear();
                AccountComboBox.Items.Add(
                    new AuthenticationAccount
                    {
                        UserName = "Default Account",
                        AccountType = "(No Authentication)"
                    });
                AccountComboBox.SelectedIndex = 0;
            }
        }

        private void AccountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.LauncherSettings.Default.CurrentInsiderAccountIndex = AccountComboBox.SelectedIndex;
            Properties.LauncherSettings.Default.Save();
        }

        private void XboxInsiderNew_Click(object sender, RoutedEventArgs e)
        {
            JemExtensions.WebExtensions.LaunchWebLink("xbox-insider2://");
        }

        private void XboxInsiderLegacy_Click(object sender, RoutedEventArgs e)
        {
            JemExtensions.WebExtensions.LaunchWebLink("xbox-insider://");
        }

        private void MSAccounts_Click(object sender, RoutedEventArgs e)
        {
            JemExtensions.WebExtensions.LaunchWebLink(
                "ms-settings:emailandaccounts");
        }
    }
}