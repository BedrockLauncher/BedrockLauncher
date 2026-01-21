using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Downloaders;

namespace BedrockLauncher.Pages.FetchLatestVersion
{
    public partial class FetchLatestVersionPageOne : Page
    {
        public FetchLatestVersionPagesSwitcher pageSwitcher = new FetchLatestVersionPagesSwitcher();
        public FetchLatestVersionPageOne()
        {
            InitializeComponent();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.MainViewModel.Default.SetOverlayFrame(null, true);
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;
            try
            {
                GDKVersionFetcher.FetchLatestUpdate();
            }
            catch (Exception ex)
            {
                LoginButton.IsEnabled = true;
            }
        }
    }
}
