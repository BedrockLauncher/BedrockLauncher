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
using BedrockLauncher.Pages.NoContentScreen;

namespace BedrockLauncher.Pages.SettingsScreen
{
    /// <summary>
    /// Логика взаимодействия для SettingsScreen.xaml
    /// </summary>
    public partial class SettingsScreen : Page
    {
        public GeneralSettingsPage generalSettingsPage = new GeneralSettingsPage();
        public NoContentPage noContentPage = new NoContentPage();
        public SettingsScreen()
        {
            InitializeComponent();
        }

        private void GeneralButton_Click(object sender, RoutedEventArgs e)
        {
            switch (GeneralButton.IsChecked)
            {
                case true:
                    SettingsScreenFrame.Navigate(generalSettingsPage);

                    // Выключение других кнопок
                    AboutButton.IsChecked = false;
                    break;
                case false:
                    GeneralButton.IsChecked = true; // Не снимает свойства IsChecked при повторном нажатии на кнопку
                    break;
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            switch (AboutButton.IsChecked)
            {
                case true:
                    SettingsScreenFrame.Navigate(noContentPage);

                    // Выключение других кнопок
                    GeneralButton.IsChecked = false;
                    break;
                case false:
                    AboutButton.IsChecked = true; // Не снимает свойства IsChecked при повторном нажатии на кнопку
                    break;
            }
        }
    }
}
