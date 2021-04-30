using BedrockLauncher.Classes.SkinPack;
using BedrockLauncher.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace BedrockLauncher.Pages
{
    /// <summary>
    /// Interaction logic for EditSkinScreen.xaml
    /// </summary>
    public partial class EditSkinScreen : Page
    {

        private MCSkinPack skinPack;

        private MCSkin skin;

        private int skin_index = -1;

        private bool isEditMode = false;

        #region Init

        public EditSkinScreen()
        {
            InitializeComponent();
            Init(false);
        }

        public EditSkinScreen(MCSkinPack _skinPack)
        {
            skinPack = _skinPack;
            skin = new MCSkin(_skinPack.Directory);

            InitializeComponent();
            Init(false);
            InitSkinFormFeilds();
        }


        public EditSkinScreen(MCSkinPack _skinPack, MCSkin _skin, int index)
        {
            skinPack = _skinPack;
            skin = _skin;
            skin_index = index;

            InitializeComponent();
            Init(true);
            InitEditSkinFormFeilds();
        }

        private void Init(bool _isEditMode = true)
        {
            isEditMode = _isEditMode;
            if (isEditMode)
            {
                Header.SetResourceReference(TextBlock.TextProperty, "EditSkinScreen_Title");
                CreateButton.SetResourceReference(Button.ContentProperty, "GeneralText_Save");
            }
            else
            {
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
                    if (entry.KeyName.StartsWith(skin_prefix))
                    {
                        string localizationName = entry.KeyName.Replace(skin_prefix, "");
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
            skin.texture = TextureCombobox.Text;
            SkinPreview.UpdateSkin(skin);
        }

        private void GeometryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            skin.geometry = GeometryTextBox.Text;
            SkinPreview.UpdateSkin(skin);
        }

        #endregion

        #region Closing Events

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.SetOverlayFrame(null);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            skin.localization_name = LocalizationTextBox.Text;
            skin.type = TypeTextBox.Text;

            if (isEditMode) skinPack.EditSkin(skin_index, skin);
            else skinPack.AddSkin(skin);
            ConfigManager.MainThread.SetOverlayFrame(null);
            ConfigManager.MainThread.RefreshSkinsPage();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.MainThread.SetOverlayFrame(null);
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
                string fileName = Extensions.GetAvaliableFileName(Path.GetFileName(ofd.FileName), skinPack.Directory);
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


    }
}
