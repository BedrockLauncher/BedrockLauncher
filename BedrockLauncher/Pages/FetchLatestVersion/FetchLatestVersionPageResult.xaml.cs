using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Classes;
using BedrockLauncher.Downloaders;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.FetchLatestVersion
{
    public partial class FetchLatestVersionPageResult : Page
    {
        private string _latestReleaseUuid;
        private string _latestPreviewUuid;

        public FetchLatestVersionPageResult()
        {
            InitializeComponent();
            UpdateFetcherResult fetcherResult = MainDataModel.Default.FetcherResult;
            if (fetcherResult.LatestRelease != null)
            {
                _latestReleaseUuid = fetcherResult.LatestRelease?.uuid;
                LatestReleaseName.Text = fetcherResult.LatestRelease?.version;
                CopyReleaseUUID.IsEnabled = true;
            }
            if (fetcherResult.LatestPreview != null)
            {
                _latestPreviewUuid = fetcherResult.LatestPreview?.uuid;
                LatestPreviewName.Text = fetcherResult.LatestPreview?.version;
                CopyPreviewUUID.IsEnabled = true;
            }
            Trace.WriteLine(fetcherResult);
            UpdateFetcherResult.Reset();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.MainViewModel.Default.SetOverlayFrame(null, true);
        }

        private void CopyReleaseUUID_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_latestReleaseUuid);
        }
        private void CopyPreviewUUID_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_latestPreviewUuid);
        }
    }
}
