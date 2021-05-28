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
using BedrockLauncher.Controls.Items;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for BlockPicker.xaml
    /// </summary>
    public partial class BlockPicker : UserControl
    {

        #region Definitions

        private List<string> BlockList = new List<string>();
        private List<BlockPickerItem> Buttons { get; set; } = new List<BlockPickerItem>();
        public bool IsIconCustom { get; private set; } = false;
        public string IconPath { get => (SelectedBlockIcon.Source as BitmapImage).UriSource.OriginalString; }

        #endregion

        #region Set Icon Data

        private void SetPrefabbedIconData(string value)
        {
            string packUri = value;
            var bmp = new BitmapImage(new Uri(packUri, UriKind.Relative));
            SelectedBlockIcon.Source = bmp;
            IsIconCustom = false;
        }
        private void SetCustomIconData(string filePath)
        {
            try
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(filePath, UriKind.Absolute);
                bmp.EndInit();

                if (bmp != null)
                {
                    SelectedBlockIcon.Source = bmp;
                    IsIconCustom = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        #endregion

        #region Init

        public BlockPicker()
        {
            InitializeComponent();
            GetBlockList();

            SetPrefabbedIconData(BlockList.Where(x => x.Contains("furnace.png")).FirstOrDefault());

            GenerateListItems();
            UpdateDropdownArrow();
        }
        public void Init(string value, bool isCustom)
        {
            IsIconCustom = isCustom;
            if (IsIconCustom) SetCustomIconData(value);
            else SetPrefabbedIconData(value);
        }

        public void GetBlockList()
        {
            var resource_data = Properties.Resources._BlockOrder;
            var list = resource_data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
            for (int i = 0; i < list.Count; i++) list[i] = Methods.FilepathManager.PrefabedIconRootPath + list[i];
            BlockList = list;
        }

        #endregion

        #region Btn Click Events

        private void CrossBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (sender as Button);
            
            if (btn.Tag is string)
            {
                string path = btn.Tag.ToString();
                if (Methods.FilepathManager.RemoveImageFromIconCache(path))
                {
                    GenerateListItems();
                }
            }
        }

        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (sender as Button);

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
                string fileToUse = Methods.FilepathManager.AddImageToIconCache(fileToImport);
                if (fileToUse != string.Empty) SetIcon(fileToUse);
            }
        }

        private void SetIcon(string path)
        {
            SetCustomIconData(path);
            DropdownButton.IsChecked = false;
            GenerateListItems();
        }
        private void SetIcon(int index)
        {
            SetPrefabbedIconData(BlockList[index]);
            DropdownButton.IsChecked = false;
        }

        #endregion

        #region Block List Generation

        private List<string> GetBlockListAuto()
        {
            List<string> resourcePaths = new List<string>();
            var assembly = Assembly.GetExecutingAssembly();
            var rm = new ResourceManager(assembly.GetName().Name + ".g", assembly);
            try
            {
                var list = rm.GetResourceSet(CultureInfo.CurrentCulture, true, true);
                foreach (DictionaryEntry item in list)
                {
                    string resource_path = (string)item.Key;
                    if (resource_path.StartsWith(@"resources/images/installation_icons"))
                    {
                        string path = @"/BedrockLauncher;component/" + resource_path;
                        System.Diagnostics.Debug.WriteLine(path);
                        resourcePaths.Add(path);
                    }
                }
            }
            finally
            {
                rm.ReleaseAllResources();
            }

            return resourcePaths;
        }
        private void GenerateListItems()
        {
            DropdownItemPanel.Children.Clear();
            foreach (var btn in Buttons)
            {
                btn.MainButton.Click -= Btn_Click;
                btn.CrossButton.Click -= CrossBtn_Click;
            }
            Buttons.Clear();

            GenerateDefaultIconList();
            GenerateCustomIconList();
        }
        private void GenerateDefaultIconList()
        {
            //Default Icons
            int item_index = -1;
            int columns = 11;
            int item_count = BlockList.Count;
            int rows = GetIdealItemRowCount(columns - 1, item_count + 1);

            for (int x = 0; x < rows; x++)
            {
                StackPanel stackPanel = new StackPanel();
                stackPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C1C1C"));
                stackPanel.Orientation = Orientation.Horizontal;
                for (int y = 0; y < columns; y++)
                {
                    if (item_count > item_index)
                    {
                        var blockButton = CreateDefaultBlockButton(item_index);
                        Buttons.Add(blockButton);
                        stackPanel.Children.Add(blockButton);
                        item_index++;
                    }
                }
                DropdownItemPanel.Children.Add(stackPanel);
            }
        }
        private void GenerateCustomIconList()
        {
            //Default Icons
            string[] files = Directory.GetFiles(Methods.FilepathManager.GetCacheFolderPath(), "*.png");
            int item_count = files.Length;

            if (item_count == 0) return;

            int item_index = 0;
            int columns = 11;
            int rows = GetIdealItemRowCount(columns - 1, item_count);


            for (int x = 0; x < rows; x++)
            {
                StackPanel stackPanel = new StackPanel();
                stackPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C1C1C"));
                stackPanel.Orientation = Orientation.Horizontal;
                for (int y = 0; y < columns; y++)
                {
                    if (item_count > item_index)
                    {
                        var blockButton = CreateCustomBlockButton(files[item_index]);
                        Buttons.Add(blockButton);
                        stackPanel.Children.Add(blockButton);
                        item_index++;
                    }
                }
                DropdownItemPanel.Children.Add(stackPanel);
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
        private BlockPickerItem CreateCustomBlockButton(string path)
        {
            BlockPickerItem btn = CreateBaseBlockButton();
            int img_width_height = 50;
            btn.MainButton.Tag = path;
            btn.CrossButton.Tag = path;
            btn.IsCustomImage = true;

            Image img = new Image();
            img.Width = img_width_height;
            img.Height = img_width_height;
            img.Stretch = Stretch.UniformToFill;

            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(System.IO.Path.Combine(Methods.FilepathManager.GetCacheFolderPath(), path), UriKind.Absolute);
            bmp.EndInit();

            img.Source = bmp;
            btn.MainButton.Content = img;

            return btn;
        }
        private BlockPickerItem CreateDefaultBlockButton(int index)
        {
            BlockPickerItem btn = CreateBaseBlockButton();
            int img_width_height = (index == -1 ? 30 : 50);
            btn.MainButton.Tag = index;
            btn.CrossButton.Tag = index;

            Image img = new Image();
            img.Width = img_width_height;
            img.Height = img_width_height;
            img.Stretch = Stretch.UniformToFill;

            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

            if (index == -1)
            {
                img.Source = Application.Current.Resources["PlusIcon"] as DrawingImage;
                btn.MainButton.Content = img;
            }
            else
            {
                string packUri = BlockList.ElementAtOrDefault(index) ?? BlockList[0];
                var bmp = new BitmapImage(new Uri(packUri, UriKind.Relative));
                img.Source = bmp;
                btn.MainButton.Content = img;
            }

            return btn;
        }
        private BlockPickerItem CreateBaseBlockButton()
        {
            BlockPickerItem btn = new BlockPickerItem();
            btn.MainButton.Click += Btn_Click;
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
                default:
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
