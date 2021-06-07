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

namespace BedrockLauncher.Pages.Settings
{
    /// <summary>
    /// Логика взаимодействия для InstallationsScreen.xaml
    /// </summary>
    public partial class VersionsSettingsPage : Page
    {
        public VersionsSettingsPage()
        {
            InitializeComponent();
            ConfigManager.ConfigStateChanged += ConfigManager_ConfigStateChanged;
        }

        private async void ConfigManager_ConfigStateChanged(object sender, EventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                VersionsList.ItemsSource = ConfigManager.Versions;
                var view = CollectionViewSource.GetDefaultView(VersionsList.ItemsSource) as CollectionView;
                view.Filter = ConfigManager.Filter_VersionList;
            });
        }

        public void RefreshVersionsList()
        {
            ConfigManager.OnConfigStateChanged(this, Events.ConfigStateArgs.Empty);
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
                var view = CollectionViewSource.GetDefaultView(VersionsList.ItemsSource) as CollectionView;
                view.Filter = ConfigManager.Filter_VersionList;
            });
        }
    }
}
