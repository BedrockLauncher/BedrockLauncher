using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BedrockLauncher.Classes;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Preview.Profile
{
    public partial class Component_AddProfileContainer : UserControl
    {
        public bool isEditMode = false;

        public event EventHandler GoBack;
        public event EventHandler Confirm;

        public EditProfileContainerViewModel ViewModel { get; set; } = new EditProfileContainerViewModel();

        public Component_AddProfileContainer()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        public Component_AddProfileContainer(BLProfile profileToEdit)
        {
            InitializeComponent();
            DataContext = ViewModel;
            isEditMode = true;

            ViewModel.ProfileName = profileToEdit.Name;
            ViewModel.ProfileUUID = profileToEdit.UUID;
            ViewModel.ProfileImage = profileToEdit.ImagePath;
            ViewModel.ProfileDirectory = profileToEdit.ProfilePath;

            CreateProfileSubtitle.Text = this.FindResource("NewProfile_EditProfileSubTitle") as string;
            CreateProfileButtonText.Text = this.FindResource("NewProfile_EditProfileButton") as string;

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            GoBack?.Invoke(this, EventArgs.Empty);
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ConfirmProfile();
            EvaluateDirectory();
        }

        private void CreateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            EvaluateDirectory();
            ConfirmProfile();
        }
        public void ConfirmProfile()
        {
            if (ViewModel.ProfileName.Length >= 1)
            {
                if (isEditMode) UpdateProfile();
                else CreateProfile();
            }
        }

        public void UpdateProfile()
        {
            if (MainDataModel.Default.Config.Profile_Edit(ViewModel.ProfileName, ViewModel.ProfileUUID, ViewModel.ProfileDirectory, ViewModel.ProfileImage))
            {
                Confirm?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                CreateProfileText.SetResourceReference(TextBlock.TextProperty, "NewProfile_CreateProfileText_Error");
                CreateProfileText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            }
        }
        public void CreateProfile()
        {
            if (MainDataModel.Default.Config.Profile_Add(ViewModel.ProfileName, ViewModel.ProfileUUID, ViewModel.ProfileDirectory, ViewModel.ProfileImage))
            {
                Confirm?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                CreateProfileText.SetResourceReference(TextBlock.TextProperty, "NewProfile_CreateProfileText_Error");
                CreateProfileText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            }
        }

        private void ProfileNameTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EvaluateDirectory();
        }

        private void EvaluateDirectory()
        {
            if (string.IsNullOrEmpty(ViewModel.ProfileDirectory) || ViewModel.ProfileName.StartsWith(ViewModel.ProfileDirectory)) 
                ViewModel.ProfileDirectory = ViewModel.ProfileName;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog()
            {
                Filter = "PNG Files (*.png)|*.png"
            };

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                ViewModel.ProfileImage = ofd.FileName;
        }
    }
}
