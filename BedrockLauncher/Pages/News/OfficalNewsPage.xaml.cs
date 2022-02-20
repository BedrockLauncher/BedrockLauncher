using System;
using System.Collections.Generic;
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
using CodeHollow.FeedReader;
using BedrockLauncher.Classes;
using BedrockLauncher.Classes.Launcher;
using System.Diagnostics;
using BedrockLauncher.Controls.Items;
using System.Net;
using System.Net.Http;
using System.Collections.ObjectModel;
using JemExtensions;
using BedrockLauncher.ViewModels;
using BedrockLauncher.Handlers;
using BedrockLauncher.Controls.Items.News;

namespace BedrockLauncher.Pages.News
{
    /// <summary>
    /// Interaction logic for OfficalNewsPage.xaml
    /// </summary>
    public partial class OfficalNewsPage : Page
    {
        private bool hasPreloaded = false;



        public OfficalNewsPage()
        {
            this.DataContext = NewsViewModel.Default;
            InitializeComponent();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => Downloaders.NewsDownloader.UpdateOfficalFeed(ViewModels.NewsViewModel.Default));
        }

        private void OfficalNewsFeed_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (OfficalNewsFeed.SelectedItem != null)
                {
                    var item = OfficalNewsFeed.SelectedItem as NewsItem_Offical;
                    FeedItem_Offical.LoadArticle(item);
                }
            }
        }

        private void UpdateContent()
        {
            Dispatcher.Invoke(() =>
            {
                NothingFound.Visibility = Visibility.Visible;
                NothingFound.PanelType = Controls.Various.ResultPanelType.Loading;
            });

            Dispatcher.Invoke(() => 
            {
                Handlers.FilterSortingHandler.Refresh(OfficalNewsFeed.ItemsSource);
                if (OfficalNewsFeed.Items.Count == 0) NothingFound.PanelType = Controls.Various.ResultPanelType.NoNews;
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

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = FilterSortingHandler.Filter_OfficalNewsFeed(e.Item);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!hasPreloaded)
            {
                Task.Run(() => Downloaders.NewsDownloader.UpdateOfficalFeed(ViewModels.NewsViewModel.Default));
                hasPreloaded = true;
            }
        }
    }
}
