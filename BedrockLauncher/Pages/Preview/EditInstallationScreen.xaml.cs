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

namespace BedrockLauncher.Pages.Preview
{
    /// <summary>
    /// Interaction logic for AddInstallationScreen.xaml
    /// </summary>
    public partial class EditInstallationScreen : Page
    {
        private List<Classes.MCVersion> Versions { get; set; } = new List<Classes.MCVersion>();

        private bool IsEditMode = false;

        private int EditingIndex = -1;

        public EditInstallationScreen()
        {
            InitializeComponent();
            UpdateVersionsComboBox();
        }

        public EditInstallationScreen(int index, MCInstallation i)
        {
            InitializeComponent();
            UpdateVersionsComboBox();
            UpdateEditingFields(index, i);
        }

        private void UpdateEditingFields(int index, MCInstallation i)
        {
            IsEditMode = true;
            InstallationVersionSelect.SelectedItem = Versions.Where(x => x.UUID == i.VersionUUID).FirstOrDefault();
            InstallationNameField.Text = i.DisplayName;
            InstallationDirectoryField.Text = i.DirectoryName;
            InstallationIconSelect.Init(i.IconPath_Full, i.IsCustomIcon);
            EditingIndex = index;

            Header.SetResourceReference(TextBlock.TextProperty, "EditInstallationScreen_AltTitle");
            CreateButton.SetResourceReference(Button.ContentProperty, "EditInstallationScreen_AltCreateButton");
        }


        private void GetManualComboBoxEntries()
        {
            Classes.MCVersion latest_release = new Classes.MCVersion("latest_release", Application.Current.Resources["EditInstallationScreen_LatestRelease"].ToString(), false, ConfigManager.GameManager);
            Classes.MCVersion latest_beta = new Classes.MCVersion("latest_beta", Application.Current.Resources["EditInstallationScreen_LatestSnapshot"].ToString(), false, ConfigManager.GameManager);
            Versions.InsertRange(0, new List<Classes.MCVersion>() { latest_release, latest_beta });
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
            ViewModels.LauncherModel.Default.SetOverlayFrame(null, false, false);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsEditMode) UpdateInstallation();
            else CreateInstallation();
        }

        private void UpdateInstallation()
        {
            ConfigManager.EditInstallation(EditingIndex, InstallationNameField.Text, InstallationDirectoryField.Text, Versions[InstallationVersionSelect.SelectedIndex], InstallationIconSelect.IconPath, InstallationIconSelect.IsIconCustom);
            ViewModels.LauncherModel.Default.SetOverlayFrame(null, false, false);
            ConfigManager.OnConfigStateChanged(this, Events.ConfigStateArgs.Empty);
        }

        private void CreateInstallation()
        {
            ConfigManager.CreateInstallation(InstallationNameField.Text, Versions[InstallationVersionSelect.SelectedIndex], InstallationDirectoryField.Text, InstallationIconSelect.IconPath, InstallationIconSelect.IsIconCustom);
            ViewModels.LauncherModel.Default.SetOverlayFrame(null, false, false);
            ConfigManager.OnConfigStateChanged(this, Events.ConfigStateArgs.Empty);
        }

        private void InstallationDirectoryField_TextChanged(object sender, TextChangedEventArgs e)
        {
            string currentName = InstallationDirectoryField.Text;
            string fixedName = ValidatePathName(currentName);
            if (fixedName != currentName) InstallationDirectoryField.Text = fixedName;


            string ValidatePathName(string pathName)
            {
                char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
                return new string(pathName.Where(ch => !invalidFileNameChars.Contains(ch)).ToArray());
            }
        }

        private void InstallationDirectoryField_TextInput(object sender, TextCompositionEventArgs e)
        {
            
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.LauncherModel.Default.SetOverlayFrame(null);
        }
    }
}
