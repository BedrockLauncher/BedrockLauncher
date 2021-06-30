using BedrockLauncher.Classes;
using BedrockLauncher.Methods;
using BedrockLauncher.Core.Classes;
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
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Play
{
    public partial class PlayScreenPage : Page
    {

        private const string ImagePathPrefix = @"pack://application:,,,/BedrockLauncher;component/resources/images/bg/play_screen/";

        public Dictionary<string, string> Images = new Dictionary<string, string>()
        {
            { "CavesAndCliffsPart1Update", ImagePathPrefix + "1.17_caves_and_cliffs_part_1.jpg" },
            { "NetherUpdate",              ImagePathPrefix + "1.16_nether_update.jpg" },
            { "BuzzyBeesUpdate",           ImagePathPrefix + "1.15_buzzy_bees_update.jpg" },
            { "VillagePillageUpdate",      ImagePathPrefix + "1.14_village_pillage_update.jpg" },
            { "UpdateAquatic",             ImagePathPrefix + "1.13_update_aquatic.jpg" },
            { "TechnicallyUpdated",        ImagePathPrefix + "1.13_technically_updated_java.jpg" },
            { "WorldOfColorUpdate",        ImagePathPrefix + "1.12_world_of_color_update_java.jpg" },
            { "ExplorationUpdate",         ImagePathPrefix + "1.11_exploration_update_java.jpg" },
            { "CombatUpdate",              ImagePathPrefix + "1.09_combat_update_java.jpg" },
            { "CatsAndPandasUpdate",       ImagePathPrefix + "1.08_cats_and_pandas.jpg" },
            { "PocketEditionRelease",      ImagePathPrefix + "1.0_pocket_edition.jpg" },
            { "BedrockStandard",           ImagePathPrefix + "bedrock_standard.jpg" },
            { "BedrockMaster",             ImagePathPrefix + "bedrock_master.jpg" },
            { "EarlyLegacyConsole",        ImagePathPrefix + "other_early_console_era.jpg" },
            { "MidLegacyConsole",          ImagePathPrefix + "other_mid_legacy_console.jpg" },
            { "LateLegacyConsole",         ImagePathPrefix + "other_late_legacy_console.jpg" },
            { "IndieDays",                 ImagePathPrefix + "other_indie_days.jpg" },
            { "Dungeons",                  ImagePathPrefix + "other_dungeons.jpg" },
            { "Original",                  ImagePathPrefix + "original_image.jpg" }
        };

        private bool HasLoadedOnce = false;

        public PlayScreenPage()
        {
            InitializeComponent();
        }


        public async void RefreshInstallationsList()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                var view = CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource) as CollectionView;
                view.Refresh();
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
                if (!HasLoadedOnce)
                {
                    InstallationsList.ItemsSource = LauncherModel.Default.ConfigManager.CurrentInstallations;
                    var view = CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource) as CollectionView;
                    view.Filter = LauncherModel.Default.ConfigManager.Filter_InstallationList;
                    HasLoadedOnce = true;
                }
                RefreshInstallationsList();
                LauncherModel.Default.UpdateUI();

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
            if (LauncherModel.Default.GameManager.GameProcess != null) LauncherModel.Default.GameManager.KillGame();
            else
            {
                var i = InstallationsList.SelectedItem as BLInstallation;
                bool KeepLauncherOpen = Properties.LauncherSettings.Default.KeepLauncherOpen;
                LauncherModel.Default.GameManager.Play(ViewModels.LauncherModel.Default.ConfigManager.CurrentProfile, i, KeepLauncherOpen);
            }
        }
    }
}
