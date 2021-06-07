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
using CodeHollow.FeedReader;
using BedrockLauncher.Classes;
using System.Diagnostics;
using BedrockLauncher.Controls.Items;
using BedrockLauncher.Downloaders;

namespace BedrockLauncher.Pages.Play
{
    /// <summary>
    /// Логика взаимодействия для PatchNotesPage.xaml
    /// </summary>
    public partial class PatchNotesPage : Page
    {
        private ChangelogDownloader downloader;


        public PatchNotesPage(ChangelogDownloader _downloader)
        {
            InitializeComponent();
            this.downloader = _downloader;
            this.downloader.RefreshableStateChanged += Downloader_RefreshableStateChanged;
            this.DataContext = this.downloader;
        }

        private void Downloader_RefreshableStateChanged(object sender, EventArgs e)
        {
            UpdateButton.IsEnabled = downloader.IsRefreshable;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                var view = CollectionViewSource.GetDefaultView(PatchNotesList.ItemsSource) as CollectionView;
                view.Filter = ConfigManager.Filter_PatchNotes;
            });
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                downloader.UpdateList();
            });
        }

        private void RefreshList(object sender, RoutedEventArgs e)
        {


        }

        private void PatchNotesList_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (PatchNotesList.SelectedItem != null)
                {
                    var item = PatchNotesList.SelectedItem as MCPatchNotesItem;
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

        private async void Page_Initialized(object sender, EventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                this.downloader.UpdateList();
                PatchNotesList.ItemsSource = downloader.PatchNotes;
            });
        }
    }
}