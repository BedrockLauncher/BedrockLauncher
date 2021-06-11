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
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Settings
{
    /// <summary>
    /// Логика взаимодействия для InstallationsScreen.xaml
    /// </summary>
    public partial class VersionsSettingsPage : Page
    {

        private bool HasLoadedOnce = false;
        public VersionsSettingsPage()
        {
            InitializeComponent();
        }

        public async void RefreshVersionsList()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                var view = CollectionViewSource.GetDefaultView(VersionsList.ItemsSource) as CollectionView;
                view.Refresh();
            });
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            
        }

        private void RefreshVersionsList(object sender, RoutedEventArgs e)
        {
            RefreshVersionsList();
        }

        private async void PageHost_Loaded(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                if (!HasLoadedOnce)
                {
                    VersionsList.ItemsSource = LauncherModel.Default.ConfigManager.Versions;
                    var view = CollectionViewSource.GetDefaultView(VersionsList.ItemsSource) as CollectionView;
                    view.Filter = LauncherModel.Default.ConfigManager.Filter_VersionList;
                    HasLoadedOnce = true;
                }
            });
        }
    }
}
