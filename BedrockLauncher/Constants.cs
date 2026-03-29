using BedrockLauncher.Classes;
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

        private const string APP_RESOURCEPATH_PREFIX = @"pack://application:,,,/BedrockLauncher;component/";
        private const string APP_RESOURCEPATH_SHORTPREFIX = @"/BedrockLauncher;component/";

        private static readonly string MINECRAFT_PACKAGE_FAMILY = "Microsoft.MinecraftUWP_8wekyb3d8bbwe";
        private static readonly string MINECRAFT_PREVIEW_PACKAGE_FAMILY = "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe";

        private const string MINECRAFT_URI = "minecraft";   // both release and beta
        private const string MINECRAFT_PREVIEW_URI = "minecraft-preview";

        public const string MINECRAFT_PROCESS_NAME = "Minecraft.Windows";

        public static readonly string LATEST_BETA_UUID = "latest_beta";
        public static readonly string LATEST_RELEASE_UUID = "latest_release";
        public static readonly string LATEST_PREVIEW_UUID = "latest_preview";

        public static readonly string UPDATES_RELEASE_PAGE = "https://github.com/BedrockLauncher/BedrockLauncher/releases/latest";
        public static readonly string UPDATES_BETA_PAGE = "https://github.com/BedrockLauncher/BedrockLauncher-Beta/releases";

        public static readonly string CUSTOM_VERSION_ICONPATH = INSTALLATIONS_PREFABED_ICONS_ROOT + @"Custom_Package.png";
        public static readonly string BETA_VERSION_ICONPATH = INSTALLATIONS_PREFABED_ICONS_ROOT + @"Crafting_Table.png";
        public static readonly string RELEASE_VERSION_ICONPATH = INSTALLATIONS_PREFABED_ICONS_ROOT + @"Grass_Block.png";
        public static readonly string PREVIEW_VERSION_ICONPATH = INSTALLATIONS_PREFABED_ICONS_ROOT + @"Grass_Path.png";
        public static readonly string UNKNOWN_VERSION_ICONPATH = INSTALLATIONS_PREFABED_ICONS_ROOT + @"Bedrock.png";

        public static readonly string INSTALLATIONS_ICONPATH = APP_RESOURCEPATH_SHORTPREFIX + @"Resources/images/installation_icons/";
        public static readonly string INSTALLATIONS_FALLBACK_ICONPATH = @"Furnace.png";
        public static readonly string INSTALLATIONS_LATEST_RELEASE_ICONPATH = "Grass_Block.png";
        public static readonly string INSTALLATIONS_LATEST_BETA_ICONPATH = "Crafting_Table.png";
        public static readonly string INSTALLATIONS_LATEST_PREVIEW_ICONPATH = "Grass_Path.png";

        public static readonly string PATCHNOTE_BETA_IMG = APP_RESOURCEPATH_PREFIX + "resources/images/packs/dev_pack_icon.png";
        public static readonly string PATCHNOTE_RELEASE_IMG = APP_RESOURCEPATH_PREFIX + "resources/images/packs/pack_icon.png";

        public static readonly string PATCHNOTES_IMGPREFIX_URL = @"https://launchercontent.mojang.com/";
        public static readonly string PATCHNOTES_CONTENT_BASE_URL = @"https://launchercontent.mojang.com/v2/";
        public static readonly string PATCHNOTES_MAIN_V2_URL = @"https://launchercontent.mojang.com/v2/bedrockPatchNotes.json";
        public static readonly string PATCHNOTES_TESTING_URL = @"https://launchercontent.mojang.com/testing/bedrockPatchNotes.json";

        public static readonly string PATCHNOTES_RELEASE_CHANGELOG_URL = @"https://feedback.minecraft.net/hc/en-us/sections/360001186971-Release-Changelogs";
        public static readonly string PATCHNOTES_PREVIEW_CHANGELOG_URL = @"https://feedback.minecraft.net/hc/en-us/sections/360001185332-Beta-and-Preview-Information-and-Changelogs";

        public static readonly string RSS_FALLBACK_IMG = APP_RESOURCEPATH_PREFIX + @"resources/images/packs/invalid_pack.png";
        public static readonly string RSS_LAUNCHER_IMG_PATH = @"https://launchercontent.mojang.com/";
        public static readonly string RSS_MINECRAFT_IMG_PATH = @"https://www.minecraft.net/";

        public static readonly string RSS_LAUNCHER_V2_URL = @"https://launchercontent.mojang.com/v2/news.json";
        public static readonly string RSS_COMMUNITY_URL = @"https://www.minecraft.net/en-us/feeds/community-content/rss";
        public static readonly string RSS_FORUMS_URL = @"https://www.minecraftforum.net/news.rss";

        public static readonly string PROFILE_DEFAULT_IMG = APP_RESOURCEPATH_PREFIX + @"resources/images/icons/user_icon.png";
        public static readonly string PROFILE_CUSTOM_IMG_NAME = ".profile.png";

        public const string FIRST_EDITOR_RELEASE = "1.21.50";
        public const string FIRST_EDITOR_PREVIEW = "1.19.80.20";

        public const string FIRST_GDK_VERSION = "1.21.120";

        internal static string GetPackageFamily(VersionType type)
        {
            return type == VersionType.Preview ? MINECRAFT_PREVIEW_PACKAGE_FAMILY : MINECRAFT_PACKAGE_FAMILY;
        }

        internal static string GetUri(VersionType type)
        {
            return type == VersionType.Preview ? MINECRAFT_PREVIEW_URI : MINECRAFT_URI;
        }

        internal static MCVersion GetMinimumEditorVersion(VersionType type)
        {
            return new MCVersion(type == VersionType.Preview ? FIRST_EDITOR_PREVIEW : FIRST_EDITOR_RELEASE);
        }

        internal static MCVersion GetMinimumGDKVersion()
        {
            return new MCVersion(FIRST_GDK_VERSION);
        }

        public const string ThemesCustomPrefix = "[+] ";
        private const string ThemesPathPrefix = APP_RESOURCEPATH_PREFIX + @"resources/images/bg/play_screen/";

        public static Dictionary<string, string> Themes = new Dictionary<string, string>()
        {
            { "TinyTakeover",                      ThemesPathPrefix + "26.10_tiny_takeover.png" },
            { "MountsOfMayhem",                    ThemesPathPrefix + "1.21.130_mounts_of_mayhem.png" },
            { "TheCopperAge",                      ThemesPathPrefix + "1.21.111_the_copper_age.png" },
            { "ChaseTheSkies",                     ThemesPathPrefix + "1.21.90_chase_the_skies.jpg" },
            { "SpringToLife",                      ThemesPathPrefix + "1.21.70_spring_to_life.jpg" },
            { "TheGardenAwakens",                  ThemesPathPrefix + "1.21.50_the_garden_awakens.png" },
            { "TrickyTrials",                      ThemesPathPrefix + "1.21_tricky_trials.png" },
            { "TrailsAndTales",                    ThemesPathPrefix + "1.20_trails_and_tales.png" },
            { "TheWildUpdate",                     ThemesPathPrefix + "1.19_the_wild_update.jpg" },
            { "CavesAndCliffsPart2Update",         ThemesPathPrefix + "1.17_caves_and_cliffs_part_2.jpg" },
            { "CavesAndCliffsPart1Update",         ThemesPathPrefix + "1.17_caves_and_cliffs_part_1.jpg" },
            { "NetherUpdate",                      ThemesPathPrefix + "1.16_nether_update.jpg" },
            { "BuzzyBeesUpdate",                   ThemesPathPrefix + "1.15_buzzy_bees_update.jpg" },
            { "VillagePillageUpdate",              ThemesPathPrefix + "1.14_village_pillage_update.jpg" },
            { "UpdateAquatic",                     ThemesPathPrefix + "1.13_update_aquatic.jpg" },
            { "TechnicallyUpdated",                ThemesPathPrefix + "1.13_technically_updated_java.jpg" },
            { "WorldOfColorUpdate",                ThemesPathPrefix + "1.12_world_of_color_update_java.jpg" },
            { "ExplorationUpdate",                 ThemesPathPrefix + "1.11_exploration_update_java.jpg" },
            { "CombatUpdate",                      ThemesPathPrefix + "1.09_combat_update_java.jpg" },
            { "CatsAndPandasUpdate",               ThemesPathPrefix + "1.08_cats_and_pandas.jpg" },
            { "PocketEditionRelease",              ThemesPathPrefix + "1.0_pocket_edition.jpg" },
            { "JavaAndBedrockEditionStandard",     ThemesPathPrefix + "java_and_bedrock_edition_standard.jpg" },
            { "JavaAndBedrockEditionDeluxe",       ThemesPathPrefix + "java_and_bedrock_edition_deluxe.jpg" },
            { "BedrockAndJavaTogetherTechnoblade", ThemesPathPrefix + "bedrock_and_java_together_technoblade.jpg" },
            { "BedrockAndJavaTogether",            ThemesPathPrefix + "bedrock_and_java_together.jpg" },
            { "BedrockStandard",                   ThemesPathPrefix + "bedrock_standard.jpg" },
            { "BedrockMaster",                     ThemesPathPrefix + "bedrock_master.jpg" },
            { "EarlyLegacyConsole",                ThemesPathPrefix + "other_early_console_era.jpg" },
            { "MidLegacyConsole",                  ThemesPathPrefix + "other_mid_legacy_console.jpg" },
            { "LateLegacyConsole",                 ThemesPathPrefix + "other_late_legacy_console.jpg" },
            { "IndieDays",                         ThemesPathPrefix + "other_indie_days.jpg" },
            { "Dungeons",                          ThemesPathPrefix + "other_dungeons.jpg" },
            { "Original",                          ThemesPathPrefix + "original_image.jpg" }
        };

        public static List<string> INSTALLATION_PREFABED_ICONS_LIST_RUNTIME => INSTALLATIONS_PREFABED_ICONS_LIST.Select(ICON => INSTALLATIONS_PREFABED_ICONS_ROOT + ICON).ToList();
        public static string INSTALLATIONS_PREFABED_ICONS_ROOT => APP_RESOURCEPATH_SHORTPREFIX + @"resources/images/installation_icons/";
        public static List<string> INSTALLATIONS_PREFABED_ICONS_LIST => new List<string>()
        {
            "dirt.png",
            "grass_block.png",
            "grass_path.png",
            "custom_bedrockgrass.png",
            "snowy_grass_block.png",
            "podzol.png",
            "mycelium.png",
            "farmland.png",

            "stone.png",
            "andesite.png",
            "diorite.png",
            "granite.png",
            "cobblestone.png",
            "deepslate.png",
            "tuff.png",
            "bedrock.png",
            "obsidian.png",

            "clay.png",
            "bricks.png",

            "gravel.png",
            "sand.png",
            "sandstone.png",
            "red_sand.png",
            "red_sandstone.png",

            "glass.png",

            "snow_block.png",
            "ice.png",

            "terracotta.png",
            "light_blue_glazed_terracotta.png",
            "orange_glazed_terracotta.png",
            "white_glazed_terracotta.png",

            "water.png",

            "white_wool.png",

            "cake.png",
            "pumpkin.png",

            "netherrack.png",
            "soul_sand.png",
            "glowstone.png",
            "nether_wart_block.png",
            "warped_wart_block.png",
            "nether_bricks.png",

            "end_stone.png",

            "oak_leaves.png",
            "birch_leaves.png",
            "spruce_leaves.png",
            "jungle_leaves.png",
            "acacia_leaves.png",
            "dark_oak_leaves.png",
            "azalea_leaves.png",
            "flowering_azalea_leaves.png",
            "mangrove_leaves.png",

            "oak_log.png",
            "birch_log.png",
            "spruce_log.png",
            "jungle_log.png",
            "acacia_log.png",
            "dark_oak_log.png",
            "crimson_stem.png",
            "warped_stem.png",
            "mangrove_log.png",

            "oak_planks.png",
            "birch_planks.png",
            "spruce_planks.png",
            "jungle_planks.png",
            "acacia_planks.png",
            "dark_oak_planks.png",
            "crimson_planks.png",
            "warped_planks.png",
            "mangrove_planks.png",

            "block_of_coal.png",
            "copper_block.png",
            "exposed_copper_block.png",
            "weathered_copper_block.png",
            "oxidized_copper_block.png",
            "block_of_diamond.png",
            "block_of_emerald.png",
            "block_of_gold.png",
            "block_of_iron.png",
            "block_of_redstone.png",
            "block_of_netherite.png",
            "slime_block.png",
            "honey_block.png",

            "copper_ore.png",
            "deepslate_copper_ore.png",
            "coal_ore.png",
            "deepslate_coal_ore.png",
            "diamond_ore.png",
            "deepslate_diamond_ore.png",
            "emerald_ore.png",
            "deepslate_emerald_ore.png",
            "gold_ore.png",
            "deepslate_gold_ore.png",
            "iron_ore.png",
            "deepslate_iron_ore.png",
            "lapis_lazuli_ore.png",
            "deepslate_lapis_lazuli_ore.png",
            "redstone_ore.png",
            "deepslate_redstone_ore.png",
            "nether_quartz_ore.png",
            "ancient_debris.png",

            "creeper_head.png",
            "skeleton_skull.png",

            "crafting_table.png",
            "furnace.png",
            "lit_furnace.png",
            "chest.png",
            "ender_chest.png",
            "xmas_chest.png",
            "bookshelf.png",
            "tnt.png",
            "enchanting_table.png",
            "lectern_book.png",
            "nether_reactor_core.png",
            "nether_reactor_core_initialized.png",
            "nether_reactor_core_finished.png",
            "observer.png",
            "piston.png",
            "sticky_piston.png",
            "command_block.png",
            "custom_package.png",
        };

        public static string CurrentArchitecture
        {
            get
            {
                return RuntimeInformation.OSArchitecture switch
                {
                    Architecture.Arm64 => "arm",
                    Architecture.X86 => "x86",
                    Architecture.X64 => "x64",
                    _ => "null"
                };
            }
        }

        public static RemovalOptions PackageRemovalOptions
        {
            get
            {
                RemovalOptions options = new RemovalOptions();
                // Use default removal (current user only) instead of RemoveForAllUsers to avoid requiring admin privileges
                // RemovalOptions.RemoveForAllUsers requires administrator privileges
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
