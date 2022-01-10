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
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Play
{
    public partial class InstallationsScreen : Page
    {
        private bool HasLoadedOnce = false;
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
                var view = CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource) as CollectionView;
                if (!HasLoadedOnce)
                {
                    view.Filter = FilterSortingHandler.Filter_InstallationList;
                    HasLoadedOnce = true;
                }
                FilterSortingHandler.Sort_InstallationList(ref view);
                view.Refresh();
                if (InstallationsList.Items.Count > 0 && NothingFound.Visibility != Visibility.Collapsed) NothingFound.Visibility = Visibility.Collapsed;
                else if (InstallationsList.Items.Count <= 0 && NothingFound.Visibility != Visibility.Visible) NothingFound.Visibility = Visibility.Visible;
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
            if (HasLoadedOnce)
            {
                FilterSortingHandler.Installations_SearchFilter = SearchBox.Text;
                this.RefreshInstallations();
            }
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HasLoadedOnce)
            {

                if (SortByComboBox.SelectedItem == SortByLatestPlayed)
                    FilterSortingHandler.Installations_SortFilter = Enums.SortBy_Installation.LatestPlayed;

                if (SortByComboBox.SelectedItem == SortByName)
                    FilterSortingHandler.Installations_SortFilter = Enums.SortBy_Installation.Name;

                this.RefreshInstallations();
            }
        }

        private void InstallationsList_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            HasLoadedOnce = false;
            this.RefreshInstallations();
        }
    }
}
