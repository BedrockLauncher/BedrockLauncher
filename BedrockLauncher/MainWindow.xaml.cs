using System;
using System.Globalization;
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
using System.Windows.Shapes;
using Newtonsoft.Json;
using BedrockLauncher;

namespace BedrockLauncher
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Threading;
    using System.Windows.Data;
    using Windows.ApplicationModel;
    using Windows.Foundation;
    using Windows.Management.Core;
    using Windows.Management.Deployment;
    using Windows.System;
    using WPFDataTypes;
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window, ICommonVersionCommands
    {
        private static readonly string MINECRAFT_PACKAGE_FAMILY = "Microsoft.MinecraftUWP_8wekyb3d8bbwe";

        private VersionList _versions;
        private readonly VersionDownloader _anonVersionDownloader = new VersionDownloader();
        private readonly VersionDownloader _userVersionDownloader = new VersionDownloader();
        private readonly Task _userVersionDownloaderLoginTask;
        private volatile int _userVersionDownloaderLoginTaskStarted;
        private volatile bool _hasLaunchTask = false;

        public BetterBedrockMain betterBedrockMain = new BetterBedrockMain();

        public MainPage mainPage = new MainPage();
        public GeneralSettingsPage generalSettingsPage = new GeneralSettingsPage();
        public SettingsScreen settingsScreenPage = new SettingsScreen();
        public NoContentPage noContentPage = new NoContentPage();
        public PlayScreenPage playScreenPage = new PlayScreenPage();

        public MainWindow()
        {
            InitializeComponent();

            _versions = new VersionList("versions.json", this);
            VersionList.ItemsSource = _versions;
            var view = CollectionViewSource.GetDefaultView(VersionList.ItemsSource) as CollectionView;
            view.Filter = VersionListFilter;
            _userVersionDownloaderLoginTask = new Task(() => {
                _userVersionDownloader.EnableUserAuthorization();
            });
            Dispatcher.Invoke(async () => {
                try
                {
                    await _versions.LoadFromCache();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("List cache load failed:\n" + e.ToString());
                }
                try
                {
                    await _versions.DownloadList();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("List download failed:\n" + e.ToString());
                }
            });
        }


        public void LanguageChange(string language)
        {
            ResourceDictionary dict = new ResourceDictionary
            {
                Source = new Uri($"..\\Resources\\i18n\\lang.{language}.xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        public ICommand LaunchCommand => new RelayCommand((v) => InvokeLaunch((Version)v));

        public ICommand RemoveCommand => new RelayCommand((v) => InvokeRemove((Version)v));

        public ICommand DownloadCommand => new RelayCommand((v) => InvokeDownload((Version)v));

        private void InvokeLaunch(Version v)
        {
            if (_hasLaunchTask)
                return;
            _hasLaunchTask = true;
            Task.Run(async () => {
                v.StateChangeInfo = new VersionStateChangeInfo();
                v.StateChangeInfo.IsLaunching = true;
                string gameDir = Path.GetFullPath(v.GameDirectory);
                try
                {
                    await ReRegisterPackage(gameDir);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("App re-register failed:\n" + e.ToString());
                    MessageBox.Show("App re-register failed:\n" + e.ToString());
                    _hasLaunchTask = false;
                    v.StateChangeInfo = null;
                    return;
                }
                try
                {
                    var pkg = await AppDiagnosticInfo.RequestInfoForPackageAsync(MINECRAFT_PACKAGE_FAMILY);
                    if (pkg.Count > 0)
                        await pkg[0].LaunchAsync();
                    Debug.WriteLine("App launch finished!");
                    _hasLaunchTask = false;
                    v.StateChangeInfo = null;
                    // close launcher if needed
                    if (Properties.Settings.Default.KeepLauncherOpenCheckBox == false)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Application.Current.MainWindow.Close();
                        });
                    }

                }
                catch (Exception e)
                {
                    Debug.WriteLine("App launch failed:\n" + e.ToString());
                    MessageBox.Show("App launch failed:\n" + e.ToString());
                    _hasLaunchTask = false;
                    v.StateChangeInfo = null;
                    return;
                }
            });
        }

        private async Task DeploymentProgressWrapper(IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> t)
        {
            TaskCompletionSource<int> src = new TaskCompletionSource<int>();
            t.Progress += (v, p) => {
                Debug.WriteLine("Deployment progress: " + p.state + " " + p.percentage + "%");
            };
            t.Completed += (v, p) => {
                if (p == AsyncStatus.Error)
                {
                    Debug.WriteLine("Deployment failed: " + v.GetResults().ErrorText);
                    src.SetException(new Exception("Deployment failed: " + v.GetResults().ErrorText));
                }
                else
                {
                    Debug.WriteLine("Deployment done: " + p);
                    src.SetResult(1);
                }
            };
            await src.Task;
        }

        private string GetBackupMinecraftDataDir()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string tmpDir = Path.Combine(localAppData, "TmpMinecraftLocalState");
            return tmpDir;
        }

        private void BackupMinecraftDataForRemoval()
        {
            var data = ApplicationDataManager.CreateForPackageFamily(MINECRAFT_PACKAGE_FAMILY);
            string tmpDir = GetBackupMinecraftDataDir();
            if (Directory.Exists(tmpDir))
            {
                Debug.WriteLine("BackupMinecraftDataForRemoval error: " + tmpDir + " already exists");
                Process.Start("explorer.exe", tmpDir);
                MessageBox.Show("The temporary directory for backing up MC data already exists. This probably means that we failed last time backing up the data. Please back the directory up manually.");
                throw new Exception("Temporary dir exists");
            }
            Debug.WriteLine("Moving Minecraft data to: " + tmpDir);
            Directory.Move(data.LocalFolder.Path, tmpDir);
        }

        private void RestoreMove(string from, string to)
        {
            foreach (var f in Directory.EnumerateFiles(from))
            {
                string ft = Path.Combine(to, Path.GetFileName(f));
                if (File.Exists(ft))
                {
                    if (MessageBox.Show("The file " + ft + " already exists in the destination.\nDo you want to replace it? The old file will be lost otherwise.", "Restoring data directory from previous installation", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        continue;
                    File.Delete(ft);
                }
                File.Move(f, ft);
            }
            foreach (var f in Directory.EnumerateDirectories(from))
            {
                string tp = Path.Combine(to, Path.GetFileName(f));
                if (!Directory.Exists(tp))
                {
                    if (File.Exists(tp) && MessageBox.Show("The file " + tp + " is not a directory. Do you want to remove it? The data from the old directory will be lost otherwise.", "Restoring data directory from previous installation", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        continue;
                    Directory.CreateDirectory(tp);
                }
                RestoreMove(f, tp);
            }
        }

        private void RestoreMinecraftDataFromReinstall()
        {
            string tmpDir = GetBackupMinecraftDataDir();
            if (!Directory.Exists(tmpDir))
                return;
            var data = ApplicationDataManager.CreateForPackageFamily(MINECRAFT_PACKAGE_FAMILY);
            Debug.WriteLine("Moving backup Minecraft data to: " + data.LocalFolder.Path);
            RestoreMove(tmpDir, data.LocalFolder.Path);
            Directory.Delete(tmpDir, true);
        }

        private async Task RemovePackage(Package pkg)
        {
            Debug.WriteLine("Removing package: " + pkg.Id.FullName);
            if (!pkg.IsDevelopmentMode)
            {
                BackupMinecraftDataForRemoval();
                await DeploymentProgressWrapper(new PackageManager().RemovePackageAsync(pkg.Id.FullName, 0));
            }
            else
            {
                Debug.WriteLine("Package is in development mode");
                await DeploymentProgressWrapper(new PackageManager().RemovePackageAsync(pkg.Id.FullName, RemovalOptions.PreserveApplicationData));
            }
            Debug.WriteLine("Removal of package done: " + pkg.Id.FullName);
        }

        private string GetPackagePath(Package pkg)
        {
            try
            {
                return pkg.InstalledLocation.Path;
            }
            catch (FileNotFoundException)
            {
                return "";
            }
        }

        private async Task UnregisterPackage(string gameDir)
        {
            foreach (var pkg in new PackageManager().FindPackages(MINECRAFT_PACKAGE_FAMILY))
            {
                string location = GetPackagePath(pkg);
                if (location == "" || location == gameDir)
                {
                    await RemovePackage(pkg);
                }
            }
        }

        private async Task ReRegisterPackage(string gameDir)
        {
            foreach (var pkg in new PackageManager().FindPackages(MINECRAFT_PACKAGE_FAMILY))
            {
                string location = GetPackagePath(pkg);
                if (location == gameDir)
                {
                    Debug.WriteLine("Skipping package removal - same path: " + pkg.Id.FullName + " " + location);
                    return;
                }
                await RemovePackage(pkg);
            }
            Debug.WriteLine("Registering package");

            string manifestPath = Path.Combine(gameDir, "AppxManifest.xml");
            await DeploymentProgressWrapper(new PackageManager().RegisterPackageAsync(new Uri(manifestPath), null, DeploymentOptions.DevelopmentMode));
            Debug.WriteLine("App re-register done!");
            RestoreMinecraftDataFromReinstall();
        }

        private void InvokeDownload(Version v)
        {
            


            CancellationTokenSource cancelSource = new CancellationTokenSource();
            v.StateChangeInfo = new VersionStateChangeInfo();
            v.StateChangeInfo.IsInitializing = true;
            v.StateChangeInfo.CancelCommand = new RelayCommand((o) => cancelSource.Cancel());

            Debug.WriteLine(v);
            Debug.WriteLine("Download start");
            Task.Run(async () => {
                string dlPath = "Minecraft-" + v.Name + ".Appx";
                VersionDownloader downloader = _anonVersionDownloader;
                if (v.IsBeta)
                {
                    downloader = _userVersionDownloader;
                    if (Interlocked.CompareExchange(ref _userVersionDownloaderLoginTaskStarted, 1, 0) == 0)
                    {
                        _userVersionDownloaderLoginTask.Start();
                    }
                    Debug.WriteLine("Waiting for authentication");
                    try
                    {
                        await _userVersionDownloaderLoginTask;
                        Debug.WriteLine("Authentication complete");
                    }
                    catch (Exception e)
                    {
                        v.StateChangeInfo = null;
                        Debug.WriteLine("Authentication failed:\n" + e.ToString());
                        MessageBox.Show("Failed to authenticate. Please make sure your account is subscribed to the beta programme.\n\n" + e.ToString(), "Authentication failed");
                        return;
                    }
                }
                try
                {
                    await downloader.Download(v.UUID, "1", dlPath, (current, total) => {
                        if (v.StateChangeInfo.IsInitializing)
                        {
                            Debug.WriteLine("Actual download started");
                            v.StateChangeInfo.IsInitializing = false;
                            if (total.HasValue)
                                v.StateChangeInfo.TotalSize = total.Value;
                        }
                        v.StateChangeInfo.DownloadedBytes = current;
                    }, cancelSource.Token);
                    Debug.WriteLine("Download complete");
                    
                    //await System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeAsync((Action)(() => { ((MainWindow)Application.Current.MainWindow).pr; }));

                }
                catch (Exception e)
                {
                    Debug.WriteLine("Download failed:\n" + e.ToString());
                    if (!(e is TaskCanceledException))
                        MessageBox.Show("Download failed:\n" + e.ToString());
                    v.StateChangeInfo = null;
                    return;
                }
                try
                {
                    // Better Bedrock
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        //button1.Background = (Brush)this.TryFindResource("buttonGradientBrush");
                        ((MainWindow)Application.Current.MainWindow).progressbarcontent.SetResourceReference(TextBlock.TextProperty, "MainPage_ProgressBarInstalling");
                        ((MainWindow)Application.Current.MainWindow).ProgressBarText.Text = "";
                        //InvokeLaunch(VersionList.SelectedItem as Version);
                    });
                    v.StateChangeInfo.IsExtracting = true;
                    string dirPath = v.GameDirectory;
                    if (Directory.Exists(dirPath))
                        Directory.Delete(dirPath, true);
                    ZipFile.ExtractToDirectory(dlPath, dirPath);
                    v.StateChangeInfo = null;
                    File.Delete(Path.Combine(dirPath, "AppxSignature.p7x"));
                    File.Delete(dlPath);

                    betterBedrockMain.BetterBedrock(v.DisplayName, v.GameDirectory);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ((MainWindow)Application.Current.MainWindow).ProgressBarGrid.Visibility = Visibility.Hidden;
                        //InvokeLaunch(VersionList.SelectedItem as Version);
                    });
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Extraction failed:\n" + e.ToString());
                    MessageBox.Show("Extraction failed:\n" + e.ToString());
                    v.StateChangeInfo = null;
                    return;
                }
                v.StateChangeInfo = null;
                v.UpdateInstallStatus();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    InvokeLaunch(VersionList.SelectedItem as Version);
                });
            });
        }

        private void InvokeRemove(Version v)
        {
            Task.Run(async () => {
                v.StateChangeInfo = new VersionStateChangeInfo();
                v.StateChangeInfo.IsUninstalling = true;
                await UnregisterPackage(Path.GetFullPath(v.GameDirectory));
                Directory.Delete(v.GameDirectory, true);
                v.StateChangeInfo = null;
                v.UpdateInstallStatus();
            });
        }

        private bool VersionListFilter(object obj)
        {
            Version v = obj as Version;
            VersionList.SelectedIndex = Properties.Settings.Default.LastSelectedVersion; // show last version in combobox
            return (!v.IsBeta || Properties.Settings.Default.ShowBetas) && (v.IsInstalled || !Properties.Settings.Default.ShowInstalledOnly);
        }

        private void NewsButton_Click(object sender, RoutedEventArgs e)
        {
            switch (NewsButton.IsChecked)
            {
                case true:
                    MainWindowFrame.Navigate(noContentPage); // Переключение фрейма MainWindow на нужное окно
                    PlayScreenBorder.Visibility = Visibility.Hidden; // Скрывает нижнюю панель в MainWindow

                    // Выключение других кнопок
                    BedrockEditionButton.IsChecked = false;
                    SettingsButton.IsChecked = false;
                    break;
                case false:
                    NewsButton.IsChecked = true; // Не снимает свойства IsChecked при повторном нажатии на кнопку
                    break;
            }
        }

        private void BedrockEditionButton_Click(object sender, RoutedEventArgs e)
        {
            switch (BedrockEditionButton.IsChecked)
            {
                case true:
                    MainWindowFrame.Navigate(mainPage); // Переключение фрейма MainWindow на окно MainPage.xaml
                    mainPage.MainPageFrame.Navigate(playScreenPage); // Переключение фрейма MainPage на окно PlayScreenPage.xaml
                    PlayScreenBorder.Visibility = Visibility.Visible; // Показывает нижнюю панель в MainWindow
                    // Оставляет нажатой только кнопку PlayButton в окне MainPage.xaml
                    mainPage.PlayButton.IsChecked = true;
                    mainPage.InstallationsButton.IsChecked = false;
                    mainPage.SkinsButton.IsChecked = false;
                    mainPage.PatchNotesButton.IsChecked = false;

                    // Выключение других кнопок
                    NewsButton.IsChecked = false;
                    SettingsButton.IsChecked = false;
                    break;
                case false:
                    BedrockEditionButton.IsChecked = true; // Не снимает свойства IsChecked при повторном нажатии на кнопку
                    MainWindowFrame.Navigate(mainPage); // Переключение фрейма MainWindow на окно MainPage.xaml
                    mainPage.MainPageFrame.Navigate(playScreenPage); // Переключение фрейма MainPage на окно PlayScreenPage.xaml
                    PlayScreenBorder.Visibility = Visibility.Visible; // Показывает нижнюю панель в MainWindow

                    // Оставляет нажатой только кнопку PlayButton в окне MainPage.xaml
                    mainPage.PlayButton.IsChecked = true;
                    mainPage.InstallationsButton.IsChecked = false;
                    mainPage.SkinsButton.IsChecked = false;
                    mainPage.PatchNotesButton.IsChecked = false;
                    break;
            }
        }

        private void JavaEditionButton_Click(object sender, RoutedEventArgs e)
        {
            switch (JavaEditionButton.IsChecked)
            {
                case true:
                    MainWindowFrame.Navigate(mainPage); // Переключение фрейма MainWindow на окно MainPage.xaml
                    mainPage.MainPageFrame.Navigate(playScreenPage); // Переключение фрейма MainPage на окно PlayScreenPage.xaml
                    PlayScreenBorder.Visibility = Visibility.Visible; // Показывает нижнюю панель в MainWindow
                    // Оставляет нажатой только кнопку PlayButton в окне MainPage.xaml
                    mainPage.PlayButton.IsChecked = true;
                    mainPage.InstallationsButton.IsChecked = false;
                    mainPage.SkinsButton.IsChecked = false;
                    mainPage.PatchNotesButton.IsChecked = false;

                    // Выключение других кнопок
                    NewsButton.IsChecked = false;
                    SettingsButton.IsChecked = false;
                    BedrockEditionButton.IsChecked = false;
                    break;
                case false:
                    JavaEditionButton.IsChecked = true; // Не снимает свойства IsChecked при повторном нажатии на кнопку
                    MainWindowFrame.Navigate(mainPage); // Переключение фрейма MainWindow на окно MainPage.xaml
                    mainPage.MainPageFrame.Navigate(playScreenPage); // Переключение фрейма MainPage на окно PlayScreenPage.xaml
                    PlayScreenBorder.Visibility = Visibility.Visible; // Показывает нижнюю панель в MainWindow

                    // Оставляет нажатой только кнопку PlayButton в окне MainPage.xaml
                    mainPage.PlayButton.IsChecked = true;
                    mainPage.InstallationsButton.IsChecked = false;
                    mainPage.SkinsButton.IsChecked = false;
                    mainPage.PatchNotesButton.IsChecked = false;
                    break;
            }
            // Trying to find and open java launcher shortcut
            try { Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + @"\Programs\Minecraft Launcher\Minecraft Launcher"); Application.Current.MainWindow.Close(); } catch { MessageBox.Show("Cant find java launcher :/"); }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            switch (SettingsButton.IsChecked)
            {
                case true:
                    MainWindowFrame.Navigate(settingsScreenPage); // Переключение фрейма MainWindow на нужное окно
                    PlayScreenBorder.Visibility = Visibility.Hidden; // Скрывает нижнюю панель в MainWindow
                    settingsScreenPage.SettingsScreenFrame.Navigate(generalSettingsPage);

                    // Выключение других кнопок
                    BedrockEditionButton.IsChecked = false;
                    NewsButton.IsChecked = false;

                    settingsScreenPage.GeneralButton.IsChecked = true;
                    settingsScreenPage.AccountsButton.IsChecked = false;
                    settingsScreenPage.AboutButton.IsChecked = false;

                    //main
                    break;
                case false:
                    settingsScreenPage.SettingsScreenFrame.Navigate(generalSettingsPage);
                    SettingsButton.IsChecked = true; // Не снимает свойства IsChecked при повторном нажатии на кнопку
                    settingsScreenPage.GeneralButton.IsChecked = true;
                    settingsScreenPage.AccountsButton.IsChecked = false;
                    settingsScreenPage.AboutButton.IsChecked = false;
                    break;
            }
            
        }

        private void ComboBoxItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // To prevent scrolling when mouseover
            e.Handled = true;
        }

        private void MainPlayButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.LastSelectedVersion = VersionList.SelectedIndex;
            Properties.Settings.Default.Save();
            {
                var v = VersionList.SelectedItem as Version;
                switch (v.DisplayInstallStatus.ToString())
                {
                    case "Not installed":
                        InvokeDownload(VersionList.SelectedItem as Version);
                        ProgressBarGrid.Visibility = Visibility.Visible;
                        //MainPlayButton.IsEnabled = false;
                        break;
                    case "Installed":
                        InvokeLaunch(VersionList.SelectedItem as Version);
                        break;
                }
            }
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LanguageChange("en-US");
            Properties.Settings.Default.Language = "ru-RU";
            Properties.Settings.Default.Save();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            // Language setting on startup
            switch (Properties.Settings.Default.Language)
            {
                case "none":
                    CultureInfo ci = CultureInfo.InstalledUICulture; // get system locale
                    switch (ci.Name)
                    {
                        default:
                            Debug.WriteLine("default language");
                            Properties.Settings.Default.Language = "default";
                            Properties.Settings.Default.Save();
                            break;
                        case "ru-RU":
                            LanguageChange(ci.Name);
                            Properties.Settings.Default.Language = "ru-RU";
                            Properties.Settings.Default.Save();
                            break;
                        case "en-US":
                            LanguageChange(ci.Name);
                            Properties.Settings.Default.Language = "en-US";
                            Properties.Settings.Default.Save();
                            break;
                    }
                    break;
                case "ru-RU":
                    LanguageChange("ru-RU");
                    break;

                case "en-US":
                    LanguageChange("en-US");
                    break;

                default:
                    break;

            }
        }
    }

    namespace WPFDataTypes
    {
        public class NotifyPropertyChangedBase : INotifyPropertyChanged
        {

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string name)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
            public void ProgressBarIsIndeterminate(bool isIndeterminate)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ((MainWindow)Application.Current.MainWindow).progressSizeHack.IsIndeterminate = isIndeterminate;
                });
            }
            public void ProgressBarUpdate(double downloadedBytes, double totalSize)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ((MainWindow)Application.Current.MainWindow).progressSizeHack.Value = downloadedBytes;
                    ((MainWindow)Application.Current.MainWindow).progressSizeHack.Maximum = totalSize;
                    int.Parse(downloadedBytes.ToString());
                    ((MainWindow)Application.Current.MainWindow).ProgressBarText.Text = (Math.Round(downloadedBytes / 1024 / 1024, 2)).ToString() + " MB / " + (Math.Round(totalSize / 1024 / 1024, 2)).ToString() + " MB";
                });
            }

        }

        public interface ICommonVersionCommands
        {

            ICommand LaunchCommand { get; }

            ICommand DownloadCommand { get; }

            ICommand RemoveCommand { get; }

        }
        public class Versions : List<Object>
        {
        }
            public class Version : NotifyPropertyChangedBase
        {
            public Version(string uuid, string name, bool isBeta, ICommonVersionCommands commands)
            {
                this.UUID = uuid;
                this.Name = name;
                this.IsBeta = isBeta;
                this.DownloadCommand = commands.DownloadCommand;
                this.LaunchCommand = commands.LaunchCommand;
                this.RemoveCommand = commands.RemoveCommand;
            }

            public string UUID { get; set; }
            public string Name { get; set; }
            public bool IsBeta { get; set; }

            public string GameDirectory => "Minecraft-" + Name;

            public bool IsInstalled => Directory.Exists(GameDirectory);

            public string DisplayName
            {
                get
                {
                    return Name + (IsBeta ? " (beta)" : "");
                }
            }
            public string DisplayInstallStatus
            {
                get
                {
                    return IsInstalled ? "Installed" : "Not installed";
                }
            }

            public ICommand LaunchCommand { get; set; }
            public ICommand DownloadCommand { get; set; }
            public ICommand RemoveCommand { get; set; }

            private VersionStateChangeInfo _stateChangeInfo;
            public VersionStateChangeInfo StateChangeInfo
            {
                get { return _stateChangeInfo; }
                set { _stateChangeInfo = value; OnPropertyChanged("StateChangeInfo"); OnPropertyChanged("IsStateChanging"); }
            }

            public bool IsStateChanging => StateChangeInfo != null;

            public void UpdateInstallStatus()
            {
                OnPropertyChanged("IsInstalled");
            }

        }

        public class VersionStateChangeInfo : NotifyPropertyChangedBase
        {

            private bool _isInitializing;
            private bool _isExtracting;
            private bool _isUninstalling;
            private bool _isLaunching;
            public long _downloadedBytes;
            private long _totalSize;

            public bool IsInitializing
            {
                get { return _isInitializing; }
                set { _isInitializing = value; ProgressBarIsIndeterminate(_isInitializing); OnPropertyChanged("IsProgressIndeterminate"); OnPropertyChanged("DisplayStatus"); }
            }

            public bool IsExtracting
            {
                get { return _isExtracting; }
                set { _isExtracting = value; ProgressBarIsIndeterminate(_isExtracting); OnPropertyChanged("IsProgressIndeterminate"); OnPropertyChanged("DisplayStatus"); }
            }

            public bool IsUninstalling
            {
                get { return _isUninstalling; }
                set { _isUninstalling = value; ProgressBarIsIndeterminate(_isUninstalling); OnPropertyChanged("IsProgressIndeterminate"); OnPropertyChanged("DisplayStatus"); }
            }

            public bool IsLaunching
            {
                get { return _isLaunching; }
                set { _isLaunching = value; ProgressBarIsIndeterminate(_isLaunching); OnPropertyChanged("IsProgressIndeterminate"); OnPropertyChanged("DisplayStatus"); }
            }

            public bool IsProgressIndeterminate
            {
                get { return IsInitializing || IsExtracting || IsUninstalling || IsLaunching; }
            }

            public long DownloadedBytes
            {
                get { return _downloadedBytes; }
                set { _downloadedBytes = value; ProgressBarUpdate(_downloadedBytes, _totalSize); OnPropertyChanged("DownloadedBytes"); OnPropertyChanged("DisplayStatus"); }
            }

            public long TotalSize
            {
                get { return _totalSize; }
                set { _totalSize = value; OnPropertyChanged("TotalSize"); OnPropertyChanged("DisplayStatus"); }
            }

            public string DisplayStatus
            {
                get
                {
                    if (IsInitializing)
                        return "Downloading...";
                    if (IsExtracting)
                        return "Extracting...";
                    if (IsUninstalling)
                        return "Uninstalling...";
                    if (IsLaunching)
                        return "Launching...";
                    return (DownloadedBytes / 1024 / 1024) + "MiB/" + (TotalSize / 1024 / 1024) + "MiB";
                }
            }

            public ICommand CancelCommand { get; set; }

        }

    }
}
