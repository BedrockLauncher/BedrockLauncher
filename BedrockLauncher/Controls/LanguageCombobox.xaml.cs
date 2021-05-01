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
using BedrockLauncher.Core;

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
            switch (this.Text)
            {
                case "Русский - Россия":
                    BL_Core.LanguageManager.LanguageChange("ru-RU");
                    BL_Core.Properties.Settings.Default.Language = "ru-RU";
                    BL_Core.Properties.Settings.Default.Save();
                    break;
                case "English - United States":
                    BL_Core.LanguageManager.LanguageChange("en-US");
                    BL_Core.Properties.Settings.Default.Language = "en-US";
                    BL_Core.Properties.Settings.Default.Save();
                    break;
            }
        }

        private void LanguageCombobox_Initialized(object sender, EventArgs e)
        {
            // Set chosen language in language combobox
            switch (BL_Core.Properties.Settings.Default.Language)
            {
                case "en-US":
                    this.Text = "English - United States";
                    break;
                case "ru-RU":
                    this.Text = "Русский - Россия";
                    break;
                default:
                    this.Text = "English - United States";
                    break;
            }
        }
    }
}
