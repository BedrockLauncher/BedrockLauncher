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
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public PlayScreenPage playScreenPage = new PlayScreenPage();
        public NoContentPage noContentPage = new NoContentPage();

        public MainPage()
        {
            InitializeComponent();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            switch (PlayButton.IsChecked)
            {
                case true:
                    MainPageFrame.Navigate(playScreenPage); // Переключение фрейма в MainPage на страницу PlayScreenPage.xaml
                    ((MainWindow)Application.Current.MainWindow).PlayScreenBorder.Visibility = Visibility.Visible; // Показывает нижнюю панель в MainWindow

                    // Выключение других кнопок
                    InstallationsButton.IsChecked = false;
                    SkinsButton.IsChecked = false;
                    PatchNotesButton.IsChecked = false;
                    break;
                case false:
                    PlayButton.IsChecked = true; // Не снимает свойства IsChecked при повторном нажатии на кнопку
                    break;
            }
        }

        private void InstallationsButton_Click(object sender, RoutedEventArgs e)
        {
            switch (InstallationsButton.IsChecked)
            {
                case true:
                    MainPageFrame.Navigate(noContentPage); // Переключение фрейма в MainPage на нужное окно
                    ((MainWindow)Application.Current.MainWindow).PlayScreenBorder.Visibility = Visibility.Hidden; // Скрывает нижнюю панель в MainWindow

                    // Выключение других кнопок
                    PlayButton.IsChecked = false;
                    SkinsButton.IsChecked = false;
                    PatchNotesButton.IsChecked = false;
                    break;
                case false:
                    InstallationsButton.IsChecked = true; // Не снимает свойства IsChecked при повторном нажатии на кнопку
                    break;
            }
        }

        private void SkinsButton_Click(object sender, RoutedEventArgs e)
        {
            switch (SkinsButton.IsChecked)
            {
                case true:
                    MainPageFrame.Navigate(noContentPage); // Переключение фрейма в MainPage на нужное окно
                    ((MainWindow)Application.Current.MainWindow).PlayScreenBorder.Visibility = Visibility.Hidden; // Скрывает нижнюю панель в MainWindow

                    // Выключение других кнопок
                    InstallationsButton.IsChecked = false;
                    PlayButton.IsChecked = false;
                    PatchNotesButton.IsChecked = false;
                    break;
                case false:
                    SkinsButton.IsChecked = true; // Не снимает свойства IsChecked при повторном нажатии на кнопку
                    break;
            }
        }

        private void PatchNotesButton_Click(object sender, RoutedEventArgs e)
        {
            switch (PatchNotesButton.IsChecked)
            {
                case true:
                    MainPageFrame.Navigate(noContentPage); // Переключение фрейма в MainPage на нужное окно
                    ((MainWindow)Application.Current.MainWindow).PlayScreenBorder.Visibility = Visibility.Hidden; // Скрывает нижнюю панель в MainWindow

                    // Выключение других кнопок
                    InstallationsButton.IsChecked = false;
                    SkinsButton.IsChecked = false;
                    PlayButton.IsChecked = false;
                    break;
                case false:
                    PatchNotesButton.IsChecked = true; // Не снимает свойства IsChecked при повторном нажатии на кнопку
                    break;
            }
        }
    }
}
