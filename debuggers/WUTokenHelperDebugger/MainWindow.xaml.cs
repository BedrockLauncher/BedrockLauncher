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

namespace WUTokenHelperDebugger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void AccountsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelected();
        }

        private void UpdateSelected()
        {
            if (AccountsList.SelectedItem != null && AccountsList.SelectedItem is WUTokenHelperInterface.WUAccount)
            {
                var account = (WUTokenHelperInterface.WUAccount)AccountsList.SelectedItem;
                AccountName.Text = account.UserName;
                AccountType.Text = account.AccountType;
            }
            else
            {
                AccountName.Text = "N/A";
                AccountType.Text = "N/A";
            }
        }

        private void CollectAccounts()
        {
            AccountsList.Items.Clear();
            try
            {
                WUTokenHelperInterface.GetWUUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            foreach (var entry in WUTokenHelperInterface.CurrentAccounts) AccountsList.Items.Add(entry);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            CollectAccounts();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CollectAccounts();
            UpdateSelected();
        }
    }
}
