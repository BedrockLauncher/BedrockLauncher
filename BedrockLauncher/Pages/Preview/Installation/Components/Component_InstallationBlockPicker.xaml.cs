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
using System.Globalization;
using System.Collections;
using System.Reflection;
using System.Resources;
using System.IO;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using BedrockLauncher.ViewModels;
using BedrockLauncher.Classes;

namespace BedrockLauncher.Pages.Preview.Installation.Components
{
    /// <summary>
    /// Interaction logic for InstallationBlockPicker.xaml
    /// </summary>
    public partial class Component_InstallationBlockPicker : UserControl
    {

        public static readonly int NUMBER_OF_COLUMNS = 11;

        #region Definitions

        private List<string> BlockList = new List<string>();
        private List<Component_InstallationBlockPickerItem> Buttons { get; set; } = new List<Component_InstallationBlockPickerItem>();
        public bool IsIconCustom { get; private set; } = false;
        public string IconPath
        {
            get
            {
                try
                {
                    string uri = (SelectedBlockIcon.Source as BitmapImage).UriSource.OriginalString;
                    return System.IO.Path.GetFileName(uri);
                }
                catch
                {
                    IsIconCustom = false;
                    return "furnace.png";
                }

            }
        }

        #endregion

        #region Set Icon Data

        private void SetIconData(string uri)
        {
            try
            {
                BitmapImage bmp;
                if (IsIconCustom)
                {
                    bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(uri, UriKind.Absolute);
                    bmp.EndInit();
                }
                else
                {
                    bmp = new BitmapImage(new Uri(uri, UriKind.Relative));
                }

                    if (bmp != null) SelectedBlockIcon.Source = bmp;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
            }
        }

        #endregion

        #region Init

        public Component_InstallationBlockPicker()
        {
            InitializeComponent();
        }
        public void Init(BLInstallation i = null)
        {
            Task.Run(() => InitAsync(i));
        }


        public async Task InitAsync(BLInstallation i = null)
        {
            await Task.Run(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    BlockList = Constants.INSTALLATION_PREFABED_ICONS_LIST_RUNTIME;

                    SetIconData(BlockList.Where(x => x.Contains("furnace.png")).FirstOrDefault());

                    GenerateListItems();
                    UpdateDropdownArrow();

                    if (i != null)
                    {
                        IsIconCustom = i.IsCustomIcon;
                        SetIconData(i.IconPath_Full);
                    }

                    DropdownButton.IsEnabled = true;
                });
            });
        }

        #endregion

        #region Btn Click Events

        private void CrossBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (sender as Button);
            
            if (btn.Tag is string)
            {
                string path = btn.Tag.ToString();
                if (MainDataModel.Default.FilePaths.RemoveImageFromIconCache(path))
                {
                    GenerateListItems();
                }
            }
        }

        private void Btn_Click(object sender, EventArgs e)
        {
            Component_InstallationBlockPickerItem btn = (sender as Component_InstallationBlockPickerItem);

            if (btn.Tag is int)
            {
                int index = (int)btn.Tag;
                if (index == -1) AddIcon();
                else SetIcon(index);
            }
            else if (btn.Tag is string)
            {
                string path = btn.Tag.ToString();
                SetIcon(path);
            }
        }

        private void AddIcon()
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "PNG Files (*.png)|*.png"
            };

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string fileToImport = ofd.FileName;
                string fileToUse = MainDataModel.Default.FilePaths.AddImageToIconCache(fileToImport);
                if (fileToUse != string.Empty) SetIcon(fileToUse);
            }
        }

        private void SetIcon(string path)
        {
            IsIconCustom = true;
            SetIconData(path);
            DropdownButton.IsChecked = false;
            GenerateListItems();
        }
        private void SetIcon(int index)
        {
            IsIconCustom = false;
            SetIconData(BlockList[index]);
            DropdownButton.IsChecked = false;
        }

        #endregion

        #region Block List Generation

        private void GenerateListItems()
        {
            DropdownItemPanel.Items.Clear();
            foreach (var btn in Buttons)
            {
                btn.PreviewMouseLeftButtonDown -= Btn_Click;
                btn.CrossButton.Click -= CrossBtn_Click;
            }
            Buttons.Clear();

            GenerateDefaultIconList();
            GenerateCustomIconList();
        }
        private void GenerateDefaultIconList()
        {
            int remaining_spaces = (BlockList.Count / NUMBER_OF_COLUMNS) - (BlockList.Count % NUMBER_OF_COLUMNS) + (NUMBER_OF_COLUMNS % 2);

            for (int block = 0; block < BlockList.Count; block++)
            {
                var blockButton = CreateDefaultBlockButton(block);
                Buttons.Add(blockButton);
                DropdownItemPanel.Items.Add(blockButton);
            }

            for (int i = 0; i < remaining_spaces; i++)
            {
                DropdownItemPanel.Items.Add(new BlockPickerBlankItem());
            }
        }
        private void GenerateCustomIconList()
        {
            //Default Icons
            string[] files = Directory.GetFiles(MainDataModel.Default.FilePaths.GetCacheFolderPath(), "*.png");
            int item_count = files.Length;

            var plus_button = CreateDefaultBlockButton(-1);
            Buttons.Add(plus_button);
            DropdownItemPanel.Items.Add(plus_button);

            if (item_count == 0) return;

            foreach (var block in files)
            {
                var blockButton = CreateCustomBlockButton(block);
                Buttons.Add(blockButton);
                DropdownItemPanel.Items.Add(blockButton);
            }

        }
        private int GetIdealItemRowCount(int cols, int size)
        {
            int items_left = size;
            int ideal_row_count = 0;
            int currentIndex = 0;

            while (items_left != 0)
            {
                if (currentIndex > cols) currentIndex = 0;
                if (currentIndex == 0) ideal_row_count++;
                items_left--;
                currentIndex += 1;
            }
            return ideal_row_count;
        }
        private Component_InstallationBlockPickerItem CreateCustomBlockButton(string path)
        {
            Component_InstallationBlockPickerItem btn = CreateBaseBlockButton();
            int img_width_height = 50;
            btn.Tag = path;
            btn.Host.Tag = path;
            btn.CrossButton.Tag = path;
            btn.IsCustomImage = true;

            Image img = new Image();
            img.VerticalAlignment = VerticalAlignment.Bottom;
            img.HorizontalAlignment = HorizontalAlignment.Center;
            img.Width = img_width_height;
            img.Height = img_width_height;
            img.Stretch = Stretch.Uniform;

            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(System.IO.Path.Combine(MainDataModel.Default.FilePaths.GetCacheFolderPath(), path), UriKind.Absolute);
            bmp.EndInit();

            img.Source = bmp;
            btn.Host.Content = img;

            return btn;
        }
        private Component_InstallationBlockPickerItem CreateDefaultBlockButton(int index)
        {
            Component_InstallationBlockPickerItem btn = CreateBaseBlockButton();
            int img_width_height = (index == -1 ? 30 : 50);
            btn.Tag = index;
            btn.Host.Tag = index;
            btn.CrossButton.Tag = index;

            Image img = new Image();
            img.VerticalAlignment = VerticalAlignment.Bottom;
            img.HorizontalAlignment = HorizontalAlignment.Center;
            img.Width = img_width_height;
            img.Height = img_width_height;
            img.Stretch = Stretch.Uniform;

            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

            if (index == -1)
            {
                img.Source = Application.Current.FindResource("PlusIcon") as DrawingImage;
                btn.Host.Content = img;
            }
            else
            {
                string packUri = BlockList.ElementAtOrDefault(index) ?? BlockList[0];
                var bmp = new BitmapImage(new Uri(packUri, UriKind.Relative));
                img.Source = bmp;
                btn.Host.Content = img;
            }

            return btn;
        }
        private Component_InstallationBlockPickerItem CreateBaseBlockButton()
        {
            Component_InstallationBlockPickerItem btn = new Component_InstallationBlockPickerItem();
            btn.SelectItem += Btn_Click;
            btn.KeyDown += (sender, e) => { if (e.Key == Key.Enter) Btn_Click(sender, null); };
            btn.CrossButton.Click += CrossBtn_Click;
            return btn;
        }

        #endregion

        #region Dropdown Controls

        private void UpdateDropdownArrow()
        {
            switch (DropdownButton.IsChecked.Value)
            {
                case true:
                    ArrowScale.ScaleY = -2.0;
                    break;
                case false:
                    ArrowScale.ScaleY = 2.0;
                    break;
            }

        }
        private void DropdownButton_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized) UpdateDropdownArrow();
        }
        private void DropdownButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized) UpdateDropdownArrow();
        }

        #endregion
    }
}
