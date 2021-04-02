using System;
using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Core;

namespace BedrockLauncher.Pages.GameScreen
{
    /// <summary>
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class GamePage : Page
    {
        public string LastButtonName { get; set; } = string.Empty;

        public GamePage()
        {
            InitializeComponent();
            LastButtonName = PlayButton.Name;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.ButtonManager(sender, e);
        }

        private void InstallationsButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.ButtonManager(sender, e);
        }

        private void SkinsButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.ButtonManager(sender, e);
        }

        private void PatchNotesButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.ButtonManager(sender, e);
        }

        private void VersionsButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.ButtonManager(sender, e);
        }
    }
}
