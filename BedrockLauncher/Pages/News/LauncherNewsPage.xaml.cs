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
using BedrockLauncher.Extensions;
using BedrockLauncher.Downloaders;
using BedrockLauncher.Handlers;
using MdXaml;

namespace BedrockLauncher.Pages.News
{
    /// <summary>
    /// Interaction logic for LauncherNewsPage.xaml
    /// </summary>
    public partial class LauncherNewsPage : Page
    {

        private bool HasPreloaded = false;

        public LauncherNewsPage()
        {
            this.DataContext = ViewModels.NewsViewModel.Default;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, EventArgs e)
        {
            if (!HasPreloaded)
            {
                Task.Run(() => Downloaders.NewsDownloader.UpdateLauncherFeed(ViewModels.NewsViewModel.Default));
                HasPreloaded = true;
            }
        }


        private void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => ViewModels.MainViewModel.Updater.CheckForUpdatesAsync());
        }

        private void ForceUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.MainViewModel.Updater.UpdateButton_Click(sender, e);
        }

        private void CheckForUpdatesButton_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => Downloaders.NewsDownloader.UpdateLauncherFeed(ViewModels.NewsViewModel.Default));
        }
    }
}
