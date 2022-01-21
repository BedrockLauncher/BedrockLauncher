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
using BedrockLauncher.Extensions;

namespace BedrockLauncher.Controls.Config
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
