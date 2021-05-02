using BedrockLauncher.Classes.SkinPack;
using BedrockLauncher.Methods;
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
            LoadHTML();
        }

        private void Renderer_ImageLoad(object sender, TheArtOfDev.HtmlRenderer.WPF.RoutedEvenArgs<TheArtOfDev.HtmlRenderer.Core.Entities.HtmlImageLoadEventArgs> args)
        {

        }

        public ChangelogPreviewPage()
        {
            InitializeComponent();
        }

        #region Closing Events

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.SetOverlayFrame(null);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.SetOverlayFrame(null);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.SetOverlayFrame(null);
        }


        #endregion

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void LoadHTML()
        {
            Renderer.Text = HTML;
        }

        private void Renderer_IsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            /*
            if (Renderer.IsBrowserInitialized)
            {
                Renderer.LoadHtml(HTML);
            }
            */
        }
    }
}
