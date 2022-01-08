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
            ShowBetasCheckBox.Click += (sender, e) => RefreshInstallations();
            ShowReleasesCheckBox.Click += (sender, e) => RefreshInstallations();
            LauncherModel.Default.ConfigUpdated += InstallationsUpdate;
        }
        public async void RefreshInstallations()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                var view = CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource) as CollectionView;
                LauncherModel.Default.Sort_InstallationList(ref view);
                view.Refresh();
                if (InstallationsList.Items.Count > 0 && NothingFound.Visibility != Visibility.Collapsed) NothingFound.Visibility = Visibility.Collapsed;
                else if (InstallationsList.Items.Count <= 0 && NothingFound.Visibility != Visibility.Visible) NothingFound.Visibility = Visibility.Visible;
            });
        }
        private void NewInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.LauncherModel.Default.SetOverlayFrame(new EditInstallationScreen());
        }
        private async void PageHost_Loaded(object sender, RoutedEventArgs e)
        {
            await this.ReloadInstallations();
        }
        private async Task ReloadInstallations()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                if (!HasLoadedOnce)
                {
                    InstallationsList.ItemsSource = null;
                    InstallationsList.ItemsSource = LauncherModel.Default.Config.CurrentInstallations;
                    var view = CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource) as CollectionView;
                    view.Filter = LauncherModel.Default.Filter_InstallationList;
                    HasLoadedOnce = true;
                }
                this.RefreshInstallations();
            });
        }
        private async void InstallationsUpdate(object sender, EventArgs e)
        {
            HasLoadedOnce = false;
            await this.ReloadInstallations();
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (HasLoadedOnce)
            {
                LauncherModel.Default.Installations_SearchFilter = SearchBox.Text;
                RefreshInstallations();
            }
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HasLoadedOnce)
            {

                if (SortByComboBox.SelectedItem == SortByLatestPlayed)
                    LauncherModel.Default.Installations_SortFilter = Enums.SortBy_Installation.LatestPlayed;

                if (SortByComboBox.SelectedItem == SortByName)
                    LauncherModel.Default.Installations_SortFilter = Enums.SortBy_Installation.Name;

                RefreshInstallations();
            }
        }
    }
}
