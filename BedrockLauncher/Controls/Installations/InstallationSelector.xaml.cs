using BedrockLauncher.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace BedrockLauncher.Controls.Installations
{
    /// <summary>
    /// Interaction logic for InstallationSelector.xaml
    /// </summary>
    public partial class InstallationSelector : ComboBox
    {
        private bool HasLoadedOnce = false;

        public InstallationSelector()
        {
            InitializeComponent();
            MainViewModel.Default.ConfigUpdated += InstallationsUpdate;
        }
        public async void RefreshInstallations()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                var view = CollectionViewSource.GetDefaultView(this.ItemsSource) as CollectionView;
                if (view != null)
                {
                    MainViewModel.Default.Sort_InstallationList(ref view);
                    if (view.Filter == null) view.Filter = MainViewModel.Default.Filter_InstallationList;
                    view.Refresh();
                }
                this.SelectedValue = Properties.LauncherSettings.Default.CurrentInstallation;
            });
        }
        private async Task ReloadInstallations()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                if (!HasLoadedOnce)
                {
                    this.ItemsSource = null;
                    this.ItemsSource = MainViewModel.Default.Config.CurrentInstallations;
                    var view = CollectionViewSource.GetDefaultView(this.ItemsSource) as CollectionView;
                    if (view != null) view.Filter = MainViewModel.Default.Filter_InstallationList;
                    HasLoadedOnce = true;
                    this.SelectedIndex = 0;
                }
                this.RefreshInstallations();
            });
        }
        private async void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            await this.ReloadInstallations();
        }
        private async void InstallationsUpdate(object sender, EventArgs e)
        {
            HasLoadedOnce = false;
            await this.ReloadInstallations();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.LauncherSettings.Default.CurrentInstallation = (string)this.SelectedValue;
            Properties.LauncherSettings.Default.Save();
        }
    }
}
