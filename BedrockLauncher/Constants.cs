using BedrockLauncher.UpdateProcessor.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Management.Deployment;

namespace BedrockLauncher
{
    public static class Constants
    {

        public static string AppVersion
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        private static readonly string MINECRAFT_PACKAGE_FAMILY = "Microsoft.MinecraftUWP_8wekyb3d8bbwe";
        private static readonly string MINECRAFT_PREVIEW_PACKAGE_FAMILY = "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe";

        public static readonly string LATEST_BETA_UUID = "latest_beta";
        public static readonly string LATEST_RELEASE_UUID = "latest_release";

        public static readonly string BETA_VERSION_ICONPATH = @"/BedrockLauncher;component/resources/images/icons/ico/crafting_table_block_icon.ico";
        public static readonly string RELEASE_VERSION_ICONPATH = @"/BedrockLauncher;component/resources/images/icons/ico/grass_block_icon.ico";

        internal static string GetPackageFamily(VersionType type)
        {
            return type == VersionType.Preview ? MINECRAFT_PREVIEW_PACKAGE_FAMILY : MINECRAFT_PACKAGE_FAMILY;
        }


        public const string ElevationExePath = "BedrockLauncher.Elevated.exe";


        private const string ThemesPathPrefix = @"pack://application:,,,/BedrockLauncher;component/resources/images/bg/play_screen/";

        public static Dictionary<string, string> Themes = new Dictionary<string, string>()
        {
            { "CavesAndCliffsPart1Update", ThemesPathPrefix + "1.17_caves_and_cliffs_part_1.jpg" },
            { "NetherUpdate",              ThemesPathPrefix + "1.16_nether_update.jpg" },
            { "BuzzyBeesUpdate",           ThemesPathPrefix + "1.15_buzzy_bees_update.jpg" },
            { "VillagePillageUpdate",      ThemesPathPrefix + "1.14_village_pillage_update.jpg" },
            { "UpdateAquatic",             ThemesPathPrefix + "1.13_update_aquatic.jpg" },
            { "TechnicallyUpdated",        ThemesPathPrefix + "1.13_technically_updated_java.jpg" },
            { "WorldOfColorUpdate",        ThemesPathPrefix + "1.12_world_of_color_update_java.jpg" },
            { "ExplorationUpdate",         ThemesPathPrefix + "1.11_exploration_update_java.jpg" },
            { "CombatUpdate",              ThemesPathPrefix + "1.09_combat_update_java.jpg" },
            { "CatsAndPandasUpdate",       ThemesPathPrefix + "1.08_cats_and_pandas.jpg" },
            { "PocketEditionRelease",      ThemesPathPrefix + "1.0_pocket_edition.jpg" },
            { "BedrockStandard",           ThemesPathPrefix + "bedrock_standard.jpg" },
            { "BedrockMaster",             ThemesPathPrefix + "bedrock_master.jpg" },
            { "EarlyLegacyConsole",        ThemesPathPrefix + "other_early_console_era.jpg" },
            { "MidLegacyConsole",          ThemesPathPrefix + "other_mid_legacy_console.jpg" },
            { "LateLegacyConsole",         ThemesPathPrefix + "other_late_legacy_console.jpg" },
            { "IndieDays",                 ThemesPathPrefix + "other_indie_days.jpg" },
            { "Dungeons",                  ThemesPathPrefix + "other_dungeons.jpg" },
            { "Original",                  ThemesPathPrefix + "original_image.jpg" }
        };

        public static string CurrentArchitecture
        {
            get
            {
                var currentArchitecture = RuntimeInformation.OSArchitecture;
                if (currentArchitecture == Architecture.Arm64) return "arm";
                else if (currentArchitecture == Architecture.X86) return "x86";
                else if (currentArchitecture == Architecture.X64) return "x64";
                else return "null";
            }
        }

        public static RemovalOptions PackageRemovalOptions
        {
            get
            {
                RemovalOptions options = new RemovalOptions();
                options |= RemovalOptions.RemoveForAllUsers;
                return options;
            }
        }

        public static DeploymentOptions PackageDeploymentOptions
        {
            get
            {
                DeploymentOptions options = new DeploymentOptions();
                options |= DeploymentOptions.DevelopmentMode;
                options |= DeploymentOptions.ForceTargetApplicationShutdown;
                return options;
            }
        }


        public static class Debugging
        {
            //IMPORTANT FOR DATA BINDING: DO NOT TOUCH (leave at false)
            //NOTE: More than 1000 lines will require paying for PostSharp
            public const bool ExcludeExplicitProperties = false;

            //TODO: Fix performance issues
            public static bool CalculateVersionSizes { get; internal set; } = true;
            public static bool RetriveNewVersionsOnLoad { get; internal set; } = true;
            public static bool CheckForUpdatesOnLoad { get; internal set; } = true;


            public static void ThrowIntentionalException()
            {
                Exception ex = new Exception("Intentionally Thrown Exception by Developer");
                throw ex;
            }
        }
    }



}
