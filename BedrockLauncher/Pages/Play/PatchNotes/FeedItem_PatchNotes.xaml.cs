using BedrockLauncher.Classes.Launcher;
using BedrockLauncher.Downloaders;
using BedrockLauncher.UI.Pages.Preview;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BedrockLauncher.Pages.Play.PatchNotes
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

        private async void FeedItemButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            PatchNotes_Game_Item item = button.DataContext as PatchNotes_Game_Item;
            await LoadChangelog(item);
        }
        public static async Task LoadChangelog(PatchNotes_Game_Item item)
        {
            try
            {
                var response = await SharedHttpClient.Instance.GetStringAsync(Constants.PATCHNOTES_CONTENT_BASE_URL + item.contentPath);
                using var doc = JsonDocument.Parse(response);
                item.body = doc.RootElement.GetProperty("body").GetString();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }

            ViewModels.MainViewModel.Default.SetOverlayFrame(new ChangelogPreviewPage(item.body, item.title));
        }

        private ImageSource ToImageSource(string path, bool isFallback)
        {
            if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri url))
                return new BitmapImage(url);
            else if (!isFallback) return ToImageSource((this.DataContext as PatchNotes_Game_Item).image_url, true);
            else return null;
        }

        private void RealImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            var dataContext = this.DataContext as PatchNotes_Game_Item;
            RealImage.SetCurrentValue(Image.SourceProperty, ToImageSource(dataContext.fallback_image, true));
        }

        private void RealImage_Loaded(object sender, RoutedEventArgs e)
        {
            var dataContext = this.DataContext as PatchNotes_Game_Item;
            RealImage.SetCurrentValue(Image.SourceProperty, ToImageSource(dataContext.image_url, false));
        }
    }
}
