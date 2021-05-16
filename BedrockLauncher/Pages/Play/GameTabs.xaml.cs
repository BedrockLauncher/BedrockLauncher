using System;
using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Methods;

namespace BedrockLauncher.Pages.Play
{
    /// <summary>
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class GameTabs : Page
    {
        public string LastButtonName { get; set; } = string.Empty;

        public GameTabs()
        {
            InitializeComponent();
            LastButtonName = PlayButton.Name;
        }

        private void Buttons_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.ViewModel.ButtonManager(sender, e);
        }
    }
}
