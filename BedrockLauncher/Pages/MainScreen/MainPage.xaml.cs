using System;
using System.Windows;
using System.Windows.Controls;

namespace BedrockLauncher.Pages.MainScreen
{
    /// <summary>
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public string LastButtonName { get; set; } = string.Empty;

        public MainPage()
        {
            InitializeComponent();
            LastButtonName = PlayButton.Name;
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

        private void VersionsButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ButtonManager(sender, e);
        }
    }
}
