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
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Play
{
    public partial class InstallationsScreen : Page
    {

        public InstallationsScreen()
        {
            InitializeComponent();
            this.DataContext = MainViewModel.Default;
            ShowBetasCheckBox.Click += (sender, e) => RefreshInstallations();
            ShowReleasesCheckBox.Click += (sender, e) => RefreshInstallations();
        }
        public void RefreshInstallations()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (InstallationsList != null) FilterSortingHandler.Sort_InstallationList(InstallationsList.ItemsSource);
            });
        }
        private void NewInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Default.SetOverlayFrame(new EditInstallationScreen());
        }
        private void PageHost_Loaded(object sender, RoutedEventArgs e)
        {
            this.RefreshInstallations();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterSortingHandler.InstallationsSearchFilter = SearchBox.Text;
            this.RefreshInstallations();
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortByComboBox.SelectedItem == SortByLatestPlayed)
                FilterSortingHandler.InstallationsSortMode = Enums.InstallationSort.LatestPlayed;

            if (SortByComboBox.SelectedItem == SortByName)
                FilterSortingHandler.InstallationsSortMode = Enums.InstallationSort.Name;

            this.RefreshInstallations();
        }

        private void InstallationsList_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            this.RefreshInstallations();
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = Handlers.FilterSortingHandler.Filter_InstallationList(e.Item);
        }
    }
}
