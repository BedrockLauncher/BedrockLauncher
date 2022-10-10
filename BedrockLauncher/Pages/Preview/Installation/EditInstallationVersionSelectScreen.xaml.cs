using BedrockLauncher.Classes;
using JemExtensions;
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
using System.Windows.Shapes;

namespace BedrockLauncher.Pages.Preview.Installation
{
    /// <summary>
    /// Interaction logic for InstallationVersionSelectScreen.xaml
    /// </summary>
    public partial class EditInstallationVersionSelectScreen : Page
    {
        private ViewModels.EditInstallationVersionSelectViewModel MainContext => (ViewModels.EditInstallationVersionSelectViewModel)this.DataContext;

        private bool IsDone = false;
        private string SelectedVersionUUID = string.Empty;
        public EditInstallationVersionSelectScreen()
        {
            this.DataContext = new ViewModels.EditInstallationVersionSelectViewModel();
            InitializeComponent();       
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = Filter(e.Item);
        }

        private bool Filter(object obj)
        {
            MCVersion v = obj as MCVersion;
            if (v == null) return false;


            if (!MainContext.ShowImported && v.IsCustom) return false;

            if (!v.IsCustom)
            {
                if (!MainContext.ShowPreview && v.IsPreview) return false;
                else if (!MainContext.ShowBeta && v.IsBeta) return false;
                else if (!MainContext.ShowRelease && v.IsRelease) return false;
            }

            if (!MainContext.ShowX64 && v.Architecture == "x64") return false;
            else if (!MainContext.ShowX86 && v.Architecture == "x86") return false;
            else if (!MainContext.ShowARM && v.Architecture == "arm") return false;



            else if (!v.DisplayName.Contains(MainContext.FilterString)) return false;
            else return true;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.IsInitialized && this.IsLoaded) Refresh();
        }

        private void Refresh()
        {
            Dispatcher.Invoke(() =>
            {
                Handlers.FilterSortingHandler.Refresh(VersionsList.ItemsSource);
            });
        }

        private void Finish(bool update = false)
        {
            if (update)
            {
                var o = (VersionsList.SelectedItem as MCVersion);

                if (o == null) SelectedVersionUUID = string.Empty;
                else SelectedVersionUUID = o.UUID;
            }
            IsDone = true;
            ViewModels.MainViewModel.Default.SetDialogFrame(null);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized && this.IsLoaded) Refresh();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            Finish(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Finish();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Finish();
        }

        public async Task<string> GetVersionUUID()
        {
            return await Task.Run(() =>
            {
                while (!IsDone) { }
                return SelectedVersionUUID;
            });
        }
    }
}
