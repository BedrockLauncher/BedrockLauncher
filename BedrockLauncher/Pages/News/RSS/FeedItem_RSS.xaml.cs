using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Classes.Launcher;

namespace BedrockLauncher.Pages.News.RSS
{
    /// <summary>
    /// Interaction logic for FeedItem_RSS.xaml
    /// </summary>
    public partial class FeedItem_RSS : Button
    {
        public FeedItem_RSS()
        {
            InitializeComponent();
        }

        private void FeedItemEntry_Click(object sender, RoutedEventArgs e)
        {
            News_Item item = this.DataContext as News_Item;
            item.OpenLink();
        }
    }
}
