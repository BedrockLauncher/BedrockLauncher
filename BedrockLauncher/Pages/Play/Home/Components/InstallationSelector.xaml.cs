﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BedrockLauncher.Handlers;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Play.Home.Components
{
    /// <summary>
    /// Interaction logic for InstallationSelector.xaml
    /// </summary>
    public partial class InstallationSelector : ComboBox
    {

        public InstallationSelector()
        {
            InitializeComponent();
            DataContext = MainDataModel.Default;
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
