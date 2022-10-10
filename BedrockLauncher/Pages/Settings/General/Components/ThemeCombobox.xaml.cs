using BedrockLauncher.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BedrockLauncher.Pages.Settings.General.Components
{
    /// <summary>
    /// Interaction logic for ThemeCombobox.xaml
    /// </summary>
    public partial class ThemeCombobox : ComboBox
    {
        public ThemeCombobox()
        {
            InitializeComponent();
        }

        private void ThemeCombobox_DropDownClosed(object sender, EventArgs e)
        {
            var item = this.SelectedItem as ComboBoxItem;
            if (item == null) return;
            Properties.LauncherSettings.Default.CurrentTheme = item.Tag.ToString();
            Properties.LauncherSettings.Default.Save();
        }

        private void GenerateItems()
        {
            AddItem("LatestUpdate", Brushes.Yellow);
            foreach (var entry in Constants.Themes) AddItem(entry.Key);
            GetCustomItems();

            void GetCustomItems()
            {
                DirectoryInfo directoryInfo = Directory.CreateDirectory(MainDataModel.Default.FilePaths.ThemesFolder);
                foreach (var file in directoryInfo.GetFiles())
                {
                    if (file.Extension == ".png" || file.Extension == ".jpg" || file.Extension == ".jpeg")
                    {
                        AddItem(file.Name, Brushes.LightYellow, true);
                    }
                }
            }
            
            
            void AddItem(string tag, Brush brush = null, bool isCustom = false)
            {
                var item = new ComboBoxItem();
                item.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;

                var customString = Application.Current.TryFindResource("ThemeEntries_Custom") ?? string.Empty;

                if (brush != null) item.Foreground = brush;
                if (isCustom)
                {
                    item.Tag = string.Format("{0}{1}", Constants.ThemesCustomPrefix, tag);
                    item.Content = string.Format("{0} - {1}", customString.ToString(), tag);
                }
                else
                {
                    item.Tag = tag;
                    item.SetResourceReference(ComboBoxItem.ContentProperty, "ThemeEntries_" + tag);
                }
                this.Items.Add(item);
            }
        }

        private void ThemeCombobox_Initialized(object sender, EventArgs e)
        {
            GenerateItems();

            var items = this.Items.Cast<ComboBoxItem>().Select(x => x).ToList();

            var item = this.SelectedItem as ComboBoxItem;
            if (item == null) return;
            string currentTheme = Properties.LauncherSettings.Default.CurrentTheme;


            if (items.Exists(x => x.Tag.ToString() == currentTheme))
            {
                this.SelectedItem = items.Where(x => x.Tag.ToString() == currentTheme).FirstOrDefault();
            }
            else
            {
                this.SelectedItem = items.Where(x => x.Tag.ToString() == "LatestUpdate").FirstOrDefault();
            }
        }
    }
}
