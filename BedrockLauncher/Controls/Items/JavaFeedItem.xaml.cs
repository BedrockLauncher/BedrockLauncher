using BedrockLauncher.Classes;
using BedrockLauncher.Core.Classes;
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

namespace BedrockLauncher.Controls.Items
{
    /// <summary>
    /// Interaction logic for JavaFeedItem.xaml
    /// </summary>
    public partial class JavaFeedItem : Button
    {
        public JavaFeedItem()
        {
            InitializeComponent();
        }

        public static void LoadArticle(MCNetFeedItem item)
        {
            Process.Start(new ProcessStartInfo(item.Link));
        }

        private void FeedItemEntry_Click(object sender, RoutedEventArgs e)
        {
            MCNetFeedItem item = this.DataContext as MCNetFeedItem;
            LoadArticle(item);
        }
    }
}
