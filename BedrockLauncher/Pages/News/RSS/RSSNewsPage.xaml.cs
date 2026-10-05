using BedrockLauncher.Classes.Launcher;
using CodeHollow.FeedReader;
using RestSharp;
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

namespace BedrockLauncher.Pages.News.RSS
{
    /// <summary>
    /// Interaction logic for RSSNewsPage.xaml
    /// </summary>
    public partial class RSSNewsPage : Page
    {


        private bool hasPreloaded = false;


        public RSSNewsPage(ViewModels.RSSViewModel dataContext)
        {
            DataContext = dataContext;
            InitializeComponent();
        }

        public void RefreshNews() => Task.Run(((ViewModels.RSSViewModel)DataContext).UpdateFeed);

        private void Page_Loaded(object sender, EventArgs e)
        {
            if (!hasPreloaded)
            {
                Task.Run(((ViewModels.RSSViewModel)DataContext).UpdateFeed);
                hasPreloaded = true;
            }

        }

        private void OfficialNewsFeed_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (OfficialNewsFeed.SelectedItem != null)
                {
                    var item = OfficialNewsFeed.SelectedItem as News_Item;
                    item.OpenLink();
                }
            }
        }
    }
}
