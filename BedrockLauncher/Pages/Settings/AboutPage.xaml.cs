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
using System.Diagnostics;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Settings
{
    /// <summary>
    /// Логика взаимодействия для AboutPage.xaml
    /// </summary>
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        private void Page_Initialized(object sender, EventArgs e)
        {

        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Process.Start(new ProcessStartInfo(button.Tag.ToString()));
            e.Handled = true;
        }

        private void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Updater.CheckForUpdates();
        }

        private void ForceUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Updater.UpdateButton_Click(sender, e);
        }


    }
}
