using BedrockLauncher.Classes.SkinPack;
using BedrockLauncher.Core;
using CefSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace BedrockLauncher.Pages
{
    /// <summary>
    /// Interaction logic for BlankDialogScreen.xaml
    /// </summary>
    public partial class ChangelogPreviewPage : Page
    {

        public string HTML { get; set; } = string.Empty;

        public ChangelogPreviewPage(string html, string header, string url)
        {
            InitializeComponent();
            HTML = html;
            Header.Text = header;
            SourceHyperlink.NavigateUri = new Uri(url);
            Clipboard.SetText(HTML);
            Renderer.Text = HTML;
        }

        public ChangelogPreviewPage()
        {
            InitializeComponent();
        }

        #region Closing Events

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.MainWindowOverlayFrame.Content = null;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.MainWindowOverlayFrame.Content = null;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.MainWindowOverlayFrame.Content = null;
        }


        #endregion

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
