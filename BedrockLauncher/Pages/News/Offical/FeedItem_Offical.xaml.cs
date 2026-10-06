using BedrockLauncher.Classes.Launcher;
using System.Windows;
using System.Windows.Controls;

namespace BedrockLauncher.Pages.News.Official
{
    /// <summary>
    /// Interaction logic for FeedItem_Official.xaml
    /// </summary>
    public partial class FeedItem_Official : Button
    {
        public FeedItem_Official()
        {
            InitializeComponent();
        }

        public static void LoadArticle(News_Item item) => JemExtensions.WebExtensions.LaunchWebLink(item.Link);

        private void FeedItemEntry_Click(object sender, RoutedEventArgs e)
        {
            News_Item item = this.DataContext as News_Item;
            LoadArticle(item);
        }
    }
}
