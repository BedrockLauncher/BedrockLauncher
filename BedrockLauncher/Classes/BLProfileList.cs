using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using JemExtensions;
using Newtonsoft.Json;
using BedrockLauncher.Enums;
using PostSharp.Patterns.Model;
using System.ComponentModel;
using System.Xml.Linq;
using BedrockLauncher.ViewModels;
using BedrockLauncher.Handlers;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Enums;
using Windows.Networking.NetworkOperators;

namespace BedrockLauncher.Classes
{

    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]    //224 Lines
    public class BLProfileList : JemExtensions.WPF.NotifyPropertyChangedBase
    {
        private const string SystemMinecraftInstallationPrefix = "system_minecraft:";
        private const string CatalogGeneratedInstallationPrefix = "catalog_version:";
        private const string VersionsPageSelectedInstallationPrefix = "versions_page_selected:";
        private static readonly string[] RequiredGdkRuntimeDlls =
        {
            "vcruntime140_1.dll",
            "concrt140_app.dll",
            "msvcp140_app.dll",
            "vcruntime140_app.dll"
        };
        private DateTime lastPlayableSelectionSyncUtc = DateTime.MinValue;
        public int Version = 2;



        public Dictionary<string, BLProfile> profiles { get; set; } = new Dictionary<string, BLProfile>();

        #region Runtime Values
        [JsonIgnore]
        public string FilePath { get; private set; } = string.Empty;

        [JsonIgnore]
        public string CurrentInstallationUUID
        {
            get
            {
                Depends.On(Properties.LauncherSettings.Default.CurrentInstallationUUID);
                return Properties.LauncherSettings.Default.CurrentInstallationUUID;
            }
            set
            {
                Properties.LauncherSettings.Default.CurrentInstallationUUID = value;
                Properties.LauncherSettings.Default.Save();
            }
        }
        [JsonIgnore] 
        public BLProfile CurrentProfile
        {
            get
            {
                Depends.On(Properties.LauncherSettings.Default.CurrentProfileUUID);
                if (profiles.ContainsKey(Properties.LauncherSettings.Default.CurrentProfileUUID)) return profiles[Properties.LauncherSettings.Default.CurrentProfileUUID];
                else return null;
            }
            set
            {
                if (profiles.ContainsKey(Properties.LauncherSettings.Default.CurrentProfileUUID)) profiles[Properties.LauncherSettings.Default.CurrentProfileUUID] = value;
            }
        }

        [JsonIgnore]
        public string CurrentProfileImagePath
        {
            get
            {
                Depends.On(Properties.LauncherSettings.Default.CurrentProfileUUID);
                if (profiles.ContainsKey(Properties.LauncherSettings.Default.CurrentProfileUUID)) return profiles[Properties.LauncherSettings.Default.CurrentProfileUUID].ImagePath;
                return string.Empty;
            }
        }
        [JsonIgnore] 
        public BLInstallation CurrentInstallation
        {
            get
            {
                Depends.On(CurrentInstallationUUID, CurrentInstallations);
                if (CurrentProfile == null) return null;
                else if (CurrentInstallations == null) return null;
                else if (CurrentInstallations.Any(x => x.InstallationUUID == CurrentInstallationUUID))
                    return CurrentInstallations.First(x => x.InstallationUUID == CurrentInstallationUUID);
                else return null;
            }
            set
            {
                if (CurrentProfile == null) return;
                else if (CurrentInstallations == null) return;
                else if (CurrentInstallations.Any(x => x.InstallationUUID == CurrentInstallationUUID))
                {
                    int index = CurrentInstallations.FindIndex(x => x.InstallationUUID == CurrentInstallationUUID);
                    CurrentInstallations[index] = value;
                }
                else return;
            }
        }
        [JsonIgnore] 
        public ObservableCollection<BLInstallation> CurrentInstallations
        {
            get
            {
                Depends.On(CurrentProfile);
                if (CurrentProfile == null) return null;
                else if (CurrentProfile.Installations == null) return null;
                else return CurrentProfile.Installations;
            }
            set
            {
                if (CurrentProfile == null) return;
                else if (CurrentProfile.Installations == null) return;
                else CurrentProfile.Installations = value;
            }
        }

        #endregion

        #region IO Methods

        public static BLProfileList Load(string filePath, string lastProfile = null, string lastInstallation = null)
        {
            string json;
            BLProfileList fileData = new BLProfileList();
            if (File.Exists(filePath))
            {
                json = File.ReadAllText(filePath);
                try
                {
                    fileData = JsonConvert.DeserializeObject<BLProfileList>(json, new JsonSerializerSettings()
                    {
                        NullValueHandling = NullValueHandling.Include,
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    });
                }
                catch
                {
                    fileData = new BLProfileList();
                }
            }
            fileData.FilePath = filePath;
            fileData.Init(lastProfile, lastInstallation);
            fileData.Validate();
            return fileData;
        }
        public void Init(string lastProfile = null, string lastInstallation = null)
        {
            foreach(var profile in profiles) profile.Value.UUID = profile.Key;

            if (profiles.ContainsKey(Properties.LauncherSettings.Default.CurrentProfileUUID)) Properties.LauncherSettings.Default.CurrentProfileUUID = lastProfile;
            else if (profiles.Count != 0) Properties.LauncherSettings.Default.CurrentProfileUUID = profiles.First().Key;

            if (CurrentProfile != null)
            {
                if (CurrentInstallations.Any(x => x.InstallationUUID == lastInstallation)) CurrentInstallationUUID = lastInstallation;
                else if (CurrentInstallations.Count != 0) CurrentInstallationUUID = CurrentInstallations.First().InstallationUUID;
            }
        }
        public void Save(string filePath)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        public void Save()
        {
            if (!string.IsNullOrEmpty(FilePath)) Save(FilePath);
        }
        public void Validate()
        {
            foreach (var profile in profiles.Values)
            {
                if (profile.Installations == null)
                    profile.Installations = new ObservableCollection<BLInstallation>();

                foreach (BLInstallation generatedInstallation in profile.Installations
                    .Where(installation => installation.ReadOnly && IsGeneratedInstallation(installation))
                    .ToList())
                {
                    profile.Installations.Remove(generatedInstallation);
                }

                foreach (var installation in profile.Installations.Where(x => x.VersionUUID == Constants.LATEST_RELEASE_UUID))
                    installation.VersioningMode = VersioningMode.LatestRelease;

                foreach (var installation in profile.Installations.Where(x => x.VersionUUID == Constants.LATEST_PREVIEW_UUID))
                    installation.VersioningMode = VersioningMode.LatestPreview;
            }

            Save();
        }

        public void SyncSystemMinecraftInstallations()
        {
            if (profiles.Count == 0) return;

            bool changed = false;

            foreach (BLProfile profile in profiles.Values)
            {
                if (profile.Installations == null) continue;

                List<BLInstallation> staleSystemInstallations = profile.Installations
                    .Where(IsGeneratedInstallation)
                    .Where(x => x.ReadOnly)
                    .ToList();

                foreach (BLInstallation staleInstallation in staleSystemInstallations)
                {
                    profile.Installations.Remove(staleInstallation);
                    changed = true;
                }

                // Detected Store/local versions are exposed as a temporary Play list.
                // They are not persisted as profile installations, otherwise the
                // Installations page gets duplicate/read-only entries.
            }

            changed |= SelectPlayableInstallationIfNeeded();

            if (changed)
            {
                Save();
                OnPropertyChanged(nameof(CurrentInstallations));
                OnPropertyChanged(nameof(CurrentInstallation));
            }
        }

        public BLInstallation GetSelectedOrFirstPlayableInstallation()
        {
            IReadOnlyList<BLInstallation> playableInstallations = GetPlayableInstallationsSnapshot();
            BLInstallation current = playableInstallations
                .FirstOrDefault(x => string.Equals(x.InstallationUUID, CurrentInstallationUUID, StringComparison.OrdinalIgnoreCase));

            if (IsPlayableInstallation(current))
                return current;

            return GetBestPlayableInstallation(playableInstallations);
        }

        public BLInstallation EnsurePlayableInstallationSelected(bool forceSync = false)
        {
            if (forceSync || DateTime.UtcNow - lastPlayableSelectionSyncUtc > TimeSpan.FromSeconds(2))
            {
                lastPlayableSelectionSyncUtc = DateTime.UtcNow;
                SyncSystemMinecraftInstallations();
            }

            IReadOnlyList<BLInstallation> playableInstallations = GetPlayableInstallationsSnapshot();
            BLInstallation current = playableInstallations
                .FirstOrDefault(x => string.Equals(x.InstallationUUID, CurrentInstallationUUID, StringComparison.OrdinalIgnoreCase));

            if (IsPlayableInstallation(current))
                return current;

            BLInstallation playableInstallation = GetBestPlayableInstallation(playableInstallations);
            if (playableInstallation != null)
                SelectInstallation(playableInstallation.InstallationUUID, saveProfile: true);

            return playableInstallation ?? current;
        }

        private bool SelectPlayableInstallationIfNeeded()
        {
            IReadOnlyList<BLInstallation> playableInstallations = GetPlayableInstallationsSnapshot();
            BLInstallation current = playableInstallations
                .FirstOrDefault(x => string.Equals(x.InstallationUUID, CurrentInstallationUUID, StringComparison.OrdinalIgnoreCase));

            if (IsPlayableInstallation(current))
                return false;

            BLInstallation playableInstallation = GetBestPlayableInstallation(playableInstallations);
            if (playableInstallation == null)
                return false;

            return SelectInstallation(playableInstallation.InstallationUUID, saveProfile: false);
        }

        private bool SelectInstallation(string installationUUID, bool saveProfile)
        {
            if (string.IsNullOrWhiteSpace(installationUUID) ||
                string.Equals(CurrentInstallationUUID, installationUUID, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            CurrentInstallationUUID = installationUUID;
            if (saveProfile)
                Save();

            OnPropertyChanged(nameof(CurrentInstallationUUID));
            OnPropertyChanged(nameof(CurrentInstallation));
            return true;
        }

        private BLInstallation GetBestPlayableInstallation(IReadOnlyList<BLInstallation> playableInstallations = null)
        {
            return (playableInstallations ?? GetPlayableInstallationsSnapshot())
                .OrderByDescending(GetPlayableInstallationSourcePriority)
                .ThenByDescending(GetPlayableInstallationVersion)
                .ThenBy(x => x.DisplayName)
                .FirstOrDefault();
        }

        public IReadOnlyList<BLInstallation> GetPlayableInstallationsSnapshot()
        {
            List<BLInstallation> userInstallations = CurrentInstallations?
                .Where(installation => installation != null)
                .Where(installation => !IsGeneratedInstallation(installation))
                .Where(IsPlayableInstallation)
                .ToList() ?? new List<BLInstallation>();

            if (userInstallations.Count > 0)
                return NormalizePlayableInstallations(userInstallations);

            IEnumerable<BLInstallation> detectedInstallations = GetInstalledLauncherMinecraftVersions()
                .Select(CreateLauncherVersionInstallation)
                .Concat(GetInstalledSystemMinecraftVersions().Select(CreateSystemMinecraftInstallation));

            return NormalizePlayableInstallations(detectedInstallations);
        }

        private static IReadOnlyList<BLInstallation> NormalizePlayableInstallations(IEnumerable<BLInstallation> installations)
        {
            return installations
                .Where(IsPlayableInstallation)
                .GroupBy(GetPlayableInstallationDedupKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(GetPlayableInstallationSourcePriority)
                    .ThenByDescending(GetPlayableInstallationVersion)
                    .ThenBy(x => x.DisplayName)
                    .First())
                .OrderByDescending(GetPlayableInstallationSourcePriority)
                .ThenByDescending(GetPlayableInstallationVersion)
                .ThenBy(x => x.DisplayName)
                .ToList();
        }

        private static bool IsPlayableInstallation(BLInstallation installation)
        {
            if (installation?.IsPlayableInstalled != true || IsFloatingInstallationForPlay(installation))
                return false;

            if (installation.InstallationUUID?.StartsWith(VersionsPageSelectedInstallationPrefix, StringComparison.OrdinalIgnoreCase) == true)
                return HasLocalVersionFolder(installation);

            return true;
        }

        private static bool HasLocalVersionFolder(BLInstallation installation)
        {
            string[] candidates =
            {
                installation.VersionUUID,
                installation.Version?.Name
            };

            foreach (string candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                foreach (string directory in LauncherVersionPathHelper.GetDirectoriesToScan(MainDataModel.Default.FilePaths.VersionsFolder)
                    .Select(root => Path.GetFullPath(Path.Combine(root.FullName, candidate)))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!Directory.Exists(directory))
                        continue;

                    string manifestPath = FindFileIgnoringCase(directory, MCVersionExtensions.MainifestFileName);
                    if (string.IsNullOrWhiteSpace(manifestPath))
                        continue;

                    if (IsGdkVersion(candidate) || IsGdkVersion(installation.Version?.Name))
                        return IsCompleteGdkVersionDirectory(directory);

                    return true;
                }
            }

            return false;
        }

        private static bool IsFloatingInstallationForPlay(BLInstallation installation)
        {
            return installation.VersionUUID == Constants.LATEST_RELEASE_UUID ||
                   installation.VersionUUID == Constants.LATEST_PREVIEW_UUID ||
                   installation.VersionUUID == Constants.LATEST_BETA_UUID ||
                   installation.InstallationUUID == Constants.LATEST_RELEASE_UUID ||
                   installation.InstallationUUID == Constants.LATEST_PREVIEW_UUID ||
                   installation.InstallationUUID == Constants.LATEST_BETA_UUID;
        }

        public static bool IsGeneratedInstallation(BLInstallation installation)
        {
            if (installation == null) return false;

            return IsFloatingInstallationForPlay(installation) ||
                   IsLegacyVersionsPageSelectedInstallation(installation.InstallationUUID) ||
                   installation.InstallationUUID?.StartsWith(CatalogGeneratedInstallationPrefix, StringComparison.OrdinalIgnoreCase) == true ||
                   installation.InstallationUUID?.StartsWith(SystemMinecraftInstallationPrefix, StringComparison.OrdinalIgnoreCase) == true ||
                   installation.InstallationUUID?.StartsWith(VersionsPageSelectedInstallationPrefix, StringComparison.OrdinalIgnoreCase) == true;
        }

        private static int GetPlayableInstallationSourcePriority(BLInstallation installation)
        {
            if (!IsGeneratedInstallation(installation))
                return 3;
            if (installation.InstallationUUID?.StartsWith(VersionsPageSelectedInstallationPrefix, StringComparison.OrdinalIgnoreCase) == true)
                return 2;
            if (installation.InstallationUUID?.StartsWith(SystemMinecraftInstallationPrefix, StringComparison.OrdinalIgnoreCase) == true)
                return 1;

            return 0;
        }

        private static string GetPlayableInstallationDedupKey(BLInstallation installation)
        {
            if (!IsGeneratedInstallation(installation))
                return "user:" + installation.InstallationUUID;

            string versionType = installation.Version?.Type.ToString() ?? installation.VersionType.ToString();
            string versionName = installation.Version?.Name ?? installation.VersionUUID ?? installation.DisplayName;
            string architecture = installation.Version?.Architecture ?? string.Empty;
            return $"generated:{versionType}:{versionName}:{architecture}";
        }

        private static System.Version GetPlayableInstallationVersion(BLInstallation installation)
        {
            if (System.Version.TryParse(installation?.Version?.Name, out System.Version version))
                return version;

            return new System.Version(0, 0);
        }

        private static List<MCVersion> GetInstalledLauncherMinecraftVersions()
        {
            List<MCVersion> installedVersions = MainDataModel.Default.Versions
                .Where(x => x != null)
                .Where(x => !IsFloatingVersionEntry(x.UUID))
                .Where(x => System.Version.TryParse(x.Name, out _))
                .Where(x => x.IsInstalledInLauncher)
                .ToList();

            foreach (MCVersion localVersion in GetInstalledLauncherMinecraftVersionsFromDisk())
            {
                installedVersions.Add(localVersion);
            }

            return installedVersions
                .GroupBy(GetVersionInstallationKey)
                .Select(group => group
                    .OrderByDescending(GetVersionSpecificity)
                    .ThenBy(x => x.Name)
                    .First())
                .OrderByDescending(x => System.Version.TryParse(x.Name, out System.Version parsed) ? parsed : new System.Version(0, 0))
                .ToList();
        }

        private static IEnumerable<MCVersion> GetInstalledLauncherMinecraftVersionsFromDisk()
        {
            foreach (DirectoryInfo versionsDirectory in LauncherVersionPathHelper.GetDirectoriesToScan(MainDataModel.Default.FilePaths.VersionsFolder))
            {
                foreach (DirectoryInfo directory in versionsDirectory.EnumerateDirectories())
                {
                    if (IsIgnoredVersionDirectory(directory.Name))
                        continue;

                    string manifestPath = FindFileIgnoringCase(directory.FullName, MCVersionExtensions.MainifestFileName);
                    if (string.IsNullOrWhiteSpace(manifestPath))
                        continue;

                    MCVersion localVersion = TryCreateLocalVersion(directory, manifestPath);
                    if (localVersion != null && localVersion.IsInstalledInLauncher)
                        yield return localVersion;
                }
            }
        }

        private static MCVersion TryCreateLocalVersion(DirectoryInfo directory, string manifestPath)
        {
            try
            {
                var (packageName, packageVersion, architecture) = GetManifestIdentity(manifestPath);
                VersionType type;
                if (string.Equals(packageName, "Microsoft.MinecraftUWP", StringComparison.OrdinalIgnoreCase))
                    type = BedrockLauncher.UpdateProcessor.Enums.VersionType.Release;
                else if (string.Equals(packageName, "Microsoft.MinecraftWindowsBeta", StringComparison.OrdinalIgnoreCase))
                    type = BedrockLauncher.UpdateProcessor.Enums.VersionType.Preview;
                else
                    return null;

                string versionName = GetLocalFolderVersionName(directory.Name, packageVersion, type);
                string packageIdPath = FindFileIgnoringCase(directory.FullName, MCVersionExtensions.IdentificationFilename);
                string packageId = !string.IsNullOrWhiteSpace(packageIdPath)
                    ? File.ReadAllText(packageIdPath).Trim()
                    : directory.Name;

                if (string.IsNullOrWhiteSpace(packageId))
                    packageId = directory.Name;

                if (IsGdkVersion(versionName) && !IsCompleteGdkVersionDirectory(directory.FullName))
                    return null;

                return new MCVersion(directory.Name, packageId, versionName, type, architecture);
            }
            catch
            {
                return null;
            }
        }

        private static (string PackageName, string PackageVersion, string Architecture) GetManifestIdentity(string manifestPath)
        {
            XElement identity = XDocument.Load(manifestPath)
                .Descendants()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Identity", StringComparison.OrdinalIgnoreCase));

            if (identity == null)
                throw new InvalidDataException("Minecraft package manifest does not contain an Identity element.");

            return (
                identity.Attribute("Name")?.Value,
                identity.Attribute("Version")?.Value,
                identity.Attribute("ProcessorArchitecture")?.Value
            );
        }

        private static MCVersion FindMatchingKnownVersion(MCVersion localVersion)
        {
            return MainDataModel.Default.Versions
                .Where(x => x != null)
                .Where(x => !IsFloatingVersionEntry(x.UUID))
                .FirstOrDefault(x =>
                    x.Type == localVersion.Type &&
                    string.Equals(x.Name, localVersion.Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Architecture, localVersion.Architecture, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsIgnoredVersionDirectory(string directoryName)
        {
            return directoryName.Equals("AppxBackups", StringComparison.OrdinalIgnoreCase) ||
                directoryName.Contains(".broken_", StringComparison.OrdinalIgnoreCase) ||
                directoryName.Contains(".incomplete_", StringComparison.OrdinalIgnoreCase);
        }

        private static string FindFileIgnoringCase(string directory, string fileName)
        {
            try
            {
                return Directory.EnumerateFiles(directory)
                    .FirstOrDefault(path => string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        private static string GetLocalFolderVersionName(string folderName, string manifestVersion, VersionType type)
        {
            string normalizedFolderName = folderName;
            if (type == BedrockLauncher.UpdateProcessor.Enums.VersionType.Preview &&
                normalizedFolderName.StartsWith("preview-", StringComparison.OrdinalIgnoreCase))
            {
                normalizedFolderName = normalizedFolderName.Substring("preview-".Length);
            }
            else if (type == BedrockLauncher.UpdateProcessor.Enums.VersionType.Beta &&
                normalizedFolderName.StartsWith("beta-", StringComparison.OrdinalIgnoreCase))
            {
                normalizedFolderName = normalizedFolderName.Substring("beta-".Length);
            }

            if (System.Version.TryParse(normalizedFolderName, out _))
                return normalizedFolderName;

            return GetDisplayVersionFromPackageVersion(manifestVersion);
        }

        private static string GetDisplayVersionFromPackageVersion(string packageVersion)
        {
            if (!System.Version.TryParse(packageVersion, out System.Version version))
                return packageVersion;

            if (version.Major == 1 && version.Minor >= 22 && version.Build >= 100)
            {
                int feature = version.Build / 100;
                int patch = version.Build % 100;
                return patch > 0 ? $"{version.Minor}.{feature}.{patch}" : $"{version.Minor}.{feature}";
            }

            if (version.Major == 1 && version.Build >= 100)
            {
                int build = version.Build / 100;
                int revision = version.Build % 100;
                return revision > 0 ? $"1.{version.Minor}.{build}.{revision}" : $"1.{version.Minor}.{build}";
            }

            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private static bool IsGdkVersion(string versionName)
        {
            if (!MinecraftVersion.TryParse(versionName, out MinecraftVersion version))
                return false;
            if (!MinecraftVersion.TryParse(Constants.FIRST_GDK_VERSION, out MinecraftVersion minimumGdkVersion))
                return false;

            return version.CompareTo(minimumGdkVersion) >= 0;
        }

        private static bool IsCompleteGdkVersionDirectory(string directory)
        {
            return File.Exists(Path.Combine(directory, "Minecraft.Windows.exe")) &&
                File.Exists(Path.Combine(directory, "MicrosoftGame.Config")) &&
                Directory.Exists(Path.Combine(directory, "data")) &&
                RequiredGdkRuntimeDlls.All(fileName => File.Exists(Path.Combine(directory, fileName)));
        }

        private static List<MCVersion> GetInstalledSystemMinecraftVersions()
        {
            return MainDataModel.Default.Versions
                .Where(x => x != null)
                .Where(x => !x.IsCustom)
                .Where(x => !IsFloatingVersionEntry(x.UUID))
                .Where(x => System.Version.TryParse(x.Name, out _))
                .Where(x => x.IsInstalledExternally)
                .GroupBy(GetSystemVersionGroupKey)
                .Select(group => group
                    .OrderByDescending(GetVersionSpecificity)
                    .ThenBy(x => x.Name)
                    .First())
                .OrderByDescending(x => System.Version.TryParse(x.Name, out System.Version parsed) ? parsed : new System.Version(0, 0))
                .ToList();
        }

        private static BLInstallation CreateSystemMinecraftInstallation(MCVersion version)
        {
            string displayName = GetSystemInstallationName(version);
            return new BLInstallation()
            {
                DisplayName = displayName,
                DirectoryName = displayName,
                VersionUUID = version.UUID,
                VersioningMode = VersioningMode.None,
                IconPath = version.InstallationIconFileName,
                IsCustomIcon = false,
                ReadOnly = true,
                InstallationUUID = GetSystemInstallationUUID(version)
            };
        }

        private static BLInstallation CreateLauncherVersionInstallation(MCVersion version)
        {
            string displayName = GetVersionsPageSelectedInstallationName(version);
            return new BLInstallation()
            {
                DisplayName = displayName,
                DirectoryName = ValidatePathNameStatic(displayName),
                VersionUUID = version.UUID,
                VersioningMode = VersioningMode.None,
                IconPath = version.InstallationIconFileName,
                IsCustomIcon = false,
                ReadOnly = true,
                InstallationUUID = GetVersionsPageSelectedInstallationUUID(version)
            };
        }

        private static string GetSystemInstallationName(MCVersion version)
        {
            string productName = version.Type == BedrockLauncher.UpdateProcessor.Enums.VersionType.Preview ? "Minecraft Preview" : "Minecraft for Windows";
            return $"{productName} {version.Name}";
        }

        private static string GetSystemInstallationUUID(MCVersion version)
        {
            return SystemMinecraftInstallationPrefix + GetSystemVersionGroupKey(version);
        }

        private static string GetVersionsPageSelectedInstallationUUID(MCVersion version)
        {
            return VersionsPageSelectedInstallationPrefix + GetVersionInstallationKey(version);
        }

        private static string GetVersionsPageSelectedInstallationName(MCVersion version)
        {
            string productName = version.Type == BedrockLauncher.UpdateProcessor.Enums.VersionType.Preview ? "Minecraft Preview" : "Minecraft for Windows";
            return $"{productName} {version.Name}";
        }

        private static bool IsLegacyVersionsPageSelectedInstallation(string installationUUID)
        {
            if (string.IsNullOrWhiteSpace(installationUUID)) return false;
            if (!installationUUID.StartsWith(VersionsPageSelectedInstallationPrefix, StringComparison.OrdinalIgnoreCase)) return false;

            string suffix = installationUUID.Substring(VersionsPageSelectedInstallationPrefix.Length);
            return Enum.TryParse(suffix, ignoreCase: true, out BedrockLauncher.UpdateProcessor.Enums.VersionType _);
        }

        private static string GetSystemVersionGroupKey(MCVersion version)
        {
            if (!System.Version.TryParse(version.Name, out System.Version parsed)) return version.UUID;

            if (parsed.Major == 1)
            {
                int build = Math.Max(parsed.Build, 0);
                return $"{version.Type}:1.{parsed.Minor}.{build}";
            }

            int minor = Math.Max(parsed.Minor, 0);
            return $"{version.Type}:{parsed.Major}.{minor}";
        }

        private static string GetVersionInstallationKey(MCVersion version)
        {
            string versionKey = !string.IsNullOrWhiteSpace(version.UUID)
                ? version.UUID
                : $"{version.Type}:{version.Name}:{version.Architecture}";

            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            string safeVersionKey = new string(versionKey.Where(ch => !invalidFileNameChars.Contains(ch)).ToArray());
            return $"{version.Type}:{safeVersionKey}";
        }

        private static string ValidatePathNameStatic(string pathName)
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            return new string(pathName.Select(ch => invalidFileNameChars.Contains(ch) ? '_' : ch).ToArray());
        }

        private static int GetVersionSpecificity(MCVersion version)
        {
            if (!System.Version.TryParse(version.Name, out System.Version parsed)) return 0;

            int specificity = 0;
            if (parsed.Major >= 0) specificity++;
            if (parsed.Minor >= 0) specificity++;
            if (parsed.Build >= 0) specificity++;
            if (parsed.Revision >= 0) specificity++;
            return specificity;
        }

        private static bool IsFloatingVersionEntry(string uuid)
        {
            return uuid == Constants.LATEST_RELEASE_UUID ||
                   uuid == Constants.LATEST_PREVIEW_UUID ||
                   uuid == Constants.LATEST_BETA_UUID;
        }

        private void GenerateProfileImage(string img, string uuid)
        {
            string path = MainDataModel.Default.FilePaths.GetProfilePath(uuid);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            string new_img = Path.Combine(path, Constants.PROFILE_CUSTOM_IMG_NAME);
            if (string.IsNullOrEmpty(img)) return;
            else
            {
                try
                {
                    File.Copy(img, new_img, true);
                }
                catch
                {
                    //TODO: Add Error Message
                }
            }

        }

        #endregion

        #region Management Methods
        string ValidatePathName(string pathName)
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            return new string(pathName.Select(ch => invalidFileNameChars.Contains(ch) ? '_' : ch).ToArray());
        }
        public bool Profile_Add(string name, string uuid, string directory, string img)
        {
            var real_directory = ValidatePathName(directory);
            BLProfile profileSettings = new BLProfile(name, real_directory, uuid);
            

            if (profiles.ContainsKey(uuid)) return false;
            else
            {
                profiles.Add(uuid, profileSettings);
                GenerateProfileImage(img, uuid);

                Profile_Switch(uuid);
                Validate();
                Save();
                return true;
            }

        }
        public bool Profile_Edit(string name, string uuid, string directory, string img)
        {
            var real_directory = ValidatePathName(directory);

            if (!profiles.ContainsKey(uuid)) return false;
            else
            {
                profiles[uuid].Name = name;
                profiles[uuid].ProfilePath = name;
                GenerateProfileImage(img, uuid);

                Profile_Switch(uuid);
                Validate();
                Save();
                return true;
            }

        }
        public void Profile_Remove(string profileUUID)
        {
            if (profiles.ContainsKey(profileUUID) && profiles.Count > 1)
            {
                profiles.Remove(profileUUID);
                Save();
                Profile_Switch(profiles.FirstOrDefault().Key);
            }

        }
        public void Profile_Switch(string profileUUID)
        {
            if (profiles.ContainsKey(profileUUID))
            {
                Properties.LauncherSettings.Default.CurrentProfileUUID = profileUUID;      
                Properties.LauncherSettings.Default.Save();

                OnPropertyChanged(nameof(CurrentProfile));
                OnPropertyChanged(nameof(CurrentInstallations));
                OnPropertyChanged(nameof(CurrentInstallation));
                OnPropertyChanged(nameof(CurrentProfileImagePath));
            }
        }

        public void Installation_Add(BLInstallation installation)
        {
            if (CurrentProfile == null) return;
            if (CurrentInstallations == null) return;
            if (!CurrentInstallations.Any(x => x.InstallationUUID == installation.InstallationUUID))
            {
                CurrentInstallations.Add(installation);
                Save();
            }
        }

        public void Installation_Move(BLInstallation installation, bool moveUp)
        {
            if (CurrentProfile == null) return;
            if (CurrentInstallations == null) return;
            if (CurrentInstallations.Any(x => x.InstallationUUID == installation.InstallationUUID))
            {
                int oldIndex = CurrentInstallations.FindIndex(x => x.InstallationUUID == installation.InstallationUUID);
                int count = CurrentInstallations.Count() - 1;
                int newIndex = oldIndex + (moveUp ? -1 : 1);
                if (newIndex >= 0 && newIndex <= count) CurrentInstallations.Move(oldIndex, newIndex);
                Save();
            }
        }

        public void Installation_MoveDown(BLInstallation installation)
        {
            Installation_Move(installation, false);
        }

        public void Installation_MoveUp(BLInstallation installation)
        {
            Installation_Move(installation, true);
        }

        public void Installation_Clone(BLInstallation installation)
        {
            if (CurrentProfile == null) return;
            if (CurrentInstallations == null) return;
            if (CurrentInstallations.Any(x => x.InstallationUUID == installation.InstallationUUID))
            {
                string newName = installation.DisplayName;
                int i = 1;

                while (CurrentInstallations.Any(x => x.DisplayName == newName))
                {
                    newName = $"{installation.DisplayName} ({i})";
                    i++;
                }
                var Clone = installation.Clone(newName);
                Clone.DirectoryName = ValidatePathName(newName);
                Clone.ReadOnly = false;
                Installation_Add(Clone);
            }
        }
        public void Installation_Create(string name, MCVersion version, string directory, string iconPath = null, bool isCustom = false)
        {
            if (CurrentProfile == null) return;
            if (CurrentInstallations == null) return;
            if (string.IsNullOrEmpty(name) || name == BedrockLauncher.Localization.Language.LanguageManager.GetResource("VersionEntries_UnnamedInstallation").ToString()) name = Guid.NewGuid().ToString();
            GetVersionParams(version, out VersioningMode versioningMode, out string version_uuid);
            BLInstallation new_installation = new BLInstallation()
            {
                DisplayName = name,
                IconPath = (iconPath == null ? version?.InstallationIconFileName ?? Constants.INSTALLATIONS_FALLBACK_ICONPATH : iconPath),
                IsCustomIcon = isCustom,
                DirectoryName = ValidatePathName(name),
                VersioningMode = versioningMode,
                VersionUUID = version_uuid
            };

            Installation_Add(new_installation);
        }

        public BLInstallation SelectOrCreateVersionInstallation(MCVersion version)
        {
            if (version == null) return null;
            if (CurrentProfile == null) return null;
            if (CurrentInstallations == null) return null;

            string installationUUID = GetVersionsPageSelectedInstallationUUID(version);
            string displayName = GetVersionsPageSelectedInstallationName(version);
            BLInstallation installation = CurrentInstallations.FirstOrDefault(x => x.InstallationUUID == installationUUID);

            if (installation == null)
            {
                installation = new BLInstallation()
                {
                    InstallationUUID = installationUUID,
                    ReadOnly = true
                };
                CurrentInstallations.Add(installation);
            }

            installation.DisplayName = displayName;
            installation.DirectoryName = ValidatePathName(displayName);
            installation.VersionUUID = version.UUID;
            installation.VersioningMode = VersioningMode.None;
            installation.IconPath = version.InstallationIconFileName;
            installation.IsCustomIcon = false;

            CurrentInstallationUUID = installation.InstallationUUID;
            Save();
            OnPropertyChanged(nameof(CurrentInstallations));
            OnPropertyChanged(nameof(CurrentInstallation));

            return installation;
        }

        public void Installation_Edit(string uuid, string name, MCVersion version, string directory, string iconPath = null, bool isCustom = false)
        {
            if (CurrentProfile == null) return;
            if (CurrentInstallations == null) return;
            string OldName = "";
            if (CurrentInstallations.Any(x => x.InstallationUUID == uuid))
            {
                int index = CurrentInstallations.FindIndex(x => x.InstallationUUID == uuid);
                OldName = CurrentInstallations[index].DisplayName;
            }
            GetVersionParams(version, out VersioningMode versioningMode, out string version_uuid);
            BLInstallation new_installation = new BLInstallation()
            {
                DisplayName = name,
                IconPath = (iconPath == null ? version?.InstallationIconFileName ?? Constants.INSTALLATIONS_FALLBACK_ICONPATH : iconPath),
                IsCustomIcon = isCustom,
                DirectoryName = ValidatePathName(name),
                VersioningMode = versioningMode,
                VersionUUID = version_uuid
            };
            

            if (CurrentInstallations.Any(x => x.InstallationUUID == uuid))
            {
                int index = CurrentInstallations.FindIndex(x => x.InstallationUUID == uuid);
                CurrentInstallations[index] = new_installation;
                Save();
            }
            //We need to move data to new directory
            if (OldName != name) Directory.Move(Path.Combine(MainDataModel.Default.FilePaths.GetProfilePath(Properties.LauncherSettings.Default.CurrentProfileUUID), OldName), Path.Combine(MainDataModel.Default.FilePaths.GetProfilePath(Properties.LauncherSettings.Default.CurrentProfileUUID), name));
        }
        public void Installation_Delete(BLInstallation installation, bool deleteData = true)
        {
            if (CurrentProfile == null) return;
            if (CurrentInstallations == null) return;
            if (deleteData)
            {
                try { installation.DeleteUserData(); }
                catch (Exception ex) { _ = MainDataModel.BackwardsCommunicationHost.exceptionmsg(ex); }
            }
            CurrentInstallations.Remove(installation);
            Save();
        }
        public void Installation_UpdateLP(BLInstallation installation)
        {
            if (installation == null) return;
            installation.LastPlayed = DateTime.Now;
            Save();
        }

        #endregion

        #region Extensions

        public static void GetVersionParams(MCVersion version, out VersioningMode versioningMode, out string version_id)
        {
            version_id = Constants.LATEST_RELEASE_UUID;
            versioningMode = VersioningMode.LatestRelease;

            if (version != null)
            {
                //if (version.UUID == Constants.LATEST_BETA_UUID) versioningMode = VersioningMode.LatestBeta;
                if (version.UUID == Constants.LATEST_RELEASE_UUID) versioningMode = VersioningMode.LatestRelease;
                else if (version.UUID == Constants.LATEST_PREVIEW_UUID) versioningMode = VersioningMode.LatestPreview;
                else versioningMode = VersioningMode.None;

                version_id = version.UUID;
            }
        }

        #endregion
    }

}
