using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Classes.Launcher;

namespace BedrockLauncher.Pages.News.Offical
{
    /// <summary>
    /// Interaction logic for FeedItem_Offical.xaml
    /// </summary>
    public partial class FeedItem_Offical : Button
    {
        public FeedItem_Offical()
        {
            InitializeComponent();
        }

        public static void LoadArticle(News_Item item)
        {
            JemExtensions.WebExtensions.LaunchWebLink(item.Link);
        }

        private void FeedItemEntry_Click(object sender, RoutedEventArgs e)
        {
            News_Item item = this.DataContext as News_Item;
            LoadArticle(item);
        }
    }
}
