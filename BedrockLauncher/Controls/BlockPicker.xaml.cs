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

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for BlockPicker.xaml
    /// </summary>
    public partial class BlockPicker : UserControl
    {

        private List<Button> Buttons = new List<Button>();
        public string SelectedIconPath
        {
            get
            {
                return (SelectedBlockIcon.Source as BitmapImage).UriSource.OriginalString;
            }
            set
            {
                string packUri = value;
                var bmp = new BitmapImage(new Uri(packUri, UriKind.Relative));
                SelectedBlockIcon.Source = bmp;
            }
        }

        private List<string> BlockList = new List<string>()
        {
            @"/BedrockLauncher;component/resources/images/installation_icons/bedrock.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/bookshelf.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/bricks.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/cake.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/pumpkin.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/chest.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/clay.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/block_of_coal.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/coal_ore.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/cobblestone.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/crafting_table.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/creeper_head.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/block_of_diamond.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/diamond_ore.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/dirt.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/podzol.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/snowy_grass_block.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/block_of_emerald.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/emerald_ore.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/enchanting_table.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/end_stone.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/farmland.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/furnace.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/lit_furnace.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/glass.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/light_blue_glazed_terracotta.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/orange_glazed_terracotta.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/white_glazed_terracotta.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/glowstone.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/block_of_gold.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/gold_ore.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/grass_block.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/gravel.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/terracotta.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/ice.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/block_of_iron.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/iron_ore.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/lapis_lazuli_ore.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/birch_leaves.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/jungle_leaves.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/oak_leaves.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/spruce_leaves.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/lectern_book.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/acacia_log.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/birch_log.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/dark_oak_log.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/jungle_log.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/oak_log.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/spruce_log.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/mycelium.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/nether_bricks.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/netherrack.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/obsidian.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/acacia_planks.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/birch_planks.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/dark_oak_planks.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/jungle_planks.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/oak_planks.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/spruce_planks.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/nether_quartz_ore.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/red_sand.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/red_sandstone.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/block_of_redstone.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/redstone_ore.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/sand.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/sandstone.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/skeleton_skull.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/snow_block.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/soul_sand.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/stone.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/andesite.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/diorite.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/granite.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/tnt.png",
            @"/BedrockLauncher;component/resources/images/installation_icons/water.png",            
            @"/BedrockLauncher;component/resources/images/installation_icons/white_wool.png"
        };


        public BlockPicker()
        {
            InitializeComponent();
            GenerateListItems();
            UpdateDropdownArrow();
        }
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
                        Console.WriteLine(path);
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
            foreach (var btn in Buttons) btn.Click -= Btn_Click;
            Buttons.Clear();

            int item_index = -1;

            int columns = 11;
            int rows = 7;

            for (int x = 0; x < rows; x++)
            {
                StackPanel stackPanel = new StackPanel();
                stackPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C1C1C"));
                stackPanel.Orientation = Orientation.Horizontal;
                for (int y = 0; y < columns; y++)
                {
                    var blockButton = CreateBlock(item_index);
                    Buttons.Add(blockButton);
                    stackPanel.Children.Add(blockButton);
                    item_index++;
                }
                DropdownItemPanel.Children.Add(stackPanel);
            }

            Button CreateBlock(int index)
            {
                int item_height = 65;
                int item_width = 60;

                int img_width_height = (index == -1 ? 30 : 50);

                Button btn = new Button();
                btn.Style = Application.Current.FindResource("PanelButton") as Style;
                btn.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                btn.VerticalContentAlignment = VerticalAlignment.Stretch;
                btn.Padding = new Thickness(5);
                btn.Width = item_width;
                btn.Height = item_height;
                btn.Click += Btn_Click;
                btn.Tag = index;

                Image img = new Image();
                img.Width = img_width_height;
                img.Height = img_width_height;
                img.Stretch = Stretch.UniformToFill;

                if (index == -1)
                {
                    img.Source = Application.Current.Resources["PlusIcon"] as DrawingImage;
                    btn.Content = img;
                }
                else
                {
                    string packUri = BlockList.ElementAtOrDefault(index) ?? BlockList[0];
                    var bmp = new BitmapImage(new Uri(packUri, UriKind.Relative));
                    img.Source = bmp;
                    btn.Content = img;
                }


                return btn;
            }
        }
        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (sender as Button);
            int index = (int)btn.Tag;
            if (index == -1)
            {
                return;
            }
            else
            {
                SelectedIconPath = BlockList[index];
                DropdownButton.IsChecked = false;
            }
        }
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
    }
}
