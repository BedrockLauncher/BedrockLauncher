using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Classes;
using BedrockLauncher.Downloaders;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.FetchLatestVersion
{
    public partial class FetchLatestVersionPageContent : Page
    {
        public FetchLatestVersionPageContent()
        {
            InitializeComponent();
            UpdateFetcherResult.Reset();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.MainViewModel.Default.SetOverlayFrame(null, true);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            FetchLatestVersionPage.FetchLatestVersionContainer.FetchLatestVersionPageFrame.Navigate(new FetchLatestVersionPageResult());
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;
            try
            {
                GDKVersionFetcher.FetchLatestUpdates();
                MainDataModel.Default.FetcherResult.State = Enums.VersionFetcherState.Connecting;
            }
            catch (Exception ex)
            {
                LoginButton.IsEnabled = true;
                UpdateFetcherResult.Reset();
            }
        }
    }
}
