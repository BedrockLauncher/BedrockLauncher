using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using BedrockLauncher.UI.Pages.Common;

namespace BedrockLauncher.Dungeons.Methods
{
    public static class GameManager
    {
        
        public static string GetOurDungeonsPath()
        {
            return Properties.DungeonSettings.Default.InstallLocation;
        }
        public static string GetDungeonsLocalFolder()
        {
            switch (Properties.DungeonSettings.Default.GameVariant)
            {
                case Enums.GameVariant.Launcher:
                    return Launcher();
                case Enums.GameVariant.Store:
                    return Store();
                case Enums.GameVariant.Steam:
                    return Steam();
                default:
                    throw new NotImplementedException();
            }

            string Launcher()
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dungeons");
            }

            string Store()
            {
                string NormalDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dungeons");
                string StoreDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Microsoft.Lovika_8wekyb3d8bbwe", "LocalCache", "Local", "Dungeons");
                if (Directory.Exists(NormalDirectory)) return NormalDirectory;
                else return StoreDirectory;
            }

            string Steam()
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dungeons");
            }
        }
        public static string GetDungeonsContentFolder()
        {
            string installFolder = GetOurDungeonsPath();
            string pathToFolder = @"Dungeons\Content";
            return Path.Combine(installFolder, pathToFolder);
        }
        public static string GetDungeonsSaveDataFolder()
        {
            switch (Properties.DungeonSettings.Default.GameVariant)
            {
                case Enums.GameVariant.Launcher:
                    return Launcher();
                case Enums.GameVariant.Store:
                    return WindowsStore();
                case Enums.GameVariant.Steam:
                    return Steam();
                default:
                    throw new NotImplementedException();
            }

            string Launcher()
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games", "Mojang Studios", "Dungeons");
            }

            string WindowsStore()
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Microsoft.Lovika_8wekyb3d8bbwe", "LocalCache", "Local", "Dungeons");
            }

            string Steam()
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games", "Mojang Studios", "Dungeons");
            }
        }
        public static string GetOurDungeonsModFolder()
        {
            if (Properties.DungeonSettings.Default.ModsLocation == string.Empty)
            {
                string ExecutableLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string ExecutableDirectory = Path.GetDirectoryName(ExecutableLocation);
                string path = Path.Combine(ExecutableDirectory, "data");
                Directory.CreateDirectory(path);
                return Path.Combine(path, "mods");
            }
            else return Properties.DungeonSettings.Default.ModsLocation;
        }


        public static void InstallStorePatch(bool updateMode)
        {
            string scriptURL = "https://dokucraft.co.uk/stash/resources/docs/DungeonsInstallScript.ps1";

            string args = updateMode ? " -update" : string.Empty;

            string powershell_Command = "Set-ExecutionPolicy -Scope Process Bypass;" +
                "mkdir C:\\mcdtemp;" +
                "Invoke-WebRequest -Uri \"" + scriptURL + "\" -OutFile C:\\mcdtemp\\winstore.ps1;" +
                $"C:\\mcdtemp\\winstore.ps1{args};" +
                "read-host \"Press ENTER to continue...\"";
            RunPowershell(powershell_Command);
        }
        public static void LaunchDungeons()
        {
            try
            {
                switch (Properties.DungeonSettings.Default.GameVariant)
                {
                    case Enums.GameVariant.Launcher: Launcher(); break;
                    case Enums.GameVariant.Store: Store(); break;
                    case Enums.GameVariant.Steam: Steam(); break;
                    default: throw new NotImplementedException();
                }

                void Store() => RunPowershell("explorer.exe shell:AppsFolder/$(Get-AppxPackage Lovika | Expand-Property -Property PackageName)!Game");
                void Steam() => Process.Start("steam://rungameid//1672970");
                void Launcher() => Process.Start(Path.Combine(GetOurDungeonsPath(), "Dungeons.exe"));
            }
            catch (Exception ex)
            {
                ErrorScreenShow.exceptionmsg(ex);
            }

        }
        public static void OpenConfigFolder()
        {
            try
            {
                string path = Path.Combine(GetDungeonsLocalFolder(), @"Saved\Config\WindowsNoEditor");
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                ErrorScreenShow.exceptionmsg(ex);
            }

        }
        public static void OpenContentFolder()
        {
            try
            {
                string path = GetDungeonsContentFolder();
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                ErrorScreenShow.exceptionmsg(ex);
            }

        }
        public static void OpenSaveDataFolder()
        {
            try
            {
                string path = Path.Combine(GetDungeonsSaveDataFolder());
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                ErrorScreenShow.exceptionmsg(ex);
            }

        }
        public static void RepairDungeons()
        {
            switch (Properties.DungeonSettings.Default.GameVariant)
            {
                case Enums.GameVariant.Launcher:
                    Launcher();
                    break;
                case Enums.GameVariant.Store:
                    Store();
                    break;
                case Enums.GameVariant.Steam:
                    Steam();
                    break;
                default:
                    throw new NotImplementedException();
            }

            void Store()
            {
                Process.Start("ms-windows-store://DownloadsAndUpdates/");
            }

            void Launcher()
            {
                try
                {
                    // Trying to find and open Java launcher shortcut
                    string DungeonsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + @"\Programs\Minecraft Launcher\Minecraft Launcher";
                    Process.Start(DungeonsPath);
                }
                catch
                {
                    ErrorScreenShow.errormsg("Error_CantFindJavaLauncher_Title", "Error_CantFindJavaLauncher");
                }
            }

            void Steam()
            {
                Process.Start("steam://open/games");
            }
        }
        public static void InstallModManagement()
        {
            try
            {
                string ModsFolder = Path.Combine(GetDungeonsContentFolder(), "Paks", "~mods");
                string OurFolder = GetOurDungeonsModFolder();
                Directory.CreateDirectory(OurFolder);
                JemExtensions.SymLinkHelper.CreateSymbolicLink(ModsFolder, OurFolder, JemExtensions.SymLinkHelper.SymbolicLinkType.Directory);
            }
            catch (Exception ex)
            {
                ErrorScreenShow.exceptionmsg(ex);
            }
        }
        public static void RemoveModManagement()
        {
            try
            {
                string ModsFolder = Path.Combine(GetDungeonsContentFolder(), "Paks", "~mods");
                Directory.Delete(ModsFolder, true);
            }
            catch (Exception ex)
            {
                ErrorScreenShow.exceptionmsg(ex);
            }
        }


        private static void RunPowershell(string commands)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"powershell.exe";
            startInfo.Arguments = commands;
            Process.Start(startInfo);
        }
    }
}
