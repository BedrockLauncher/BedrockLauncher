using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Classes.Launcher;
using BedrockLauncher.UI.Pages.Preview;
using Markdig;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for FeedItem_Launcher.xaml
    /// </summary>
    public partial class FeedItem_Launcher : Button
    {
        public FeedItem_Launcher()
        {
            InitializeComponent();
        }

        private void FeedItemButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            AppPatchNote item = button.DataContext as AppPatchNote;
            LoadChangelog(item);
        }

        public static void LoadChangelog(AppPatchNote item)
        {
            string header_title = string.Format("{0} {1}", (item.isBeta ? "Beta" : "Release"), item.tag_name); //TODO: Localize
            string html = Markdown.ToHtml(item.body);
            ViewModels.MainViewModel.Default.SetOverlayFrame(new ChangelogPreviewPage(html, header_title, item.html_url));
        }
    }
}
