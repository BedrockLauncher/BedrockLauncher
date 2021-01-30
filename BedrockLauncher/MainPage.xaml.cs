using System;
using System.Windows;
using System.Windows.Controls;

namespace BedrockLauncher
{
    /// <summary>
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ButtonManager(sender, e);
        }

        private void InstallationsButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ButtonManager(sender, e);
        }

        private void SkinsButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ButtonManager(sender, e);
        }

        private void PatchNotesButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ButtonManager(sender, e);
        }
    }
}
