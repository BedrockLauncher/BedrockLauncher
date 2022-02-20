
using BedrockLauncher.Extensions;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using JemExtensions;
using BedrockLauncher.Classes.SkinPack;
using BedrockLauncher.ViewModels;
using System.Windows.Navigation;

namespace BedrockLauncher.Pages.Preview
{
    /// <summary>
    /// Interaction logic for EditSkinScreen.xaml
    /// </summary>
    public partial class EditSkinScreen : Page
    {

        private MCSkinPack skinPack;

        private MCSkin skin;

        private MCSkin temp_skin;

        private int skin_index = -1;

        private bool isEditMode = false;

        #region Init

        public EditSkinScreen()
        {
            InitializeComponent();

            isEditMode = false;
        }

        public EditSkinScreen(MCSkinPack _skinPack)
        {
            InitializeComponent();

            skinPack = _skinPack;
            skin = new MCSkin(_skinPack.Directory);
            temp_skin = (MCSkin)skin.Clone();
            isEditMode = false;
        }


        public EditSkinScreen(MCSkinPack _skinPack, MCSkin _skin, int index)
        {
            InitializeComponent();

            skinPack = _skinPack;
            skin = _skin;
            temp_skin = (MCSkin)skin.Clone();
            skin_index = index;

            isEditMode = true;
        }

        private void OverlayFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            SkinPreview.Visibility = Visibility.Collapsed;
        }

        private void OverlayFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (MainViewModel.Default.ErrorFrame.Content == null && MainViewModel.Default.OverlayFrame.Content == null) SkinPreview.Visibility = Visibility.Visible;
            else SkinPreview.Visibility = Visibility.Collapsed;
        }

        private void Init()
        {
            MainViewModel.Default.OverlayFrame.Navigating += OverlayFrame_Navigating;
            MainViewModel.Default.OverlayFrame.Navigated += OverlayFrame_Navigated;

            if (isEditMode)
            {
                InitEditSkinFormFeilds();
                Header.SetResourceReference(TextBlock.TextProperty, "EditSkinScreen_Title");
                CreateButton.SetResourceReference(Button.ContentProperty, "GeneralText_Save");
            }
            else
            {
                InitSkinFormFeilds();
                Header.SetResourceReference(TextBlock.TextProperty, "EditSkinScreen_AltTitle");
                CreateButton.SetResourceReference(Button.ContentProperty, "GeneralText_Add");
            }

            InitGeometryDropdown();
            InitTypeDropdown();
        }


        private void InitSkinFormFeilds()
        {
            UpdateTextureDropdown(skinPack);
            UpdateLocalizationDropdown(skinPack);
        }

        private void InitEditSkinFormFeilds()
        {
            TypeTextBox.Text = skin.type;
            TextureCombobox.Text = skin.texture;
            GeometryTextBox.Text = skin.geometry;
            LocalizationTextBox.Text = skin.localization_name;
            SkinPreview.UpdateSkin(skin);
            InitSkinFormFeilds();
        }

        private void InitGeometryDropdown()
        {
            GeometryTextBox.Items.Add("geometry.humanoid.customSlim");
            GeometryTextBox.Items.Add("geometry.humanoid.custom");
        }

        private void InitTypeDropdown()
        {
            TypeTextBox.Items.Add("free");
        }



        #endregion

        #region Dropdown Refresh

        private void UpdateLocalizationDropdown(MCSkinPack skinPack)
        {
            LocalizationTextBox.Items.Clear();
            foreach (var item in skinPack.Texts.Values.Values)
            {
                foreach (var entry in item.Global)
                {
                    string skin_prefix = string.Format("skin.{0}.", skinPack.Content.localization_name);
                    if (entry.Key.StartsWith(skin_prefix))
                    {
                        string localizationName = entry.Key.Replace(skin_prefix, "");
                        if (!LocalizationTextBox.Items.Contains(localizationName)) LocalizationTextBox.Items.Add(localizationName);
                    }
                }

            }
        }

        private void UpdateTextureDropdown(MCSkinPack skinPack)
        {
            TextureCombobox.Items.Clear();
            var possible_images = System.IO.Directory.GetFiles(skinPack.Directory, "*.png", System.IO.SearchOption.AllDirectories);
            foreach (var entry in possible_images)
            {
                TextureCombobox.Items.Add(System.IO.Path.GetFileName(entry));
            }
        }

        #endregion

        #region Update Events

        private void TextureCombobox_TextChanged(object sender, TextChangedEventArgs e)
        {
            temp_skin.texture = TextureCombobox.Text;
            SkinPreview.UpdateSkin(temp_skin);
        }

        private void GeometryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            temp_skin.geometry = GeometryTextBox.Text;
            SkinPreview.UpdateSkin(temp_skin);
        }

        #endregion

        #region Closing Events

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SkinPreview.Visibility = Visibility.Collapsed;
            ViewModels.MainViewModel.Default.SetOverlayFrame(null);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            temp_skin.localization_name = LocalizationTextBox.Text;
            temp_skin.type = TypeTextBox.Text;

            skin = temp_skin;

            if (isEditMode) skinPack.EditSkin(skin_index, skin);
            else skinPack.AddSkin(skin);

            SkinPreview.Visibility = Visibility.Collapsed;
            ViewModels.MainViewModel.Default.SetOverlayFrame(null);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SkinPreview.Visibility = Visibility.Collapsed;
            ViewModels.MainViewModel.Default.SetOverlayFrame(null);
        }

        #endregion



        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "PNG Files (*.png)|*.png"
            };

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string fileToImport = ofd.FileName;
                string fileName = FileExtensions.GetAvaliableFileName(Path.GetFileName(ofd.FileName), skinPack.Directory);
                File.Copy(fileToImport, Path.Combine(skinPack.Directory, fileName));
                UpdateTextureDropdown(skinPack);
                TextureCombobox.Text = fileName;
            }
        }

        private void TextureCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void GeometryTextBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Page_Initialized(object sender, RoutedEventArgs e)
        {
            Init();
        }
    }
}
