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
    /// Логика взаимодействия для PlayScreenPage.xaml
    /// </summary>
    public partial class PlayScreenPage : Page
    {
        public PlayScreenPage()
        {
            InitializeComponent();
        }
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            //((MainWindow)Application.Current.MainWindow).ButtonManager(sender, e);
        }

        private void InstallationsButton_Click(object sender, RoutedEventArgs e)
        {
            //((MainWindow)Application.Current.MainWindow).ButtonManager(sender, e);
        }

        private void SkinsButton_Click(object sender, RoutedEventArgs e)
        {
            //((MainWindow)Application.Current.MainWindow).ButtonManager(sender, e);
        }

        private void PatchNotesButton_Click(object sender, RoutedEventArgs e)
        {
            //((MainWindow)Application.Current.MainWindow).ButtonManager(sender, e);
        }
    }
}
