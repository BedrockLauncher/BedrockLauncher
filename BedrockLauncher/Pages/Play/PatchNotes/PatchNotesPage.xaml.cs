using BedrockLauncher.Classes;
using BedrockLauncher.Classes.Launcher;
using BedrockLauncher.Downloaders;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace BedrockLauncher.Pages.Play.PatchNotes
{
    /// <summary>
    /// Логика взаимодействия для PatchNotesPage.xaml
    /// </summary>
    public partial class PatchNotesPage : Page
    {
        private ChangelogDownloader downloader;

        private bool hasInitalized = false;


        public PatchNotesPage(ChangelogDownloader _downloader)
        {
            this.downloader = _downloader;
            this.DataContext = this.downloader;
            InitializeComponent();
        }

        private async Task RefreshPatchNotes(bool force)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                if (force) Task.Run(downloader.UpdateList);
                var view = CollectionViewSource.GetDefaultView(PatchNotesList.ItemsSource) as CollectionView;
                if (view != null) view.Filter = Filter_PatchNotes;
            });
        } 

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!hasInitalized)
            {
                Task.Run(() => RefreshPatchNotes(true));
                hasInitalized = true;
            }
        }

        public bool Filter_PatchNotes(object obj)
        {
            if (obj is not PatchNotes_Game_Item v)
                return false;

            if (v.isBeta)
                return downloader.ShowBetas;

            return downloader.ShowReleases;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RefreshPatchNotes(true));
        }

        private void RefreshList(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RefreshPatchNotes(false));
        }

        private async void PatchNotesList_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (PatchNotesList.SelectedItem != null)
                {
                    var item = PatchNotesList.SelectedItem as PatchNotes_Game_Item;
                    await FeedItem_PatchNotes.LoadChangelog(item);
                }
            }
        }

        private void MorePatchNotes_Click(object sender, RoutedEventArgs e)
        {
            JemExtensions.WebExtensions.LaunchWebLink(Constants.PATCHNOTES_RELEASE_CHANGELOG_URL);
            e.Handled = true;
        }

        private void MoreBetaPatchNotes_Click(object sender, RoutedEventArgs e)
        {
            JemExtensions.WebExtensions.LaunchWebLink(Constants.PATCHNOTES_PREVIEW_CHANGELOG_URL);
            e.Handled = true;
        }

        private void Page_Initialized(object sender, EventArgs e)
        {

        }

        private void PatchNotesList_SourceUpdated(object sender, DataTransferEventArgs e)
        {

        }
    }
}