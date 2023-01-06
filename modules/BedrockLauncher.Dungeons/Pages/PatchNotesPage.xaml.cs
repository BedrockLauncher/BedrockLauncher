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
using System.Diagnostics;
using BedrockLauncher.Dungeons.Downloaders;
using BedrockLauncher.Dungeons.Classes.Launcher;
using BedrockLauncher.Dungeons.Controls.Items;

namespace BedrockLauncher.Dungeons.Pages
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
            //PatchNote v = obj as PatchNote;
            //
            //if (v != null)
            //{
            //    if (!downloader.ShowBetas && v.isBeta) return false;
            //    else if (!downloader.ShowReleases && !v.isBeta) return false;
            //    else return true;
            //}
            //else return false;
            return true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RefreshPatchNotes(true));
        }

        private void RefreshList(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RefreshPatchNotes(false));
        }

        private void PatchNotesList_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (PatchNotesList.SelectedItem != null)
                {
                    var item = PatchNotesList.SelectedItem as PatchNote;
                    PatchNotesItem.LoadChangelog(item);
                }
            }
        }

        private void MorePatchNotes_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://aka.ms/MCChangelogs";
            Process.Start(new ProcessStartInfo(url));
            e.Handled = true;
        }

        private void MoreBetaPatchNotes_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://feedback.minecraft.net/hc/en-us/sections/360001185332-Beta-Information-and-Changelogs";
            Process.Start(new ProcessStartInfo(url));
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