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
using BedrockLauncher.Extensions;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Settings
{
    public partial class VersionsPage : Page
    {
        private bool hasInitalized = false;
        public VersionsPage()
        {
            this.DataContext = MainViewModel.Default;
            InitializeComponent();
        }

        public void RefreshVersionsList()
        {

        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            
        }

        private void RefreshVersionsList(object sender, RoutedEventArgs e)
        {

        }

        private void PageHost_Loaded(object sender, RoutedEventArgs e)
        {
            if (!hasInitalized)
            {
                foreach (var ver in MainViewModel.Default.Versions) ver.UpdateFolderSize();
                hasInitalized = true;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(Program.OnApplicationRefresh);
            foreach (var ver in MainViewModel.Default.Versions) ver.UpdateFolderSize();
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = FilterSortingHandler.Filter_VersionList(e.Item);
        }
    }
}
