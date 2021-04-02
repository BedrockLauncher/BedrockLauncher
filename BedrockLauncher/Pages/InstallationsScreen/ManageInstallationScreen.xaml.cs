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
using BedrockLauncher.Methods;
using BedrockLauncher.Classes;
using BedrockLauncher.Core;

namespace BedrockLauncher.Pages.InstallationsScreen
{
    /// <summary>
    /// Interaction logic for AddInstallationScreen.xaml
    /// </summary>
    public partial class ManageInstallationScreen : Page
    {
        private List<Classes.Version> Versions { get; set; } = new List<Classes.Version>();

        private bool IsEditMode = false;

        private int EditingIndex = -1;

        public ManageInstallationScreen()
        {
            InitializeComponent();
            UpdateVersionsComboBox();
        }

        public ManageInstallationScreen(int index, Installation i)
        {
            InitializeComponent();
            UpdateVersionsComboBox();
            UpdateEditingFields(index, i);
        }

        private void UpdateEditingFields(int index, Installation i)
        {
            IsEditMode = true;
            InstallationVersionSelect.SelectedItem = Versions.Where(x => x.UUID == i.VersionUUID).FirstOrDefault();
            InstallationNameField.Text = i.DisplayName;
            InstallationIconSelect.SelectedIconPath = i.IconPath;
            EditingIndex = index;

            Header.SetResourceReference(TextBlock.TextProperty, "EditingInstallationScreen_Header");
            CreateButton.SetResourceReference(Button.ContentProperty, "EditingInstallationScreen_SaveButton");
        }


        private void GetManualComboBoxEntries()
        {
            Classes.Version latest_release = new Classes.Version("latest_release", Application.Current.Resources["AddInstallationScreen_LatestRelease"].ToString(), false, ConfigManager.GameManager);
            Classes.Version latest_beta = new Classes.Version("latest_beta", Application.Current.Resources["AddInstallationScreen_LatestSnapshot"].ToString(), false, ConfigManager.GameManager);
            Versions.InsertRange(0, new List<Classes.Version>() { latest_release, latest_beta });
        }

        private void UpdateVersionsComboBox()
        {
            Versions.Clear();
            InstallationVersionSelect.ItemsSource = null;
            foreach (var entry in ConfigManager.Versions)
            {
                Versions.Add(entry);
            }
            GetManualComboBoxEntries();
            InstallationVersionSelect.ItemsSource = Versions;
            InstallationVersionSelect.SelectedIndex = 0;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.MainWindowOverlayFrame.Content = null;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsEditMode) UpdateInstallation();
            else CreateInstallation();

        }

        private void UpdateInstallation()
        {
            ConfigManager.EditInstallation(EditingIndex, InstallationNameField.Text, Versions[InstallationVersionSelect.SelectedIndex], InstallationIconSelect.SelectedIconPath);
            ConfigManager.MainThread.MainWindowOverlayFrame.Content = null;
            ConfigManager.OnConfigStateChanged(this, ConfigManager.ConfigStateArgs.Empty);
        }

        private void CreateInstallation()
        {
            ConfigManager.CreateInstallation(InstallationNameField.Text, Versions[InstallationVersionSelect.SelectedIndex], InstallationIconSelect.SelectedIconPath);
            ConfigManager.MainThread.MainWindowOverlayFrame.Content = null;
            ConfigManager.OnConfigStateChanged(this, ConfigManager.ConfigStateArgs.Empty);
        }
    }
}
