//using CefSharp;
using BedrockLauncher.UI.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using BedrockLauncher.UI.ViewModels;
using System.Collections.Generic;

namespace BedrockLauncher.UI.Pages.Preview
{
    /// <summary>
    /// Interaction logic for BlankDialogScreen.xaml
    /// </summary>
    public partial class ChangelogPreviewPage : Page
    {

        public string HTML { get; set; } = string.Empty;

        public string URL { get; set; } = string.Empty;

        private const string HTMLStyle = "<style>body { color: white; } a { color: green; } img { height: auto; max-width: 100%; }</style>";
        private const string HTMLHeader = "<head>{0}{1}</head>";
        private const string HTMLFormat = "<!DOCTYPE html><html>{0}<body>{1}</body></html>";

        private string OptimizeHTML(string body)
        {
            return string.Format(HTMLFormat, UpdateHTMLHeader(new List<string>()), body);

            string UpdateHTMLHeader(List<string> styles)
            {
                string stylesheets = String.Join(Environment.NewLine, styles);
                return string.Format(HTMLHeader, stylesheets, HTMLStyle);
            }
        }

        public ChangelogPreviewPage(string html, string header, string url)
        {
            InitializeComponent();

            HTML = OptimizeHTML(html);
            Header.Text = header;
            URL = url;
        }

        public ChangelogPreviewPage(string html, string header)
        {
            InitializeComponent();

            HTML = OptimizeHTML(html);
            NonLinkHeader.Text = header;
            NonLinkSourceButton.Visibility = Visibility.Visible;
            SourceButton.Visibility = Visibility.Collapsed;
        }

        private void Renderer_ImageLoad(object sender, TheArtOfDev.HtmlRenderer.WPF.RoutedEventArgs<TheArtOfDev.HtmlRenderer.Core.Entities.HtmlImageLoadEventArgs> args)
        {

        }

        public ChangelogPreviewPage()
        {
            InitializeComponent();
        }

        #region Closing Events

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Handler.SetOverlayFrame(null);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Handler.SetOverlayFrame(null);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Handler.SetOverlayFrame(null);
        }


        #endregion

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private async void LoadHTML()
        {
            await Dispatcher.InvokeAsync(() => {
                Renderer.Text = HTML;
            });
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(URL));
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => LoadHTML());
        }
    }
}
