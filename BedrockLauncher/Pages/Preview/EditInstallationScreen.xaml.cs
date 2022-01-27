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
using BedrockLauncher.Extensions;
using BedrockLauncher.Classes;
using BedrockLauncher.ViewModels;
using System.Collections.ObjectModel;
using BedrockLauncher.UpdateProcessor.Extensions;

namespace BedrockLauncher.Pages.Preview
{


    public partial class EditInstallationScreen : Page
    {

        private bool IsEditMode = false;

        public EditInstallationsPageViewModel ViewModel { get; set; } = new EditInstallationsPageViewModel();


        public EditInstallationScreen(BLInstallation i = null)
        {
            this.DataContext = ViewModel;
            InitializeComponent();
            if (i != null) UpdateEditingFields(i);
            else UpdateAddingFields();
        }
        private void UpdateAddingFields()
        {
            InstallationIconSelect.Init();
        }

        private void UpdateEditingFields(BLInstallation i)
        {
            IsEditMode = true;

            ViewModel.SelectedVersionUUID = i.VersionUUID;
            ViewModel.InstallationName = i.DisplayName;
            ViewModel.InstallationDirectory = i.DirectoryName;
            ViewModel.SelectedUUID = i.InstallationUUID;

            InstallationIconSelect.Init(i);

            Header.SetResourceReference(TextBlock.TextProperty, "EditInstallationScreen_AltTitle");
            CreateButton.SetResourceReference(Button.ContentProperty, "EditInstallationScreen_AltCreateButton");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.MainViewModel.Default.SetOverlayFrame(null);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsEditMode) UpdateInstallation();
            else CreateInstallation();
        }

        private MCVersion GetVersion(string uuid)
        {
            return MainViewModel.Default.Versions.Where(x => x.UUID == uuid).FirstOrDefault();
        }

        private void UpdateInstallation()
        {
            MainViewModel.Default.Config.Installation_Edit(ViewModel.SelectedUUID, ViewModel.InstallationName, GetVersion(ViewModel.SelectedVersionUUID), ViewModel.InstallationDirectory, InstallationIconSelect.IconPath, InstallationIconSelect.IsIconCustom);
            MainViewModel.Default.SetOverlayFrame(null);
        }

        private void CreateInstallation()
        {
            MainViewModel.Default.Config.Installation_Create(ViewModel.InstallationName, GetVersion(ViewModel.SelectedVersionUUID), ViewModel.InstallationDirectory, InstallationIconSelect.IconPath, InstallationIconSelect.IsIconCustom);
            MainViewModel.Default.SetOverlayFrame(null);
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
            ViewModels.MainViewModel.Default.SetOverlayFrame(null);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is MCVersion)
            {
                var version = (e.Item as MCVersion);
                if (VersionDbExtensions.DoesVerionArchMatch(Constants.CurrentArchitecture, version.Architecture)) e.Accepted = true;
                else e.Accepted = false;
            }
            else e.Accepted = false;
        }
    }
}
