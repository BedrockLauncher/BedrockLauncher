using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using BedrockLauncher.Dungeons.Classes.Launcher;
using BedrockLauncher.UI.ViewModels;
using BedrockLauncher.UI.Pages.Preview;

namespace BedrockLauncher.Dungeons.Controls.Items
{
    /// <summary>
    /// Interaction logic for PatchNotesItem.xaml
    /// </summary>
    public partial class PatchNotesItem : Button
    {
        public PatchNotesItem()
        {
            InitializeComponent();
        }

        private void FeedItemButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            PatchNote item = button.DataContext as PatchNote;
            LoadChangelog(item);
        }

        public static void LoadChangelog(PatchNote item)
        {
            MainViewModel.Handler.SetOverlayFrame(new ChangelogPreviewPage(item.body, item.title));
        }
    }
}
