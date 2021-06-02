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
using System.Diagnostics;
using BedrockLauncher.Controls.Items;
using System.Net;
using System.Net.Http;
using System.Collections.ObjectModel;
using ExtensionsDotNET;

namespace BedrockLauncher.Pages.News
{
    /// <summary>
    /// Interaction logic for Java_N_DungeonsNewsPage.xaml
    /// </summary>
    public partial class Java_N_DungeonsNewsPage : Page
    {

        public class Java_N_DungeonsContext : BL_Core.Components.NotifyPropertyChangedBase
        {
            private bool _ShowJavaContent = true;
            private bool _ShowDungeonsContent = true;

            public bool ShowJavaContent
            {
                get
                {
                    return _ShowJavaContent;
                }
                set
                {
                    _ShowJavaContent = value;
                    OnPropertyChanged(nameof(ShowJavaContent));
                }
            }

            public bool ShowDungeonsContent
            {
                get
                {
                    return _ShowDungeonsContent;
                }
                set
                {
                    _ShowDungeonsContent = value;
                    OnPropertyChanged(nameof(ShowDungeonsContent));
                }
            }

            public ObservableCollection<JavaFeedItem> FeedItems { get; set; } = new ObservableCollection<JavaFeedItem>();

            public void UpdateSource()
            {
                OnPropertyChanged(nameof(FeedItems));
            }
        }


        private const string JSON_Feed = @"https://launchercontent.mojang.com/news.json";


        public async Task<MCNetLauncherFeed> GetJSON()
        {
            MCNetLauncherFeed result = null;
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var json = await httpClient.GetStringAsync(JSON_Feed);
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<MCNetLauncherFeed>(json);
                }
                catch
                {
                    result = new MCNetLauncherFeed();
                }

            }
            if (result == null) result = new MCNetLauncherFeed();
            return result;
        }

        public Java_N_DungeonsNewsPage()
        {
            this.DataContext = new Java_N_DungeonsContext();
            InitializeComponent();
        }

        private async void UpdateRSSContent()
        {
            await Dispatcher.InvokeAsync(() => 
            { 
                (this.DataContext as Java_N_DungeonsContext).FeedItems.Clear(); 
            });



            var news = await GetJSON();

            await Dispatcher.InvokeAsync(() => {
                foreach (MCNetFeedItemJSON item in news.entries)
                {
                    if (item.newsType != null && item.newsType.Contains("News page")) (this.DataContext as Java_N_DungeonsContext).FeedItems.Add(new JavaFeedItem(item));
                }
            });
        }

        private async void Page_Loaded(object sender, EventArgs e)
        {
            //TODO: Fix the Binding Errors Caused by Not Reseting these Values
            (this.DataContext as Java_N_DungeonsContext).ShowDungeonsContent = true;
            (this.DataContext as Java_N_DungeonsContext).ShowJavaContent = true;
            this.SearchBox.Text = string.Empty;

            await Task.Run(() => UpdateRSSContent());
        }

        private void OfficalNewsFeed_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (OfficalNewsFeed.SelectedItem != null)
                {
                    var item = OfficalNewsFeed.SelectedItem as MCNetFeedItemJSON;
                    JavaFeedItem.LoadArticle(item);
                }
            }
        }

        private void UpdateContent()
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(OfficalNewsFeed.ItemsSource);
            view.Filter = IsAllowed;

            bool IsAllowed(object obj)
            {
                if (!(obj is JavaFeedItem)) return false;
                else
                {
                    var item = ((obj as JavaFeedItem).DataContext as MCNetFeedItemJSON);
                    if (item.newsType != null && item.newsType.Contains("News page"))
                    {
                        var context = (this.DataContext as Java_N_DungeonsContext);
                        if (item.category == "Minecraft: Java Edition" && context.ShowJavaContent) return FilterState(item);
                        else if (item.category == "Minecraft Dungeons" && context.ShowDungeonsContent) return FilterState(item);
                        else return false;
                    }
                    else return false;
                }
            }

            bool FilterState(MCNetFeedItemJSON item)
            {
                string searchParam = SearchBox.Text;
                if (string.IsNullOrEmpty(searchParam) || item.title.Contains(searchParam, StringComparison.OrdinalIgnoreCase)) return true;
                else return false;

            }
        }

        private void CheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;
            UpdateContent();
            (this.DataContext as Java_N_DungeonsContext).UpdateSource();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.IsInitialized) return;
            UpdateContent();
            (this.DataContext as Java_N_DungeonsContext).UpdateSource();
        }
    }
}
