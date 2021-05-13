using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                InitializeComponent();
            }
        }

        private void LanguageCombobox_DropDownClosed(object sender, EventArgs e)
        {
            var item = this.SelectedItem as BL_Core.LanguageDefinition;
            if (item == null) return;
            switch (item.Locale)
            {
                case "ru-RU":
                    BL_Core.LanguageManager.SetLanguage("ru-RU");
                    break;
                case "en-US":
                    BL_Core.LanguageManager.SetLanguage("en-US");
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
            var items = BL_Core.LanguageManager.GetResourceDictonaries();
            this.ItemsSource = items;

            var item = this.SelectedItem as ComboBoxItem;
            if (item == null) return;
            // Set chosen language in language combobox
            switch (BL_Core.Properties.Settings.Default.Language)
            {
                case "en-US":
                    this.SelectedItem = items.Where(x => x.Locale.ToString() == "en-US").FirstOrDefault();
                    break;
                case "ru-RU":
                    this.SelectedItem = items.Where(x => x.Locale.ToString() == "ru-RU").FirstOrDefault();
                    break;
                default:
                    this.SelectedItem = items.Where(x => x.Locale.ToString() == "en-US").FirstOrDefault();
                    break;
            }
        }
    }
}
