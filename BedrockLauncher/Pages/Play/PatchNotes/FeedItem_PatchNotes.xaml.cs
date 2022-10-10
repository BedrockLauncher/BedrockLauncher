using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BedrockLauncher.Classes.Launcher;
using BedrockLauncher.UI.Pages.Preview;

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

        private void FeedItemButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            PatchNotes_Game_Item item = button.DataContext as PatchNotes_Game_Item;
            LoadChangelog(item);
        }

        public static void LoadChangelog(PatchNotes_Game_Item item)
        {
            ViewModels.MainViewModel.Default.SetOverlayFrame(new ChangelogPreviewPage(item.body, item.title, ""));
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
