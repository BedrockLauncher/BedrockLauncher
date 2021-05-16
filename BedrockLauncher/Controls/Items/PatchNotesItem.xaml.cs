using BedrockLauncher.Classes;
using BedrockLauncher.Methods;
using BedrockLauncher.Pages;
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
using System.Windows.Shapes;
using BedrockLauncher.Pages.Preview;

namespace BedrockLauncher.Controls.Items
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
            MCPatchNotesItem item = button.DataContext as MCPatchNotesItem;
            LoadChangelog(item);
        }

        public static void LoadChangelog(MCPatchNotesItem item)
        {
            string header_title = string.Format("{0} {1}", (item.isBeta ? "Beta" : "Release"), item.Version);
            ConfigManager.ViewModel.SetOverlayFrame(new ChangelogPreviewPage(item.Content, header_title, item.Url));
        }
    }
}
