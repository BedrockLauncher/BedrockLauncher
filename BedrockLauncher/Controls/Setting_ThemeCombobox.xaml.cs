using System;
using System.Linq;
using System.Windows.Controls;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for ThemeCombobox.xaml
    /// </summary>
    public partial class Setting_ThemeCombobox : ComboBox
    {
        public Setting_ThemeCombobox()
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

        private void ThemeCombobox_Initialized(object sender, EventArgs e)
        {
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
