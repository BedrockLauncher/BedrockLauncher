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
            ShowBetasCheckBox.Click += (sender, e) => RefreshInstallationsList();
            ShowReleasesCheckBox.Click += (sender, e) => RefreshInstallationsList();
        }

        public async void RefreshInstallationsList()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                var view = CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource) as CollectionView;
                LauncherModel.Default.ConfigManager.Sort_InstallationList(ref view);
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
            await this.Dispatcher.InvokeAsync(() =>
            {
                if (!HasLoadedOnce)
                {
                    InstallationsList.ItemsSource = LauncherModel.Default.ConfigManager.CurrentInstallations;
                    var view = CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource) as CollectionView;
                    view.Filter = LauncherModel.Default.ConfigManager.Filter_InstallationList;
                    HasLoadedOnce = true;
                }
                RefreshInstallationsList();
            });
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (HasLoadedOnce)
            {
                LauncherModel.Default.ConfigManager.Installations_SearchFilter = SearchBox.Text;
                RefreshInstallationsList();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HasLoadedOnce)
            {

                if (SortByComboBox.SelectedItem == SortByLatestPlayed)
                    LauncherModel.Default.ConfigManager.Installations_SortFilter = ConfigManager.SortBy_Installation.LatestPlayed;

                if (SortByComboBox.SelectedItem == SortByName)
                    LauncherModel.Default.ConfigManager.Installations_SortFilter = ConfigManager.SortBy_Installation.Name;

                RefreshInstallationsList();
            }
        }
    }
}
