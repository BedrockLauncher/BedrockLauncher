using BedrockLauncher.Classes;
using BedrockLauncher.Methods;
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

namespace BedrockLauncher.Pages.Play
{
    public partial class PlayScreenPage : Page
    {

        private const string ImagePathPrefix = @"pack://application:,,,/BedrockLauncher;component/resources/images/bg/play_screen/";

        public Dictionary<string, string> Images = new Dictionary<string, string>()
        {
            { "NetherUpdate", ImagePathPrefix + "1.16_nether_update.png" },
            { "BuzzyBeesUpdate", ImagePathPrefix + "1.15_buzzy_bees_update.jpg" },
            { "VillagePillageUpdate", ImagePathPrefix + "1.14_village_pillage_update.png" },
            { "UpdateAquatic", ImagePathPrefix + "1.13_update_aquatic.png" },
            { "TechnicallyUpdated", ImagePathPrefix + "1.13_technically_updated_java.jpg" },
            { "WorldOfColorUpdate", ImagePathPrefix + "1.12_world_of_color_update_java.png" },
            { "ExplorationUpdate", ImagePathPrefix + "1.11_exploration_update_java.jpg" },
            { "CombatUpdate", ImagePathPrefix + "1.09_combat_update_java.jpg" },
            { "CatsAndPandasUpdate", ImagePathPrefix + "1.08_cats_and_pandas.jpg" },
            { "PocketEditionRelease", ImagePathPrefix + "1.0_pocket_edition.png" },
            { "BedrockStandard", ImagePathPrefix + "bedrock_standard.jfif" },
            { "BedrockMaster", ImagePathPrefix + "bedrock_master.jfif" },
            { "EarlyLegacyConsole", ImagePathPrefix + "other_early_console_era.png" },
            { "MidLegacyConsole", ImagePathPrefix + "other_mid_legacy_console.jpeg" },
            { "LateLegacyConsole", ImagePathPrefix + "other_late_legacy_console.jpg" },
            { "IndieDays", ImagePathPrefix + "other_indie_days.jpg" },
            { "Dungeons", ImagePathPrefix + "other_dungeons.jpg" },
            { "Original", ImagePathPrefix + "original_image.png" }
        };

        public PlayScreenPage()
        {
            InitializeComponent();
            ConfigManager.ConfigStateChanged += ConfigManager_ConfigStateChanged;
        }

        private async void ConfigManager_ConfigStateChanged(object sender, EventArgs e)
        {
            await Task.Run(() => RefreshInstallations());
        }

        private async void RefreshInstallations()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                InstallationsList.Items.Cast<MCInstallation>().ToList().ForEach(x => x.Update());
                if (InstallationsList.SelectedItem == null) InstallationsList.SelectedItem = ConfigManager.CurrentInstallations.First();
                if (InstallationsList.ItemsSource != null) CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource).Refresh();
                else InstallationsList.ItemsSource = ConfigManager.CurrentInstallations;
            });
        }

        private string GetLatestImage()
        {
            return Images.First().Value;
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                ViewModels.LauncherModel.Default.UpdateUI();

                string packUri = string.Empty;
                string currentTheme = Properties.LauncherSettings.Default.CurrentTheme;

                bool isBugRock = Program.IsBugRockOfTheWeek();
                if (isBugRock)
                {
                    BedrockLogo.Visibility = Visibility.Collapsed;
                    BugrockLogo.Visibility = Visibility.Visible;
                    BugrockOfTheWeekLogo.Visibility = Visibility.Visible;
                }
                else
                {
                    BedrockLogo.Visibility = Visibility.Visible;
                    BugrockLogo.Visibility = Visibility.Collapsed;
                    BugrockOfTheWeekLogo.Visibility = Visibility.Collapsed;
                }

                switch (currentTheme)
                {
                    case "LatestUpdate":
                        packUri = GetLatestImage();
                        break;
                    default:
                        if (Images.ContainsKey(currentTheme)) packUri = Images.Where(x => x.Key == currentTheme).FirstOrDefault().Value;
                        else packUri = Images.Where(x => x.Key == "Original").FirstOrDefault().Value;
                        break;
                }

                var bmp = new BitmapImage(new Uri(packUri, UriKind.Absolute));
                ImageBrush.ImageSource = bmp;
            });
        }

        private void MainPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigManager.GameManager.GameProcess != null) ConfigManager.GameManager.KillGame();
            else
            {
                var i = InstallationsList.SelectedItem as MCInstallation;
                ConfigManager.GameManager.Play(i);
            }
        }
        private void InstallationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConfigManager.OnConfigStateChanged(sender, Events.ConfigStateArgs.Empty);
        }
    }
}
