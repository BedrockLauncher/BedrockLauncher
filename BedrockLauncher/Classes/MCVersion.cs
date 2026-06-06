using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BedrockLauncher.Classes;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.UpdateProcessor.Extensions;
using BedrockLauncher.UpdateProcessor.Interfaces;
using BedrockLauncher.ViewModels;
using Newtonsoft.Json;
using PostSharp.Patterns.Model;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace BedrockLauncher.Classes
{

    public class MCVersion
    {
        private static readonly object InstallProbeCacheLock = new object();
        private static readonly TimeSpan InstallProbeCacheLifetime = TimeSpan.FromSeconds(12);
        private static readonly Dictionary<string, (DateTime CheckedAt, bool IsInstalled)> InstallProbeCache = new Dictionary<string, (DateTime CheckedAt, bool IsInstalled)>();
        private static readonly string[] VersionIconFileNames =
        {
            "Grass_Block.png",
            "Copper_Block.png",
            "Deepslate_Diamond_Ore.png",
            "Azalea_Leaves.png",
            "Ancient_Debris.png",
            "Observer.png",
            "Crafting_Table.png",
            "Ender_Chest.png",
            "Tuff.png",
            "Flowering_Azalea_Leaves.png",
            "Honey_Block.png",
            "Light_Blue_Glazed_Terracotta.png",
            "Nether_Reactor_Core.png",
            "Oxidized_Copper_Block.png",
            "Exposed_Copper_Block.png",
            "Weathered_Copper_Block.png",
            "Redstone_Ore.png",
            "Lapis_Lazuli_Ore.png",
            "Emerald_Ore.png",
            "Gold_Ore.png",
            "Iron_Ore.png",
            "Diamond_Ore.png"
        };
        private static readonly string[] RequiredGdkRuntimeDlls =
        {
            "vcruntime140_1.dll",
            "concrt140_app.dll",
            "msvcp140_app.dll",
            "vcruntime140_app.dll"
        };

        public MCVersion(string uuid, string pkgId, string name, VersionType type, string architecture)
        {
            this.UUID = uuid;
            this.PackageID = pkgId;
            this.Name = name;
            this.Type = type;
            this.Architecture = architecture;
            this.PackageType = IsGdkVersion(name) ? PackageType.GDK : PackageType.UWP;
        }

        public MCVersion(string name)
        {
            this.Name = name;
        }

        public string UUID { get; set; }
        public string PackageID { get; set; }
        public string Name { get; set; }
        public string Architecture { get; set; }
        public string CustomName { get; set; }
        public VersionType Type { get; set; }
        public PackageType PackageType { get; private set; }
        public bool IsBeta
        {
            get => Type == VersionType.Beta;
        }
        public bool IsRelease
        {
            get => Type == VersionType.Release;
        }
        public bool IsPreview
        {
            get => Type == VersionType.Preview;
        }
        public bool IsCustom
        {
            get => UUID != PackageID;
        }
        public bool IsInstalled
        {
            get
            {
                Depends.On(GameDirectory, Type, Name, RequireSizeRecalculation);
                if (PackageType == PackageType.GDK && LooksLikeConcreteVersion(Name))
                    return IsInstalledInLauncher;

                return IsInstalledInLauncher || IsInstalledExternally;
            }
        }
        public bool IsInstalledInLauncher
        {
            get
            {
                Depends.On(GameDirectory);
                TryMigrateLegacyVersionFolder();
                return IsCompleteLauncherInstall();
            }
        }
        public bool IsInstalledExternally
        {
            get
            {
                Depends.On(Type, Name);
                return IsDetectedInExternalMinecraftFiles();
            }
        }
        public bool IsSelectedForPlay
        {
            get
            {
                Depends.On(MainDataModel.Default.Config.CurrentInstallationUUID);
                return MainDataModel.Default.IsVersionSelectedForPlay(this);
            }
        }

        public string GameDirectory
        {
            get
            {
                Depends.On(UUID, Name, Type);
                return PreferredGameDirectory;
            }
        }
        public string VersionFolderName
        {
            get
            {
                Depends.On(UUID, Name, Type);
                return GetSafeVersionFolderName();
            }
        }
        private string PreferredGameDirectory
        {
            get
            {
                string configuredDirectory = Path.GetFullPath(Path.Combine(MainDataModel.Default.FilePaths.VersionsFolder, VersionFolderName));
                string roamingDirectory = Path.GetFullPath(Path.Combine(GetRoamingVersionsFolder(), VersionFolderName));

                if (!string.Equals(configuredDirectory, roamingDirectory, StringComparison.OrdinalIgnoreCase) &&
                    Directory.Exists(roamingDirectory))
                {
                    return roamingDirectory;
                }

                return configuredDirectory;
            }
        }
        private string LegacyGameDirectory
        {
            get
            {
                return Path.GetFullPath(Path.Combine(MainDataModel.Default.FilePaths.VersionsFolder, UUID ?? string.Empty));
            }
        }
        public string DisplayName
        {
            get
            {
                Depends.On(Type, Name, Architecture);

                string _TypeSuffix = string.Empty;
                string _ArchSuffix = string.Empty;


                if (!VersionDbExtensions.DoesVerionArchMatch(Constants.CurrentArchitecture, Architecture))
                    _ArchSuffix = $" [{Architecture}]";

                switch (Type)
                {
                    case VersionType.Beta:
                        _TypeSuffix = string.Format(" ({0})", "Beta"); //TODO: Localize String
                        break;
                    case VersionType.Preview:
                        _TypeSuffix = string.Format(" ({0})", "Preview"); //TODO: Localize String
                        break;
                }

                return (IsCustom ? CustomName : Name) + _TypeSuffix + _ArchSuffix;
            }
        }
        public string InstallationSize
        {
            get
            {
                Depends.On(RequireSizeRecalculation);
                if (Constants.Debugging.CalculateVersionSizes) Task.Run(GetInstallSize);
                else RequireSizeRecalculation = false;
                return StoredInstallationSize;
            }
        }
        public string VersionStatusText
        {
            get
            {
                Depends.On(IsSelectedForPlay, IsInstalled, IsInstalledInLauncher, InstallationSize);
                if (IsSelectedForPlay && IsInstalled) return "Selected and ready to play";
                if (IsInstalledInLauncher) return InstallationSize;
                if (IsInstalledExternally && !(PackageType == PackageType.GDK && LooksLikeConcreteVersion(Name))) return "Installed in Minecraft";
                return "Ready to install";
            }
        }
        public string VersionActionText
        {
            get
            {
                Depends.On(IsSelectedForPlay, IsInstalled);
                return IsInstalled ? "Play" : "Install";
            }
        }
        public string IconPath
        {
            get
            {
                Depends.On(IsCustom, Type, Name);
                return Constants.INSTALLATIONS_PREFABED_ICONS_ROOT + InstallationIconFileName;
            }
        }
        public string InstallationIconFileName
        {
            get
            {
                Depends.On(IsCustom, Type, Name);
                return GetVersionIconFileName(Name, Type, IsCustom);
            }
        }
        public string ManifestPath
        {
            get
            {
                Depends.On(GameDirectory);
                return FindFileIgnoringCase(GameDirectory, MCVersionExtensions.MainifestFileName)
                    ?? Path.Combine(GameDirectory, MCVersionExtensions.MainifestFileName);
            }
        }
        public string IdentificationPath
        {
            get
            {
                Depends.On(GameDirectory);
                return FindFileIgnoringCase(GameDirectory, MCVersionExtensions.IdentificationFilename)
                    ?? Path.Combine(GameDirectory, MCVersionExtensions.IdentificationFilename);
            }
        }

        #region Size Calcualtion

        [JsonIgnore] private string StoredInstallationSize { get; set; } = "...";
        [JsonIgnore] private bool RequireSizeRecalculation { get; set; } = false;
        [JsonIgnore] private static bool Internal_SizeCalcInProgress { get; set; } = false;


        private async Task GetInstallSize()
        {
            while (Internal_SizeCalcInProgress) await Task.Delay(500);

            await Task.Run(() =>
            {
                if (!RequireSizeRecalculation) return;

                if (IsInstalled)
                {
                    Internal_SizeCalcInProgress = true;
                    var dirSize = GetDirectorySize(Path.GetFullPath(GameDirectory));
                    string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                    int order = 0;
                    double len = dirSize;
                    while (len >= 1024 && order < sizes.Length - 1)
                    {
                        order++;
                        len = len / 1024;
                    }
                    StoredInstallationSize = String.Format("{0:0.##} {1}", len, sizes[order]);
                    RequireSizeRecalculation = false;
                    Internal_SizeCalcInProgress = false;
                }
                else
                {
                    StoredInstallationSize = "...";
                    RequireSizeRecalculation = false;
                }
            });

            ulong GetDirectorySize(string dir)
            {
                dynamic fso = Activator.CreateInstance(System.Type.GetTypeFromProgID("Scripting.FileSystemObject"));
                dynamic fldr = fso.GetFolder(dir);
                return (ulong)fldr.size;
            }
        }

        #endregion

        #region Methods

        public string GetPackageNameFromMainifest()
        {
            var (Name, Version, ProcessorArchitecture) = MCVersionExtensions.GetCommonPackageValues(ManifestPath);
            return String.Join("_", Name, Version, ProcessorArchitecture);

        }
        public void OpenDirectory()
        {
            string Directory = Path.GetFullPath(GameDirectory);
            if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);
            Process.Start("explorer.exe", Directory);
        }
        public void UpdateFolderSize()
        {
            RequireSizeRecalculation = true;
            ClearInstallProbeCache();
        }
        public int Compare(MCVersion y)
        {
            try
            {
                var a = Version.Parse(this.Name);
                var b = Version.Parse(y.Name);
                return b.CompareTo(a);
            }
            catch
            {
                return y.Name.CompareTo(this.Name);
            }

        }

        public static string GetVersionIconFileName(string versionName, VersionType type, bool isCustom = false)
        {
            if (isCustom) return "Custom_Package.png";
            if (string.IsNullOrWhiteSpace(versionName)) return "Bedrock.png";

            int index = StableIconHash($"{type}:{versionName}") % VersionIconFileNames.Length;
            return VersionIconFileNames[index];
        }

        private static int StableIconHash(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in text.ToUpperInvariant())
                {
                    hash ^= c;
                    hash *= 16777619;
                }

                return (int)(hash & 0x7fffffff);
            }
        }

        private static bool IsGdkVersion(string versionName)
        {
            if (!MinecraftVersion.TryParse(versionName, out MinecraftVersion version))
                return false;
            if (!MinecraftVersion.TryParse(Constants.FIRST_GDK_VERSION, out MinecraftVersion minimumGdkVersion))
                return false;

            return version.CompareTo(minimumGdkVersion) >= 0;
        }

        private bool IsCompleteLauncherInstall()
        {
            if (!File.Exists(ManifestPath))
                return false;

            if (PackageType != PackageType.GDK)
                return true;

            string gameExe = Path.Combine(GameDirectory, "Minecraft.Windows.exe");
            string gameConfig = Path.Combine(GameDirectory, "MicrosoftGame.Config");
            string dataDirectory = Path.Combine(GameDirectory, "data");

            return File.Exists(gameExe) &&
                File.Exists(gameConfig) &&
                Directory.Exists(dataDirectory) &&
                RequiredGdkRuntimeDlls.All(fileName => File.Exists(Path.Combine(GameDirectory, fileName)));
        }

        private string GetSafeVersionFolderName()
        {
            string preferredName = Version.TryParse(Name, out _)
                ? Name
                : UUID;

            if (Type == VersionType.Preview && Version.TryParse(Name, out _))
                preferredName = "preview-" + preferredName;
            else if (Type == VersionType.Beta && Version.TryParse(Name, out _))
                preferredName = "beta-" + preferredName;

            if (string.IsNullOrWhiteSpace(preferredName))
                preferredName = UUID;

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string safeName = new string(preferredName
                .Where(ch => !invalidChars.Contains(ch))
                .ToArray())
                .Trim();

            return string.IsNullOrWhiteSpace(safeName) ? UUID : safeName;
        }

        private void TryMigrateLegacyVersionFolder()
        {
            try
            {
                string preferredDirectory = PreferredGameDirectory;
                string legacyDirectory = LegacyGameDirectory;

                if (string.Equals(preferredDirectory, legacyDirectory, StringComparison.OrdinalIgnoreCase))
                    return;
                if (Directory.Exists(preferredDirectory))
                    return;
                if (string.IsNullOrWhiteSpace(FindFileIgnoringCase(legacyDirectory, MCVersionExtensions.MainifestFileName)))
                    return;

                Directory.CreateDirectory(Path.GetDirectoryName(preferredDirectory));
                Directory.Move(legacyDirectory, preferredDirectory);
                ClearInstallProbeCache();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Unable to migrate legacy version folder for {Name}: {ex}");
            }
        }

        private static string FindFileIgnoringCase(string directory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return null;

            return Directory.EnumerateFiles(directory)
                .FirstOrDefault(path => string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetRoamingVersionsFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraft_bedrock",
                "versions");
        }

        #endregion

        #region External Minecraft Detection

        public static void ClearInstallProbeCache()
        {
            lock (InstallProbeCacheLock)
            {
                InstallProbeCache.Clear();
            }
        }

        private bool IsDetectedInExternalMinecraftFiles()
        {
            if (!LooksLikeConcreteVersion(Name)) return false;

            string cacheKey = $"{Type}|{Name}|{Architecture}";
            lock (InstallProbeCacheLock)
            {
                if (InstallProbeCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow - cached.CheckedAt < InstallProbeCacheLifetime)
                    return cached.IsInstalled;
            }

            bool isInstalled = IsRegisteredMinecraftPackageInstalled() || IsKnownMinecraftDirectoryInstalled();

            lock (InstallProbeCacheLock)
            {
                InstallProbeCache[cacheKey] = (DateTime.UtcNow, isInstalled);
            }

            return isInstalled;
        }

        private bool IsRegisteredMinecraftPackageInstalled()
        {
            try
            {
                var packageManager = new PackageManager();
                foreach (var package in packageManager.FindPackagesForUser(string.Empty, Constants.GetPackageFamily(Type)))
                {
                    string packageLocation = GetPackageInstalledLocation(package);
                    if (!string.IsNullOrWhiteSpace(packageLocation) && !IsOfficialMinecraftPackageLocation(packageLocation))
                    {
                        Trace.WriteLine("Ignoring loose external Minecraft registration for install detection.");
                        continue;
                    }

                    if (IsSameVersion(FormatPackageVersion(package.Id.Version), Name)) return true;

                    try
                    {
                        string manifestPath = Path.Combine(packageLocation, MCVersionExtensions.MainifestFileName);
                        if (IsMatchingMinecraftManifest(manifestPath)) return true;
                    }
                    catch
                    {
                        // Some Store installs hide the location; the package identity version above is enough when available.
                    }
                }
            }
            catch
            {
                // Package APIs can fail on restricted systems; filesystem probing below is the fallback.
            }

            return false;
        }

        private static string GetPackageInstalledLocation(Package package)
        {
            try
            {
                return package?.InstalledLocation?.Path ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsOfficialMinecraftPackageLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return false;

            string normalized = Path.GetFullPath(location)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return normalized.IndexOf(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf(@"\XboxGames\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf(@"\ModifiableWindowsApps\", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsKnownMinecraftDirectoryInstalled()
        {
            foreach (string directory in GetKnownMinecraftDirectories())
            {
                string manifestPath = Path.Combine(directory, MCVersionExtensions.MainifestFileName);
                if (IsMatchingMinecraftManifest(manifestPath)) return true;
            }

            return false;
        }

        private IEnumerable<string> GetKnownMinecraftDirectories()
        {
            string releaseFolderName = Type == VersionType.Preview ? "Minecraft Preview" : "Minecraft for Windows";
            string packagePrefix = Type == VersionType.Preview ? "Microsoft.MinecraftWindowsBeta_" : "Microsoft.MinecraftUWP_";

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;

                string root = drive.RootDirectory.FullName;
                yield return Path.Combine(root, "XboxGames", releaseFolderName, "Content");
                yield return Path.Combine(root, "Program Files", "ModifiableWindowsApps", releaseFolderName);

                string windowsApps = Path.Combine(root, "Program Files", "WindowsApps");
                foreach (string packageDir in EnumerateDirectoriesSafe(windowsApps, packagePrefix + "*"))
                    yield return packageDir;
            }

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                foreach (string packageDir in EnumerateDirectoriesSafe(Path.Combine(programFiles, "WindowsApps"), packagePrefix + "*"))
                    yield return packageDir;
            }
        }

        private bool IsMatchingMinecraftManifest(string manifestPath)
        {
            try
            {
                if (!File.Exists(manifestPath)) return false;

                var (name, version, processorArchitecture) = MCVersionExtensions.GetCommonPackageValues(manifestPath);
                string expectedName = Type == VersionType.Preview ? "Microsoft.MinecraftWindowsBeta" : "Microsoft.MinecraftUWP";
                if (!string.Equals(name, expectedName, StringComparison.OrdinalIgnoreCase)) return false;
                if (!VersionDbExtensions.DoesVerionArchMatch(Constants.CurrentArchitecture, processorArchitecture)) return false;

                return IsSameVersion(version, Name);
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<string> EnumerateDirectoriesSafe(string path, string pattern)
        {
            try
            {
                if (!Directory.Exists(path)) return Enumerable.Empty<string>();
                return Directory.EnumerateDirectories(path, pattern).ToList();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        private static string FormatPackageVersion(PackageVersion version)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private static bool LooksLikeConcreteVersion(string version)
        {
            return Version.TryParse(version, out _);
        }

        private static bool IsSameVersion(string installedVersion, string expectedVersion)
        {
            if (!Version.TryParse(installedVersion, out Version installed)) return false;
            if (!Version.TryParse(expectedVersion, out Version expected)) return false;

            if (installed.Major == expected.Major &&
                installed.Minor == expected.Minor &&
                installed.Build == expected.Build &&
                installed.Revision == expected.Revision)
            {
                return true;
            }

            if (IsSameEncodedGdkVersion(installed, expected)) return true;

            return false;
        }

        private static bool IsSameEncodedGdkVersion(Version installed, Version expected)
        {
            if (installed.Major != 1 || installed.Build < 0) return false;

            // Store/GDK packages encode the launcher-visible patch into the build
            // number. Examples:
            //   1.21.13004.0 == 1.21.130.4
            //   1.26.2101.0  == 26.21 / 26.21.1
            if (expected.Major == 1)
            {
                if (installed.Minor != expected.Minor || expected.Build < 0) return false;

                int expectedRevision = Math.Max(expected.Revision, 0);
                int encodedBuild = expected.Build * 100 + expectedRevision;
                if (installed.Build == encodedBuild) return true;

                return expected.Revision < 0 && installed.Build / 100 == expected.Build;
            }

            if (installed.Minor != expected.Major || expected.Minor < 0) return false;

            // Store/GDK packages may use a direct package version:
            //   1.26.0.2  == 26.0.2
            //   1.26.10.4 == 26.10.4
            if (installed.Build == expected.Minor)
            {
                if (expected.Build < 0) return true;
                return installed.Revision == expected.Build;
            }

            int installedFeature = installed.Build / 100;
            int installedPatch = installed.Build % 100;
            if (installedFeature != expected.Minor) return false;

            if (expected.Build < 0) return true;
            return installedPatch == expected.Build;
        }

        #endregion
    }

    public static class MCVersionExtensions
    {
        public const string IdentificationFilename = "PackageID.txt";
        public const string MainifestFileName = "AppxManifest.xml";

        static Tuple<string, string, string> GetCommonPackageValues_CommonFunctionality(string manifestXml)
        {
            try
            {
                XDocument XMLDoc = XDocument.Parse(manifestXml);
                var Descendants = XMLDoc.Descendants();
                XElement Identity = Descendants.Where(x => x.Name.LocalName == "Identity").FirstOrDefault();
                string Name = Identity.Attribute("Name").Value;
                string Version = Identity.Attribute("Version").Value;
                string ProcessorArchitecture = Identity.Attribute("ProcessorArchitecture").Value;

                return new Tuple<string, string, string>(Name, Version, ProcessorArchitecture);
            }
            catch
            {
                return new Tuple<string, string, string>("???", "???", "???");
            }
        }
        public static async Task<Tuple<string, string, string>> GetCommonPackageValuesAsync(string manifestPath)
        {
            string manifestXml = await File.ReadAllTextAsync(manifestPath);
            return MCVersionExtensions.GetCommonPackageValues_CommonFunctionality(manifestXml);

        }
        public static Tuple<string,string,string> GetCommonPackageValues(string manifestPath)
        {
            string manifestXml = File.ReadAllText(manifestPath);
            return MCVersionExtensions.GetCommonPackageValues_CommonFunctionality(manifestXml);
        }
    }
}
