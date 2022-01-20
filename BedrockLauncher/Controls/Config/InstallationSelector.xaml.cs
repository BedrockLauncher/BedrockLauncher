using BedrockLauncher.Handlers;
using BedrockLauncher.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace BedrockLauncher.Controls.Config
{
    /// <summary>
    /// Interaction logic for InstallationSelector.xaml
    /// </summary>
    public partial class InstallationSelector : ComboBox
    {

        public InstallationSelector()
        {
            InitializeComponent();
            DataContext = MainViewModel.Default;
        }
        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            FilterSortingHandler.Sort_InstallationList(ItemsSource);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ComboBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            FilterSortingHandler.Sort_InstallationList(ItemsSource);
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = FilterSortingHandler.Filter_InstallationList(e.Item);
        }
    }
}
