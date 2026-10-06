using BedrockLauncher.Classes;
using BedrockLauncher.Classes.Launcher;
using BedrockLauncher.Handlers;
using BedrockLauncher.Pages.General;
using BedrockLauncher.ViewModels;
using CodeHollow.FeedReader;
using JemExtensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
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

namespace BedrockLauncher.Pages.News.Official
{
    /// <summary>
    /// Interaction logic for OfficialNewsPage.xaml
    /// </summary>
    public partial class OfficialNewsPage : Page
    {
        private bool hasPreloaded = false;



        public OfficialNewsPage()
        {
            this.DataContext = NewsViewModel.Default;
            InitializeComponent();
        }

        public void RefreshNews() => Task.Run(() => Downloaders.NewsDownloader.UpdateOfficialFeed(ViewModels.NewsViewModel.Default));

        private void OfficialNewsFeed_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (OfficialNewsFeed.SelectedItem != null)
                {
                    var item = OfficialNewsFeed.SelectedItem as News_OfficialItem;
                    FeedItem_Official.LoadArticle(item);
                }
            }
        }

        private void UpdateContent()
        {
            Dispatcher.Invoke(() =>
            {
                NothingFound.Visibility = Visibility.Visible;
                NothingFound.PanelType = ResultPanelType.Loading;
            });

            Dispatcher.Invoke(() =>
            {
                Handlers.FilterSortingHandler.Refresh(OfficialNewsFeed.ItemsSource);
                if (OfficialNewsFeed.Items.Count == 0) NothingFound.PanelType = ResultPanelType.NoNews;
                else NothingFound.Visibility = Visibility.Collapsed;
            });
        }



        private void CheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;
            UpdateContent();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.IsInitialized) return;
            UpdateContent();
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e) => e.Accepted = FilterSortingHandler.Filter_OfficialNewsFeed(e.Item);

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!hasPreloaded)
            {
                Task.Run(() => Downloaders.NewsDownloader.UpdateOfficialFeed(ViewModels.NewsViewModel.Default));
                hasPreloaded = true;
            }
        }
    }
}
