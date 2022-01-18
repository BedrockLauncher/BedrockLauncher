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
using System.IO;
using BedrockLauncher.UI.Pages.Common;
using System.Diagnostics;

namespace BedrockLauncher.Dungeons.Pages
{
    /// <summary>
    /// Логика взаимодействия для ModsPage.xaml
    /// </summary>
    public partial class ModsPage : Page
    {
        public ModsPage()
        {
            InitializeComponent();
        }

        public void RefreshModsList()
        {
            ModList.Items.Clear();

            try
            {
                var ModsFolder = new DirectoryInfo(Methods.GameManager.GetOurDungeonsModFolder());
                foreach (var file in ModsFolder.GetFiles())
                {
                    if (file.Name.EndsWith(".pak.disable") || file.Name.EndsWith(".pak")) ModList.Items.Add(new Classes.DungeonsMod(file));
                }
            }
            catch (Exception ex)
            {
                BedrockLauncher.UI.Pages.Common.ErrorScreenShow.exceptionmsg(ex);
            }

        }

        private void Page_Initialized(object sender, EventArgs e)
        {

        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var mod = button.DataContext as Classes.DungeonsMod;
            mod.OpenInExplorer();
        }

        private void More_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var mod = button.DataContext as Classes.DungeonsMod;
            this.ModList.SelectedItem = mod;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.DataContext = mod;
            button.ContextMenu.IsOpen = true;
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {

        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = sender as MenuItem;
            var mod = button.DataContext as Classes.DungeonsMod;

            var title = this.FindResource("Dialog_DeleteItem_Title") as string;
            var content = this.FindResource("Dialog_DeleteItem_Text") as string;
            var item = "mod";

            var result = await DialogPrompt.ShowDialog_YesNo(title, content, item, mod.Name);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                mod.Delete();
                RefreshModsList();
            }

        }

        private void OpenModsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", Methods.GameManager.GetOurDungeonsModFolder());
            }
            catch (Exception ex)
            {
                BedrockLauncher.UI.Pages.Common.ErrorScreenShow.exceptionmsg(ex);
            }
        }

        private void PageHost_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshModsList();
        }
    }
}
