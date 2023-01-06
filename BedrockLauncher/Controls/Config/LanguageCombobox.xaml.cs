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
using BedrockLauncher.Extensions;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Controls.Config
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
            var item = this.SelectedItem as BedrockLauncher.Localization.Language.LanguageDefinition;
            if (item == null) return;
            BedrockLauncher.Localization.Language.LanguageManager.SetLanguage(item.Locale);
        }


        private void ReloadLang()
        {
            var items = BedrockLauncher.Localization.Language.LanguageManager.GetResourceDictonaries();
            this.ItemsSource = items;
            string language = BedrockLauncher.Localization.Properties.Settings.Default.Language;

            // Set chosen language in language combobox
            if (items.Exists(x => x.Locale.ToString() == language))
            {
                this.SelectedItem = items.Where(x => x.Locale.ToString() == language).FirstOrDefault();
            }
            else
            {
                this.SelectedItem = items.Where(x => x.Locale.ToString() == "en-US").FirstOrDefault();
            }
        }

        private void LanguageCombobox_Initialized(object sender, EventArgs e)
        {

        }

        private void LanguageCombobox_Loaded(object sender, RoutedEventArgs e)
        {
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime) ReloadLang();
        }
    }
}
