using BedrockLauncher.Methods;
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

namespace BedrockLauncher.Pages.Settings
{
    public partial class SettingsTabs : Page
    {
        public GeneralSettingsPage generalSettingsPage = new GeneralSettingsPage();
        public AccountsSettingsPage accountsSettingsPage = new AccountsSettingsPage();
        public VersionsSettingsPage versionsSettingsPage = new VersionsSettingsPage();
        public AboutPage aboutPage = new AboutPage();
        public SettingsTabs()
        {
            InitializeComponent();
            this.SettingsScreenFrame.Navigating += SettingsScreenFrame_Navigating;
            ConfigManager.ConfigStateChanged += ConfigManager_ConfigStateChanged;
            NavigateToGeneralPage();
        }

        private void ConfigManager_ConfigStateChanged(object sender, EventArgs e)
        {
            RefreshVersions();
        }

        private void SettingsScreenFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            int CurrentPageIndex = ViewModels.LauncherModel.Default.CurrentPageIndex_Settings;
            int LastPageIndex = ViewModels.LauncherModel.Default.LastPageIndex_Settings;
            if (CurrentPageIndex == LastPageIndex) return;

            ExpandDirection direction;

            if (CurrentPageIndex > LastPageIndex) direction = ExpandDirection.Right;
            else direction = ExpandDirection.Left;

            Components.PageAnimator.FrameSwipe(SettingsScreenFrame, SettingsScreenFrame.Content, direction);
        }

        #region UI

        public void RefreshVersions()
        {
            versionsSettingsPage.VersionsList.ItemsSource = ConfigManager.Versions;
            var view = CollectionViewSource.GetDefaultView(versionsSettingsPage.VersionsList.ItemsSource) as CollectionView;
            view.Filter = ConfigManager.Filter_VersionList;
        }

        #endregion

        #region Navigation

        public void ResetButtonManager(ToggleButton toggleButton)
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
            toggleButton.IsChecked = true;
        }

        public void ButtonManager(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            ResetButtonManager(toggleButton);

            if (toggleButton.Name == GeneralButton.Name) NavigateToGeneralPage();
            else if (toggleButton.Name == AccountsButton.Name) NavigateToAccountsPage();
            else if (toggleButton.Name == AboutButton.Name) NavigateToAboutPage();
            else if (toggleButton.Name == VersionsButton.Name) NavigateToVersionsPage();
        }

        public void NavigateToGeneralPage()
        {
            ViewModels.LauncherModel.Default.UpdateSettingsPageIndex(0);
            SettingsScreenFrame.Navigate(generalSettingsPage);
        }
        public void NavigateToVersionsPage()
        {
            ViewModels.LauncherModel.Default.UpdateSettingsPageIndex(1);
            SettingsScreenFrame.Navigate(versionsSettingsPage);
        }

        public void NavigateToAccountsPage()
        {
            ViewModels.LauncherModel.Default.UpdateSettingsPageIndex(2);
            SettingsScreenFrame.Navigate(accountsSettingsPage);
        }

        public void NavigateToAboutPage()
        {
            ViewModels.LauncherModel.Default.UpdateSettingsPageIndex(3);
            SettingsScreenFrame.Navigate(aboutPage);
        }

        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
