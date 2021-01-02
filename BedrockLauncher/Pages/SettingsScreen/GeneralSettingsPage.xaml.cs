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

namespace BedrockLauncher
{
    /// <summary>
    /// Логика взаимодействия для GeneralSettingsPage.xaml
    /// </summary>
    public partial class GeneralSettingsPage : Page
    {
        public GeneralSettingsPage()
        {
            InitializeComponent();

            // Set chosen language in language combobox
            switch (Properties.Settings.Default.Language)
            {
                case "en-US":
                    LanguageCombobox.Text = "English - United States";
                    break;
                case "ru-RU":
                    LanguageCombobox.Text = "Русский - Россия";
                    break;
                default:
                    LanguageCombobox.Text = "English - United States";
                    break;
            }
        }

        private void ComboBoxItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private void LanguageCombobox_DropDownClosed(object sender, EventArgs e)
        {
            switch (LanguageCombobox.Text)
            {
                case "Русский - Россия":
                    ((MainWindow)Application.Current.MainWindow).LanguageChange("ru-RU");
                    Properties.Settings.Default.Language = "ru-RU";
                    Properties.Settings.Default.Save();
                    break;
                case "English - United States":
                    ((MainWindow)Application.Current.MainWindow).LanguageChange("en-US");
                    Properties.Settings.Default.Language = "en-US";
                    Properties.Settings.Default.Save();
                    break;
            }
        }
    }
}
