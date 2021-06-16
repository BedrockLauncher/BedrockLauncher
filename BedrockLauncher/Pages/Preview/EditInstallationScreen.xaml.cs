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
using BedrockLauncher.Core.Classes;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Preview
{
    /// <summary>
    /// Interaction logic for EditInstallationScreen.xaml
    /// </summary>
    public partial class EditInstallationScreen : Page
    {
        private List<BLVersion> Versions { get; set; } = new List<BLVersion>();

        private bool IsEditMode = false;

        private int EditingIndex = -1;

        public EditInstallationScreen()
        {
            InitializeComponent();
            UpdateVersionsComboBox();
        }

        public EditInstallationScreen(int index, BLInstallation i)
        {
            InitializeComponent();
            UpdateVersionsComboBox();
            UpdateEditingFields(index, i);
        }

        private void UpdateEditingFields(int index, BLInstallation i)
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
            BLVersion latest_release = new BLVersion("latest_release", Application.Current.Resources["EditInstallationScreen_LatestRelease"].ToString(), false);
            BLVersion latest_beta = new BLVersion("latest_beta", Application.Current.Resources["EditInstallationScreen_LatestSnapshot"].ToString(), true);
            Versions.InsertRange(0, new List<BLVersion>() { latest_release, latest_beta });
        }

        private void UpdateVersionsComboBox()
        {
            Versions.Clear();
            InstallationVersionSelect.ItemsSource = null;
            foreach (var entry in LauncherModel.Default.ConfigManager.Versions) Versions.Add(BLVersion.Convert(entry));
            GetManualComboBoxEntries();
            InstallationVersionSelect.ItemsSource = Versions;
            var view = CollectionViewSource.GetDefaultView(InstallationVersionSelect.ItemsSource) as CollectionView;
            view.Filter = Filter_VersionList;
            InstallationVersionSelect.SelectedIndex = 0;
        }

        public bool Filter_VersionList(object obj)
        {
            BLVersion v = BLVersion.Convert(obj as MCVersion);

            if (v != null)
            {
                if (!Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return false;
                else if (!Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return false;
                else return true;
            }
            else return false;

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.LauncherModel.Default.SetOverlayFrame(null);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsEditMode) UpdateInstallation();
            else CreateInstallation();
        }

        private void UpdateInstallation()
        {
            LauncherModel.Default.ConfigManager.EditInstallation(EditingIndex, InstallationNameField.Text, InstallationDirectoryField.Text, Versions[InstallationVersionSelect.SelectedIndex], InstallationIconSelect.IconPath, InstallationIconSelect.IsIconCustom);
            LauncherModel.Default.SetOverlayFrame(null);
        }

        private void CreateInstallation()
        {
            LauncherModel.Default.ConfigManager.CreateInstallation(InstallationNameField.Text, Versions[InstallationVersionSelect.SelectedIndex], InstallationDirectoryField.Text, InstallationIconSelect.IconPath, InstallationIconSelect.IsIconCustom);
            LauncherModel.Default.SetOverlayFrame(null);
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
