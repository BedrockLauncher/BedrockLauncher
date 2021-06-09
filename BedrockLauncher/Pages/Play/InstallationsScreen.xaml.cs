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

namespace BedrockLauncher.Pages.Play
{
    public partial class InstallationsScreen : Page
    {
        public InstallationsScreen()
        {
            InitializeComponent();
            ConfigManager.Default.ConfigStateChanged += ConfigManager_ConfigStateChanged;
        }

        private void ConfigManager_ConfigStateChanged(object sender, EventArgs e)
        {
            RefreshInstallationsList(null, null);
        }

        public void RefreshInstallationsList(object sender, RoutedEventArgs e)
        {
            InstallationsList.ItemsSource = ConfigManager.Default.CurrentInstallations;
            var view = CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource) as CollectionView;
            view.Filter = ConfigManager.Default.Filter_InstallationList;
            Task.Run(UpdateUI);
        }

        private void NewInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.LauncherModel.Default.SetOverlayFrame(new EditInstallationScreen());
        }



        private void Page_Initialized(object sender, EventArgs e)
        {

        }

        private async void UpdateUI()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                if (InstallationsList.Items.Count > 0 && NothingFound.Visibility != Visibility.Collapsed) NothingFound.Visibility = Visibility.Collapsed;
                else if (InstallationsList.Items.Count <= 0 && NothingFound.Visibility != Visibility.Visible) NothingFound.Visibility = Visibility.Visible;
            });
        }

        private async void PageHost_Loaded(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                var view = CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource) as CollectionView;
                view.Filter = ConfigManager.Default.Filter_InstallationList;
                Task.Run(UpdateUI);
            });
        }
    }
}
