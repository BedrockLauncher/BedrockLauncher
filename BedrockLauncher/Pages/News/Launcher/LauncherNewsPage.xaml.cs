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
using BedrockLauncher.Classes.Launcher;
using BedrockLauncher.Downloaders;
using BedrockLauncher.Handlers;
using MdXaml;

namespace BedrockLauncher.Pages.News.Launcher
{
    /// <summary>
    /// Interaction logic for LauncherNewsPage.xaml
    /// </summary>
    public partial class LauncherNewsPage : Page
    {

        private bool HasPreloaded = false;

        public LauncherNewsPage()
        {
            this.DataContext = ViewModels.NewsViewModel.Default;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, EventArgs e)
        {
            if (!HasPreloaded)
            {
                _ = RefreshNews(true);
                HasPreloaded = true;
            }
        }

        public bool Filter_PatchNotes(object obj)
        {
            PatchNote_Launcher v = obj as PatchNote_Launcher;

            if (v != null)
            {
                if (!ViewModels.NewsViewModel.Default.Launcher_ShowBetas && v.isBeta) return false;
                else if (!ViewModels.NewsViewModel.Default.Launcher_ShowReleases && !v.isBeta) return false;
                else return true;
            }
            else return false;
        }


        private void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () => {
                var result = await ViewModels.MainDataModel.Updater.CheckForUpdatesAsync();
                if (result) ViewModels.MainViewModel.Default.UpdateButton.ShowUpdateButton();
            });
        }

        private void ForceUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.MainDataModel.Updater.UpdateButton_Click(sender, e);
        }

        private void UpdateFilters(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RefreshNews(false));
        }

        public async Task RefreshNews(bool force = true)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                if (force) Task.Run(() => Downloaders.NewsDownloader.UpdateLauncherFeed(ViewModels.NewsViewModel.Default));
                var view = CollectionViewSource.GetDefaultView(UpdatesList.ItemsSource) as CollectionView;
                if (view != null) view.Filter = Filter_PatchNotes;
            });
        }

        private void PatchNotesList_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (UpdatesList.SelectedItem != null)
                {
                    var item = UpdatesList.SelectedItem as PatchNote_Launcher;
                    FeedItem_Launcher.LoadChangelog(item);
                }
            }
        }

        private void UpdatesList_SourceUpdated(object sender, DataTransferEventArgs e)
        {

        }
    }
}
