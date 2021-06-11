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
using BedrockLauncher.Core.Classes;
using BedrockLauncher.ViewModels;

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
            this.downloader.PatchNotes.CollectionChanged += PatchNotes_CollectionChanged;
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
                view.Filter = Filter_PatchNotes;
                UpdateUI();
            });
        }

        public bool Filter_PatchNotes(object obj)
        {
            MCPatchNotesItem v = obj as MCPatchNotesItem;

            if (v != null)
            {
                if (!BetasCheckBox.IsChecked.Value && v.isBeta) return false;
                else if (!ReleasesCheckBox.IsChecked.Value && !v.isBeta) return false;
                else return true;
            }
            else return false;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                downloader.UpdateList();
            });
        }

        private async void RefreshList(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => Page_Loaded(sender, e));
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

        private async void PatchNotes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            await Task.Run(UpdateUI);
        }

        private async void UpdateUI()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                if (PatchNotesList.Items.Count > 0 && NothingFound.Visibility != Visibility.Collapsed) NothingFound.Visibility = Visibility.Collapsed;
                else if (PatchNotesList.Items.Count <= 0 && NothingFound.Visibility != Visibility.Visible) NothingFound.Visibility = Visibility.Visible;
            });
        }
    }
}