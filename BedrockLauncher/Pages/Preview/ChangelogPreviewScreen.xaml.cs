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

namespace BedrockLauncher.Pages.Preview
{
    /// <summary>
    /// Interaction logic for BlankDialogScreen.xaml
    /// </summary>
    public partial class ChangelogPreviewPage : Page
    {

        public string HTML { get; set; } = string.Empty;

        public string URL { get; set; } = string.Empty;

        public ChangelogPreviewPage(string html, string header, string url)
        {
            InitializeComponent();
            HTML = html;
            Header.Text = header;
            URL = url;
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
            ViewModels.LauncherModel.Default.SetOverlayFrame(null);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.LauncherModel.Default.SetOverlayFrame(null);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.LauncherModel.Default.SetOverlayFrame(null);
        }


        #endregion

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(URL));
        }
    }
}
