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

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for LanguageCombobox.xaml
    /// </summary>
    public partial class LanguageCombobox : ComboBox
    {
        public LanguageCombobox()
        {
            InitializeComponent();
        }

        private void LanguageCombobox_DropDownClosed(object sender, EventArgs e)
        {
            var item = this.SelectedItem as ComboBoxItem;
            if (item == null) return;
            switch (item.Tag)
            {
                case "ru-RU":
                    BL_Core.LanguageManager.LanguageChange("ru-RU");
                    BL_Core.Properties.Settings.Default.Language = "ru-RU";
                    BL_Core.Properties.Settings.Default.Save();
                    break;
                case "en-US":
                    BL_Core.LanguageManager.LanguageChange("en-US");
                    BL_Core.Properties.Settings.Default.Language = "en-US";
                    BL_Core.Properties.Settings.Default.Save();
                    break;
            }
        }

        private void ComboBoxItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // To prevent scrolling when mouseover
            e.Handled = true;
        }

        private void LanguageCombobox_Initialized(object sender, EventArgs e)
        {
            var items = this.Items.Cast<ComboBoxItem>().Select(x => x).ToList();

            var item = this.SelectedItem as ComboBoxItem;
            if (item == null) return;
            // Set chosen language in language combobox
            switch (BL_Core.Properties.Settings.Default.Language)
            {
                case "en-US":
                    this.SelectedItem = items.Where(x => x.Tag.ToString() == "en-US").FirstOrDefault();
                    break;
                case "ru-RU":
                    this.SelectedItem = items.Where(x => x.Tag.ToString() == "ru-RU").FirstOrDefault();
                    break;
                default:
                    this.SelectedItem = items.Where(x => x.Tag.ToString() == "en-US").FirstOrDefault();
                    break;
            }
        }
    }
}
