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
using BedrockLauncher.Pages.NoContentScreen;

namespace BedrockLauncher.Pages.SettingsScreen
{
    /// <summary>
    /// Логика взаимодействия для SettingsScreen.xaml
    /// </summary>
    public partial class SettingsScreen : Page
    {
        public GeneralSettingsPage generalSettingsPage = new GeneralSettingsPage();
        public AccountsSettingsPage accountsSettingsPage = new AccountsSettingsPage();
        public NoContentPage noContentPage = new NoContentPage();
        public SettingsScreen()
        {
            InitializeComponent();
        }

        #region Navigation

        public void ResetButtonManager(ToggleButton toggleButton)
        {
            // just all buttons list
            // ya i know this is really bad, i need to learn mvvm instead of doing this shit
            // but this works fine, at least
            List<ToggleButton> toggleButtons = new List<ToggleButton>() {
                GeneralButton,
                AccountsButton,
                AboutButton
            };

            foreach (ToggleButton button in toggleButtons) { button.IsChecked = false; }
            toggleButton.IsChecked = true;
        }

        public void ButtonManager(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            ResetButtonManager(toggleButton);

            if (toggleButton.Name == GeneralButton.Name) NavigateToGeneralPage();
            else if (toggleButton.Name == AccountsButton.Name) NavigateToAccountsPage();
            else if (toggleButton.Name == AboutButton.Name) NavigateToAboutPage();
        }

        public void NavigateToGeneralPage()
        {
            SettingsScreenFrame.Navigate(generalSettingsPage);
        }

        public void NavigateToAccountsPage()
        {
            SettingsScreenFrame.Navigate(accountsSettingsPage);
        }

        public void NavigateToAboutPage()
        {
            SettingsScreenFrame.Navigate(noContentPage);
        }

        #endregion
    }
}
