using BedrockLauncher.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BedrockLauncher.Components;
using BedrockLauncher.UI.Components;

namespace BedrockLauncher.Pages.Settings
{
    public partial class SettingsTabs : Page
    {
        public GeneralSettingsPage generalSettingsPage = new GeneralSettingsPage();
        public AccountsSettingsPage accountsSettingsPage = new AccountsSettingsPage();
        public VersionsPage versionsSettingsPage = new VersionsPage();
        public AboutPage aboutPage = new AboutPage();

        private Navigator Navigator { get; set; } = new Navigator();

        public SettingsTabs()
        {
            InitializeComponent();
            ButtonManager_Base(GeneralButton.Name);
        }

        #region Navigation

        public void ResetButtonManager(string buttonName)
        {
            this.Dispatcher.Invoke(() =>
            {
                // just all buttons list
                // ya i know this is really bad, i need to learn mvvm instead of doing this shit
                // but this works fine, at least
                List<ToggleButton> toggleButtons = new List<ToggleButton>() {
                GeneralButton,
                VersionsButton,
                AccountsButton,
                AboutButton
            };

                foreach (ToggleButton button in toggleButtons) { button.IsChecked = false; }

                if (toggleButtons.Exists(x => x.Name == buttonName))
                {
                    toggleButtons.Where(x => x.Name == buttonName).FirstOrDefault().IsChecked = true;
                }
            });

        }

        public void ButtonManager(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                var toggleButton = sender as ToggleButton;
                string name = toggleButton.Name;
                Task.Run(() => ButtonManager_Base(name));
            });
        }
        public void ButtonManager_Base(string senderName)
        {
            this.Dispatcher.Invoke(() =>
            {
                ResetButtonManager(senderName);

                if (senderName == GeneralButton.Name) NavigateToGeneralPage();
                else if (senderName == AccountsButton.Name) NavigateToAccountsPage();
                else if (senderName == AboutButton.Name) NavigateToAboutPage();
                else if (senderName == VersionsButton.Name) NavigateToVersionsPage();
            });
        }

        public void NavigateToGeneralPage()
        {
            Navigator.UpdatePageIndex(0);
            Task.Run(() => Navigator.Navigate(SettingsScreenFrame,generalSettingsPage));
        }
        public void NavigateToVersionsPage()
        {
            Navigator.UpdatePageIndex(1);
            Task.Run(() => Navigator.Navigate(SettingsScreenFrame,versionsSettingsPage));
        }

        public void NavigateToAccountsPage()
        {
            Navigator.UpdatePageIndex(2);
            Task.Run(() => Navigator.Navigate(SettingsScreenFrame,accountsSettingsPage));
        }

        public void NavigateToAboutPage()
        {
            Navigator.UpdatePageIndex(3);
            Task.Run(() => Navigator.Navigate(SettingsScreenFrame,aboutPage));
        }

        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
