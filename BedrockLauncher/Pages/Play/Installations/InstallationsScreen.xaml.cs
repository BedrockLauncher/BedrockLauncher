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
using BedrockLauncher.Enums;
using BedrockLauncher.Handlers;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.Pages.Preview.Installation;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Play.Installations
{
    public partial class InstallationsScreen : Page
    {

        public InstallationsScreen()
        {
            InitializeComponent();
            this.DataContext = MainDataModel.Default;
            ShowBetasCheckBox.Click += (sender, e) => RefreshInstallations();
            ShowReleasesCheckBox.Click += (sender, e) => RefreshInstallations();
            ShowPreviewsCheckBox.Click += (sender, e) => RefreshInstallations();
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
            switch (Properties.LauncherSettings.Default.InstallationsSortMode)
            {
                case Enums.InstallationSort.LatestPlayed:
                    SortByComboBox.SelectedItem = SortByLatestPlayed;
                    break;
                case Enums.InstallationSort.Name:
                    SortByComboBox.SelectedItem = SortByName;
                    break;
                case Enums.InstallationSort.None:
                    SortByComboBox.SelectedItem = SortByNone;
                    break;
                default:
                    SortByComboBox.SelectedItem = SortByLatestPlayed;
                    break;
            }
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
                Properties.LauncherSettings.Default.InstallationsSortMode = Enums.InstallationSort.LatestPlayed;

            if (SortByComboBox.SelectedItem == SortByName)
                Properties.LauncherSettings.Default.InstallationsSortMode = Enums.InstallationSort.Name;

            if (SortByComboBox.SelectedItem == SortByNone)
                Properties.LauncherSettings.Default.InstallationsSortMode = Enums.InstallationSort.None;

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
