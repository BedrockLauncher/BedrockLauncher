using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.UpdateProcessor.Authentication;

namespace BedrockLauncher.Pages.Welcome
{
    /// <summary>
    /// Logica di interazione per WelcomePageFour.xaml - Selezione Account Xbox Insider
    /// </summary>
    public partial class WelcomePageFour : Page
    {
        public WelcomePagesSwitcher pageSwitcher = new WelcomePagesSwitcher();
        public WelcomePageFour()
        {
            InitializeComponent();
            BackButton.IsEnabled = false;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            pageSwitcher.MoveToPage(3);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // Salva l'indice dell'account selezionato
            Properties.LauncherSettings.Default.CurrentInsiderAccountIndex = AccountComboBox.SelectedIndex;
            Properties.LauncherSettings.Default.Save();
            pageSwitcher.MoveToPage(5);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadInsiderAccounts();
        }

        private void LoadInsiderAccounts()
        {
            try
            {
                // Carica la lista degli account Xbox Insider
                AuthenticationManager.Default.GetWUUsers();

                // Popola il ComboBox con gli account
                AccountComboBox.ItemsSource = AuthenticationManager.Default.CurrentAccounts;

                // Seleziona l'account salvato precedentemente
                int savedIndex = Properties.LauncherSettings.Default.CurrentInsiderAccountIndex;
                if (savedIndex >= 0 && savedIndex < AccountComboBox.Items.Count)
                {
                    AccountComboBox.SelectedIndex = savedIndex;
                }
                else
                {
                    AccountComboBox.SelectedIndex = 0; // Default "No Authentication"
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Errore nel caricamento degli account Xbox: " + ex.Message);
                AccountComboBox.ItemsSource = null;
                AccountComboBox.Items.Clear();
                AccountComboBox.Items.Add(
                    new AuthenticationAccount
                    {
                        UserName = "Default Account",
                        AccountType = "(No Authentication)"
                    });
                AccountComboBox.SelectedIndex = 0;
            }
        }
    }
}
