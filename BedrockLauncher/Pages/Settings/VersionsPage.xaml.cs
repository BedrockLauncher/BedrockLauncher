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
using BedrockLauncher.Handlers;
using BedrockLauncher.Methods;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Settings
{
    public partial class VersionsPage : Page
    {

        private bool HasLoadedOnce = false;
        public VersionsPage()
        {
            InitializeComponent();
            this.DataContext = MainViewModel.Default;
        }

        public void RefreshVersionsList()
        {
            this.Dispatcher.Invoke(() =>
            {
                var view = CollectionViewSource.GetDefaultView(VersionsList.ItemsSource) as CollectionView;
                view.Refresh();
            });
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            
        }

        private void RefreshVersionsList(object sender, RoutedEventArgs e)
        {
            RefreshVersionsList();
        }

        private void PageHost_Loaded(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (!HasLoadedOnce)
                {
                    var view = CollectionViewSource.GetDefaultView(VersionsList.ItemsSource) as CollectionView;
                    view.Filter = FilterSortingHandler.Filter_VersionList;
                    HasLoadedOnce = true;
                }
            });
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(ViewModels.MainViewModel.Default.LoadVersions);
        }
    }
}
