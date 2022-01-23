using BedrockLauncher.Classes.Launcher;
using BedrockLauncher.Extensions;
using BedrockLauncher.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.UI.Pages.Preview;

namespace BedrockLauncher.Controls.Items.News
{
    /// <summary>
    /// Interaction logic for FeedItem_PatchNotes.xaml
    /// </summary>
    public partial class FeedItem_PatchNotes : Button
    {
        public FeedItem_PatchNotes()
        {
            InitializeComponent();
        }

        private void FeedItemButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            PatchNote item = button.DataContext as PatchNote;
            LoadChangelog(item);
        }

        public static void LoadChangelog(PatchNote item)
        {
            string header_title = string.Format("{0} {1}", (item.isBeta ? "Beta" : "Release"), item.Version);
            ViewModels.MainViewModel.Default.SetOverlayFrame(new ChangelogPreviewPage(item.Content, header_title, item.Url));
        }

        private ImageSource ToImageSource(string path, bool isFallback)
        {
            if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri url))
                return new BitmapImage(url);
            else if (!isFallback) return ToImageSource((this.DataContext as PatchNote).FallbackImage, true);
            else return null;
        }

        private void RealImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            var dataContext = this.DataContext as PatchNote;
            RealImage.SetCurrentValue(Image.SourceProperty, ToImageSource(dataContext.FallbackImage, true));
        }

        private void RealImage_Loaded(object sender, RoutedEventArgs e)
        {
            var dataContext = this.DataContext as PatchNote;
            RealImage.SetCurrentValue(Image.SourceProperty, ToImageSource(dataContext.ImageUrl, false));
        }
    }
}
