using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using BedrockLauncher.Core.Pages.Common;
using BedrockLauncher.Core.Methods;

namespace BedrockLauncher.Dungeons.Methods
{
    public static class GameManager
    {
        #region Paths (Windows Store)

        public static string GetDungeonsContentFolder_Store()
        {
            return string.Empty;
        }
        public static string GetDungeonsExeFolder_Store()
        {
            return string.Empty;
        }
        public static string GetDungeonsLaunchCode_Store()
        {
            return string.Empty;
        }
        public static string GetDungeonsLocalFolder_Store()
        {
            string NormalDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dungeons");
            string StoreDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Microsoft.Lovika_8wekyb3d8bbwe", "LocalCache", "Local", "Dungeons");
            if (Directory.Exists(NormalDirectory)) return NormalDirectory;
            else return StoreDirectory;
        }

        public static string GetOurDungeonsModFolder_Store()
        {
            return string.Empty;
        }

        public static string GetDungeonsSaveDataFolder_Store()
        {
            return GetDungeonsLocalFolder_Store();
        }

        #endregion

        #region Paths (Windows 7/10 Launcher)

        public static string GetDungeonsContentFolder_Launcher()
        {
            string installFolder = GetDungeonsExeFolder_Launcher();
            string pathToFolder = @"Dungeons\Content";
            return Path.Combine(installFolder, pathToFolder);
        }
        public static string GetDungeonsExeFolder_Launcher()
        {
            string defaultFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Mojang\products​");
            string installFolder = Properties.DungeonSettings.Default.InstallLocation;
            string pathToFolder = @"dungeons\dungeons";

            if (installFolder == string.Empty) return Path.Combine(defaultFolder, pathToFolder);
            else return Path.Combine(installFolder, pathToFolder);
        }
        public static string GetDungeonsLaunchCode_Launcher()
        {
            if (Dungeons.Properties.DungeonSettings.Default.IsWindowsStoreVariant) return string.Empty;

            string installFolder = GetDungeonsExeFolder_Launcher();
            string pathToExe = @"Dungeons.exe";
            return Path.Combine(installFolder, pathToExe);
        }
        public static string GetDungeonsLocalFolder_Launcher()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dungeons");
        }
        public static string GetOurDungeonsModFolder_Launcher()
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
        public static string GetDungeonsSaveDataFolder_Launcher()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Saved Games\Mojang Studios\Dungeons");
        }

        #endregion

        #region Paths (Global)

        public static string GetDungeonsLaunchCode()
        {
            if (Properties.DungeonSettings.Default.IsWindowsStoreVariant) return GetDungeonsContentFolder_Store();
            else return GetDungeonsContentFolder_Launcher();
        }

        public static string GetDungeonsLocalFolder()
        {
            if (Properties.DungeonSettings.Default.IsWindowsStoreVariant) return GetDungeonsLocalFolder_Store();
            else return GetDungeonsLocalFolder_Launcher();
        }

        public static string GetDungeonsContentFolder()
        {
            if (Properties.DungeonSettings.Default.IsWindowsStoreVariant) return GetDungeonsContentFolder_Store();
            else return GetDungeonsContentFolder_Launcher();
        }

        public static string GetDungeonsSaveDataFolder()
        {
            if (Properties.DungeonSettings.Default.IsWindowsStoreVariant) return GetDungeonsSaveDataFolder_Store();
            else return GetDungeonsSaveDataFolder_Launcher();
        }

        public static string GetOurDungeonsModFolder()
        {
            if (Properties.DungeonSettings.Default.IsWindowsStoreVariant) return GetOurDungeonsModFolder_Store();
            else return GetOurDungeonsModFolder_Launcher();
        }

        #endregion

        #region Actions (Windows Store)

        public static void InstallModManagement_Store()
        {
            string powershell_Command = "Set-ExecutionPolicy -Scope Process Bypass;" +
                "mkdir C:\\mcdtemp;" +
                "Invoke-WebRequest -Uri \"https://cdn.discordapp.com/attachments/715508829458006077/790882159187591168/installscript.ps1\" -OutFile C:\\mcdtemp\\winstore.ps1;" +
                "C:\\mcdtemp\\winstore.ps1;" +
                "read-host \"Press ENTER to continue...\"";
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"powershell.exe";
            startInfo.Arguments = powershell_Command;
            Process.Start(startInfo);
        }

        public static void RepairDungeons_Store()
        {
            Process.Start("ms-windows-store://DownloadsAndUpdates/");
        }

        #endregion

        #region Actions (Windows 7/10 Launcher)

        public static void InstallModManagement_Launcher()
        {
            try
            {
                string ModsFolder = Path.Combine(GetDungeonsContentFolder(), "Paks", "~mods");
                string OurFolder = GetOurDungeonsModFolder();
                Directory.CreateDirectory(OurFolder);
                BedrockLauncher.Core.Methods.SymLinkHelper.CreateSymbolicLink(ModsFolder, OurFolder, SymLinkHelper.SymbolicLinkType.Directory);
            }
            catch (Exception ex)
            {
                ErrorScreenShow.exceptionmsg(ex);
            }
        }

        public static void RepairDungeons_Launcher()
        {
            try
            {
                // Trying to find and open Java launcher shortcut
                string JavaPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + @"\Programs\Minecraft Launcher\Minecraft Launcher";
                Process.Start(JavaPath);
            }
            catch
            {
                ErrorScreenShow.errormsg("Error_CantFindJavaLauncher_Title", "Error_CantFindJavaLauncher");
            }
        }

        public static void RemoveModManagement_Launcher()
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

        #endregion

        #region Actions (Global)

        public static void LaunchDungeons()
        {
            try
            {
                string path = GetDungeonsLaunchCode();
                Process.Start(path);
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
            if (Properties.DungeonSettings.Default.IsWindowsStoreVariant) RepairDungeons_Store();
            else RepairDungeons_Launcher();
        }

        public static void InstallModManagement()
        {
            if (Properties.DungeonSettings.Default.IsWindowsStoreVariant) InstallModManagement_Store();
            else InstallModManagement_Launcher();
        }

        public static void RemoveModManagement()
        {
            if (Properties.DungeonSettings.Default.IsWindowsStoreVariant) InstallModManagement_Store();
            else InstallModManagement_Launcher();
        }

        #endregion
    }
}
