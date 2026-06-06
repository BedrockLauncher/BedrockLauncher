using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using System.Windows.Threading;
using BedrockLauncher.Enums;
using BedrockLauncher.Handlers;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.Pages.Preview.Installation;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Play.Installations
{
    public partial class InstallationsScreen : Page
    {
        private INotifyCollectionChanged _subscribedInstallations;

        public InstallationsScreen()
        {
            InitializeComponent();
            this.DataContext = MainDataModel.Default;
            ShowBetasCheckBox.Click += (sender, e) => RefreshInstallations();
            ShowReleasesCheckBox.Click += (sender, e) => RefreshInstallations();
            ShowPreviewsCheckBox.Click += (sender, e) => RefreshInstallations();

            MainDataModel.Default.Versions.CollectionChanged += (sender, e) => RefreshInstallations();
        }
        public void RefreshInstallations()
        {
            this.Dispatcher.Invoke(() =>
            {
                RefreshInstallationSource();
                if (InstallationsList != null) FilterSortingHandler.Sort_InstallationList(InstallationsList.ItemsSource);
                UpdateNothingFoundVisibility();
            });
        }
        private void NewInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Default.SetOverlayFrame(new EditInstallationScreen());
        }
        private void PageHost_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshInstallationSource();
            SortByComboBox.SelectedItem = Properties.LauncherSettings.Default.InstallationsSortMode switch
            {
                Enums.InstallationSort.LatestPlayed => SortByLatestPlayed,
                Enums.InstallationSort.Name => SortByName,
                Enums.InstallationSort.None => SortByNone,
                _ => SortByLatestPlayed
            };
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

        private void RefreshInstallationSource()
        {
            MainDataModel.Default.Config.SyncSystemMinecraftInstallations();

            if (FindResource("InstallationsSource4") is CollectionViewSource source)
            {
                source.Source = MainDataModel.Default.Config.CurrentInstallations;
                source.View?.Refresh();
            }

            if (MainDataModel.Default.Config.CurrentInstallations is INotifyCollectionChanged installations)
            {
                if (_subscribedInstallations != null)
                    _subscribedInstallations.CollectionChanged -= Installations_CollectionChanged;

                _subscribedInstallations = installations;
                _subscribedInstallations.CollectionChanged += Installations_CollectionChanged;
            }
        }

        private void Installations_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(RefreshInstallations), DispatcherPriority.Background);
        }

        private void UpdateNothingFoundVisibility()
        {
            if (NothingFound == null || InstallationsList == null) return;
            NothingFound.Visibility = InstallationsList.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = Handlers.FilterSortingHandler.Filter_InstallationList(e.Item);
        }
    }
}
