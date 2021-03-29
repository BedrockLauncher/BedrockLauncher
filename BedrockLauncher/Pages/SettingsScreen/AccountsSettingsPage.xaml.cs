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

namespace BedrockLauncher.Pages.SettingsScreen
{
    /// <summary>
    /// Логика взаимодействия для AccountsSettingsPage.xaml
    /// </summary>
    public partial class AccountsSettingsPage : Page
    {
        public AccountsSettingsPage()
        {
            InitializeComponent();
        }

        private void ComboBoxItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // To not move combobox list on mouse hover
            e.Handled = true;
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            AccountPicker.RefreshProfileContextMenuItems();
        }
    }
}
