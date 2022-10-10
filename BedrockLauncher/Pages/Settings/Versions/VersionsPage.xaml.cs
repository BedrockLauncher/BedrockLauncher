using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BedrockLauncher.Handlers;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Settings.Versions
{
    public partial class VersionsPage : Page
    {
        private bool hasInitalized = false;
        public VersionsPage()
        {
            this.DataContext = MainDataModel.Default;
            InitializeComponent();
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            
        }

        private void RefreshVersionsList(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (VersionsList != null) Handlers.FilterSortingHandler.Refresh(VersionsList.ItemsSource);
            });
        }

        private void PageHost_Loaded(object sender, RoutedEventArgs e)
        {
            if (!hasInitalized)
            {
                foreach (var ver in MainDataModel.Default.Versions) ver.UpdateFolderSize();
                hasInitalized = true;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(Program.OnApplicationRefresh);
            foreach (var ver in MainDataModel.Default.Versions) ver.UpdateFolderSize();
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = FilterSortingHandler.Filter_VersionList(e.Item);
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "APPX Files (*.appx)|*.appx"
            };

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string fileToImport = ofd.FileName;
                await MainDataModel.Default.PackageManager.AddPackage(fileToImport);
                await Task.Run(Program.OnApplicationRefresh);
                foreach (var ver in MainDataModel.Default.Versions) ver.UpdateFolderSize();
            }
        }
    }
}
