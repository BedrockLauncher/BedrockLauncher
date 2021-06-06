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
using BedrockLauncher.Dungeons.Methods;

namespace BedrockLauncher.Dungeons.Pages
{
    public partial class PlayPage : Page
    {

        public PlayPage()
        {
            InitializeComponent();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void MainPlayButton_Click(object sender, RoutedEventArgs e)
        {
            GameManager.LaunchDungeons();
        }
    }
}
