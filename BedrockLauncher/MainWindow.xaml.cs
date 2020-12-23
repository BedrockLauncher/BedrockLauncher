using System;
using System.Globalization;
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
using System.Windows.Shapes;
using MCLauncher;

namespace BedrockLauncher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainPage mainPage = new MainPage();
        public NoContentPage noContentPage = new NoContentPage();
        public PlayScreenPage playScreenPage = new PlayScreenPage();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void NewsButton_Click(object sender, RoutedEventArgs e)
        {
            switch (NewsButton.IsChecked)
            {
                case true:
                    MainWindowFrame.Navigate(noContentPage); // Переключение фрейма MainWindow на нужное окно
                    PlayScreenBorder.Visibility = Visibility.Hidden; // Скрывает нижнюю панель в MainWindow

                    // Выключение других кнопок
                    BedrockEditionButton.IsChecked = false;
                    SettingsButton.IsChecked = false;
                    break;
                case false:
                    NewsButton.IsChecked = true; // Не снимает свойства IsChecked при повторном нажатии на кнопку
                    break;
            }
        }

        private void BedrockEditionButton_Click(object sender, RoutedEventArgs e)
        {
            switch (BedrockEditionButton.IsChecked)
            {
                case true:
                    MainWindowFrame.Navigate(mainPage); // Переключение фрейма MainWindow на окно MainPage.xaml
                    mainPage.MainPageFrame.Navigate(playScreenPage); // Переключение фрейма MainPage на окно PlayScreenPage.xaml
                    PlayScreenBorder.Visibility = Visibility.Visible; // Показывает нижнюю панель в MainWindow
                    // Оставляет нажатой только кнопку PlayButton в окне MainPage.xaml
                    mainPage.PlayButton.IsChecked = true;
                    mainPage.InstallationsButton.IsChecked = false;
                    mainPage.SkinsButton.IsChecked = false;
                    mainPage.PatchNotesButton.IsChecked = false;

                    // Выключение других кнопок
                    NewsButton.IsChecked = false;
                    SettingsButton.IsChecked = false;
                    break;
                case false:
                    BedrockEditionButton.IsChecked = true; // Не снимает свойства IsChecked при повторном нажатии на кнопку

                    // Оставляет нажатой только кнопку PlayButton в окне MainPage.xaml
                    mainPage.PlayButton.IsChecked = true;
                    mainPage.InstallationsButton.IsChecked = false;
                    mainPage.SkinsButton.IsChecked = false;
                    mainPage.PatchNotesButton.IsChecked = false;
                    break;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            switch (SettingsButton.IsChecked)
            {
                case true:
                    MainWindowFrame.Navigate(noContentPage); // Переключение фрейма MainWindow на нужное окно
                    PlayScreenBorder.Visibility = Visibility.Hidden; // Скрывает нижнюю панель в MainWindow

                    // Выключение других кнопок
                    BedrockEditionButton.IsChecked = false;
                    NewsButton.IsChecked = false;
                    break;
                case false:
                    SettingsButton.IsChecked = true; // Не снимает свойства IsChecked при повторном нажатии на кнопку
                    break;
            }
            
        }
    }
}
