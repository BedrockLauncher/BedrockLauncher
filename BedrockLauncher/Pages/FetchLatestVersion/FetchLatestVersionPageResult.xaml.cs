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
        public FetchLatestVersionPageResult()
        {
            InitializeComponent();
            UpdateFetcherResult.Reset();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.MainViewModel.Default.SetOverlayFrame(null, true);
        }
    }
}
