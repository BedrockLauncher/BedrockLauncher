using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Classes.Launcher;

namespace BedrockLauncher.Controls
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
            NewsItem item = this.DataContext as NewsItem;
            item.OpenLink();
        }
    }
}
