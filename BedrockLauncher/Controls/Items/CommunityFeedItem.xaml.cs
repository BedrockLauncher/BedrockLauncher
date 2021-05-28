using BedrockLauncher.Classes;
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
    /// Interaction logic for CommunityFeedItem.xaml
    /// </summary>
    public partial class CommunityFeedItem : ListViewItem
    {
        public CommunityFeedItem()
        {
            InitializeComponent();
        }

        public CommunityFeedItem(MCNetFeedItem item)
        {
            this.DataContext = item;
            InitializeComponent();
        }

        private void ClickEvent(object sender)
        {
            ListViewItem button = sender as ListViewItem;
            MCNetFeedItem item = button.DataContext as MCNetFeedItem;
            LoadArticle(item);
        }

        public static void LoadArticle(MCNetFeedItem item)
        {
            Process.Start(new ProcessStartInfo(item.Link));
        }

        private void FeedItemEntry_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released) ClickEvent(sender);
        }

        private void FeedItemEntry_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ClickEvent(sender);
        }

        private void SetSelectionColor()
        {
            bool isSelected = IsMouseOver || IsSelected;
            if (isSelected) this.MainGrid.SetResourceReference(BackgroundProperty, "MCNetFeedItem.Selected.Background");
            else this.MainGrid.SetResourceReference(BackgroundProperty, "MCNetFeedItem.Static.Background");
        }

        private void FeedItemEntry_Selected(object sender, RoutedEventArgs e)
        {
            SetSelectionColor();
        }

        private void FeedItemEntry_MouseEnter(object sender, MouseEventArgs e)
        {
            SetSelectionColor();
        }

        private void FeedItemEntry_MouseLeave(object sender, MouseEventArgs e)
        {
            SetSelectionColor();
        }
    }
}
