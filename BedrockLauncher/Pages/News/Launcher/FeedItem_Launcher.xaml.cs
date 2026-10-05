using BedrockLauncher.Classes.Launcher;
using BedrockLauncher.UI.Pages.Preview;
using Markdig;
using System.Windows;
using System.Windows.Controls;

namespace BedrockLauncher.Pages.News.Launcher
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
            PatchNote_Launcher item = button.DataContext as PatchNote_Launcher;
            LoadChangelog(item);
        }

        public static void LoadChangelog(PatchNote_Launcher item)
        {
            string channel = item.isBeta
                ? (Localization.Language.LanguageManager.GetResource("GeneralText_Beta") as string ?? "Beta")
                : (Localization.Language.LanguageManager.GetResource("GeneralText_Releases") as string ?? "Release");
            string header_title = string.Format("{0} {1}", channel, item.tag_name);
            string html = Markdown.ToHtml(item.body);
            ViewModels.MainViewModel.Default.SetOverlayFrame(new ChangelogPreviewPage(html, header_title, item.html_url));
        }
    }
}
