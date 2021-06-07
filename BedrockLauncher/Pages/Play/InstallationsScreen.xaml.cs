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
            ConfigManager.ConfigStateChanged += ConfigManager_ConfigStateChanged;
        }

        private void ConfigManager_ConfigStateChanged(object sender, EventArgs e)
        {
            RefreshInstallationsList(null, null);
        }

        public void RefreshInstallationsList(object sender, RoutedEventArgs e)
        {
            InstallationsList.ItemsSource = ConfigManager.CurrentInstallations;
            var view = CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource) as CollectionView;
            view.Filter = ConfigManager.Filter_InstallationList;
        }

        private void NewInstallationButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.LauncherModel.Default.SetOverlayFrame(new EditInstallationScreen());
        }



        private void Page_Initialized(object sender, EventArgs e)
        {

        }

        private async void PageHost_Loaded(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                var view = CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource) as CollectionView;
                view.Filter = ConfigManager.Filter_InstallationList;
            });
        }
    }
}
