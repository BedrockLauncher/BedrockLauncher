using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms.Design;
using Newtonsoft.Json;
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
using BedrockLauncher.Interfaces;
using BedrockLauncher.Methods;
using BedrockLauncher.Classes;
using BedrockLauncher.Pages.MainScreen;
using BedrockLauncher.Pages.NoContentScreen;
using BedrockLauncher.Pages.PlayScreen;
using BedrockLauncher.Pages.ServersScreen;
using BedrockLauncher.Pages.SettingsScreen;
using BedrockLauncher.Pages.FirstLaunch;
using BedrockLauncher.Pages.ErrorScreen;
using BedrockLauncher.Pages.InstallationsScreen;
using BedrockLauncher.Pages.NewsScreen;
using BedrockLauncher.Pages.ProfileManagementScreen;
using ServerTab;

using Version = BedrockLauncher.Classes.Version;

namespace BedrockLauncher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window, ICommonVersionCommands
    {
        #region Definitions
        private static readonly string MINECRAFT_PACKAGE_FAMILY = "Microsoft.MinecraftUWP_8wekyb3d8bbwe";

        private bool IsDownloading = false;

        private VersionList _versions;
        private readonly VersionDownloader _anonVersionDownloader = new VersionDownloader();
        private readonly VersionDownloader _userVersionDownloader = new VersionDownloader();
        private readonly Task _userVersionDownloaderLoginTask;
        private volatile int _userVersionDownloaderLoginTaskStarted;
        private volatile bool _hasLaunchTask = false;

        // wil be removed and rewritten
        private BetterBedrockMain betterBedrockMain = new BetterBedrockMain();

        // updater to check for updates (loaded in mainwindow init)
        private static Updater updater = new Updater();

        // servers dll stuff
        private static ServersTab serversTab = new ServersTab();

        // load pages to not create new in memory after
        private MainPage mainPage = new MainPage();
        private GeneralSettingsPage generalSettingsPage = new GeneralSettingsPage();
        private SettingsScreen settingsScreenPage = new SettingsScreen();
        private NewsScreenPage newsScreenPage = new NewsScreenPage(updater);
        private NoContentPage noContentPage = new NoContentPage();
        private PlayScreenPage playScreenPage = new PlayScreenPage();
        private InstallationsScreen installationsScreen = new InstallationsScreen();
        private ServersScreenPage serversScreenPage = new ServersScreenPage(serversTab);
        #endregion

        #region Init
        public MainWindow()
        {
            InitializeComponent();
            Panel.SetZIndex(MainWindowOverlayFrame, 0);
            //serversTab.readServers();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            // show first launch window if no profile
            if (Properties.Settings.Default.CurrentProfile == "")
            {
                MainWindowOverlayFrame.Navigate(new WelcomePage());
            }
            ProfileButton.ProfileName.Text = Properties.Settings.Default.CurrentProfile;

            _versions = new VersionList("versions.json", this);

            _userVersionDownloaderLoginTask = new Task(() =>
            {
                _userVersionDownloader.EnableUserAuthorization();
            });

            UpdateVersionsList();
        }

        #endregion

        #region Version Management

        public void UpdateVersionsList()
        {
            VersionList.ItemsSource = _versions;
            installationsScreen.InstallationsList.ItemsSource = _versions;
            var view = CollectionViewSource.GetDefaultView(VersionList.ItemsSource) as CollectionView;
            var view2 = CollectionViewSource.GetDefaultView(installationsScreen.InstallationsList.ItemsSource) as CollectionView;
            view.Filter = VersionListFilter;
            view2.Filter = VersionListFilter;

            Dispatcher.Invoke(async () =>
            {
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

        private bool VersionListFilter(object obj)
        {
            Version v = obj as Version;
            VersionList.SelectedIndex = Properties.Settings.Default.LastSelectedVersion; // show last version in combobox

            if (!Properties.Settings.Default.ShowBetas && v.IsBeta) return false;
            else if (!Properties.Settings.Default.ShowReleases && !v.IsBeta) return false;
            else if (!Properties.Settings.Default.ShowNonInstalled && !v.IsInstalled) return false;
            else return true;

        }

        private void VersionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePlayButton();
        }

        private void VersionList_DropDownClosed(object sender, EventArgs e)
        {
            UpdatePlayButton();
        }

        #endregion

        #region UI

        private void UpdatePlayButton()
        {
            Task.Run(async () =>
            {
                   await Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                   {
                       var selected = VersionList.SelectedItem as Version;
                       if (selected == null)
                       {
                           PlayButtonText.SetResourceReference(TextBlock.TextProperty, "MainPage_PlayButton");
                           MainPlayButton.IsEnabled = false;
                           return;
                       }

                       if (IsDownloading || _hasLaunchTask)
                       {
                           if (IsDownloading) ProgressBarGrid.Visibility = Visibility.Visible;

                           MainPlayButton.IsEnabled = false;
                           VersionList.IsEnabled = false;
                       }
                       else
                       {
                           ProgressBarGrid.Visibility = Visibility.Collapsed;
                           MainPlayButton.IsEnabled = true;
                           VersionList.IsEnabled = true;
                       }


                       if (selected.IsInstalled) PlayButtonText.SetResourceReference(TextBlock.TextProperty, "MainPage_PlayButton");
                       else PlayButtonText.SetResourceReference(TextBlock.TextProperty, "MainPage_PlayButton_InstallNeeded");
                   }));
            });
        }

        public void LanguageChange(string language)
        {
            ResourceDictionary dict = new ResourceDictionary
            {
                Source = new Uri($"..\\Resources\\text\\lang.{language}.xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        #endregion

        #region Installation
        public ICommand LaunchCommand => new RelayCommand((v) => InvokeLaunch((Version)v));

        public ICommand RemoveCommand => new RelayCommand((v) => InvokeRemove((Version)v));

        public ICommand DownloadCommand => new RelayCommand((v) => InvokeDownload((Version)v));

        private void InvokeLaunch(Version v)
        {
            if (_hasLaunchTask)
            {
                UpdatePlayButton();
                return;
            }
            else
            {
                _hasLaunchTask = true;
                UpdatePlayButton();
            }

            Task.Run(async () =>
            {
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
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ErrorScreenShow.errormsg("appregistererror");
                    });
                    _hasLaunchTask = false;
                    UpdatePlayButton();
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
                    UpdatePlayButton();
                    v.StateChangeInfo = null;
                    // close launcher if needed and hide progressbar
                    Application.Current.Dispatcher.Invoke(() => { ((MainWindow)Application.Current.MainWindow).ProgressBarGrid.Visibility = Visibility.Hidden; });
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
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ((MainWindow)Application.Current.MainWindow).ProgressBarGrid.Visibility = Visibility.Hidden;
                        ErrorScreenShow.errormsg("applauncherror");
                    });
                    _hasLaunchTask = false;
                    UpdatePlayButton();
                    v.StateChangeInfo = null;
                    return;
                }
            });
        }

        private async Task DeploymentProgressWrapper(IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> t)
        {
            TaskCompletionSource<int> src = new TaskCompletionSource<int>();
            t.Progress += (v, p) =>
            {
                Debug.WriteLine("Deployment progress: " + p.state + " " + p.percentage + "%");
            };
            t.Completed += (v, p) =>
            {
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
                // somehow this cause errors for some people, idk why, disabled
                //BackupMinecraftDataForRemoval();
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

            Debug.WriteLine("Download start");
            Task.Run(async () =>
            {
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
                    await downloader.Download(v.UUID, "1", dlPath, (current, total) =>
                    {
                        if (v.StateChangeInfo.IsInitializing)
                        {
                            IsDownloading = true;
                            UpdatePlayButton();
                            Debug.WriteLine("Actual download started");
                            v.StateChangeInfo.IsInitializing = false;
                            if (total.HasValue)
                                v.StateChangeInfo.TotalSize = total.Value;
                        }
                        v.StateChangeInfo.DownloadedBytes = current;
                    }, cancelSource.Token);
                    Debug.WriteLine("Download complete");
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Download failed:\n" + e.ToString());
                    if (!(e is TaskCanceledException))
                        MessageBox.Show("Download failed:\n" + e.ToString());
                    v.StateChangeInfo = null;
                    IsDownloading = false;
                    return;
                }
                try
                {
                    Debug.WriteLine("Extraction started");
                    v.StateChangeInfo.IsExtracting = true;
                    string dirPath = v.GameDirectory;
                    if (Directory.Exists(dirPath))
                        Directory.Delete(dirPath, true);
                    ZipFile.ExtractToDirectory(dlPath, dirPath);
                    v.StateChangeInfo = null;
                    File.Delete(Path.Combine(dirPath, "AppxSignature.p7x"));
                    File.Delete(dlPath);
                    Debug.WriteLine("Extracted successfully");
                    InvokeLaunch(v);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Extraction failed:\n" + e.ToString());
                    MessageBox.Show("Extraction failed:\n" + e.ToString());
                    v.StateChangeInfo = null;
                    IsDownloading = false;
                    return;
                }
                v.StateChangeInfo = null;
                v.UpdateInstallStatus();
                UpdatePlayButton();
            });
        }

        private void InvokeRemove(Version v)
        {
            Task.Run(async () =>
            {
                v.StateChangeInfo = new VersionStateChangeInfo();
                v.StateChangeInfo.IsUninstalling = true;
                await UnregisterPackage(Path.GetFullPath(v.GameDirectory));
                Directory.Delete(v.GameDirectory, true);
                v.StateChangeInfo = null;
                v.UpdateInstallStatus();
            });
        }

        #endregion

        #region Misc

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
                        break;
                    case "Installed":
                        InvokeLaunch(VersionList.SelectedItem as Version);
                        break;
                }
            }

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            string[] ConsoleArgs = Environment.GetCommandLineArgs();
            foreach (string argument in ConsoleArgs)
            {
                if (argument.StartsWith("--"))
                {
                    Debug.WriteLine("Recieved argument: " + argument);
                    // hide window
                    if (argument == "--nowindow") { Application.Current.MainWindow.Hide(); }
                }
            }
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
        #endregion

        #region Navigation

        public void ResetButtonManager(ToggleButton toggleButton)
        {
            // just all buttons list
            // ya i know this is really bad, i need to learn mvvm instead of doing this shit
            // but this works fine, at least
            List<ToggleButton> toggleButtons = new List<ToggleButton>() { 
                // mainwindow
                ServersButton,
                NewsButton,
                BedrockEditionButton,
                JavaEditionButton,
                SettingsButton, 
                
                // mainpage (gonna be deleted)
                mainPage.PlayButton,
                mainPage.InstallationsButton,
                mainPage.SkinsButton,
                mainPage.PatchNotesButton,
                
                // settings screen lol
                settingsScreenPage.GeneralButton,
                settingsScreenPage.AccountsButton,
                settingsScreenPage.AboutButton
            };

            foreach (ToggleButton button in toggleButtons) { button.IsChecked = false; }

            PlayScreenBorder.Visibility = Visibility.Hidden;

            toggleButton.IsChecked = true;
        }
        public void ButtonManager(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            ResetButtonManager(toggleButton);

            if (toggleButton.Name == BedrockEditionButton.Name) NavigateToMainPage();
            else if (toggleButton.Name == NewsButton.Name) NavigateToNewsPage();
            else if (toggleButton.Name == JavaEditionButton.Name) NavigateToJavaLauncher();
            else if (toggleButton.Name == ServersButton.Name) NavigateToServersScreen();
            else if (toggleButton.Name == SettingsButton.Name) NavigateToSettings();

            // MainPageButtons
            else if (toggleButton.Name == mainPage.PlayButton.Name) NavigateToPlayScreen();
            else if (toggleButton.Name == mainPage.InstallationsButton.Name) NavigateToInstallationsPage();
            else if (toggleButton.Name == mainPage.SkinsButton.Name) NavigateToSkinsPage();
            else if (toggleButton.Name == mainPage.PatchNotesButton.Name) NavigateToPatchNotes();
            else NavigateToPlayScreen();

        }
        public void NavigateToMainPage(bool rooted = false)
        {
            BedrockEditionButton.IsChecked = true;
            MainWindowFrame.Navigate(mainPage);
            PlayScreenBorder.Visibility = Visibility.Visible;

            if (!rooted)
            {
                if (mainPage.LastButtonName == mainPage.PlayButton.Name) NavigateToPlayScreen();
                else if (mainPage.LastButtonName == mainPage.InstallationsButton.Name) NavigateToInstallationsPage();
                else if (mainPage.LastButtonName == mainPage.SkinsButton.Name) NavigateToSkinsPage();
                else if (mainPage.LastButtonName == mainPage.PatchNotesButton.Name) NavigateToPatchNotes();
            }
        }
        public void NavigateToNewsPage()
        {
            MainWindowFrame.Navigate(newsScreenPage);
        }
        public void NavigateToJavaLauncher()
        {
            try 
            {
                // Trying to find and open java launcher shortcut
                string JavaPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + @"\Programs\Minecraft Launcher\Minecraft Launcher";
                Process.Start(JavaPath);
                Application.Current.MainWindow.Close(); 
            } 
            catch 
            {
                NavigateToPlayScreen();
                ErrorScreenShow.errormsg("CantFindJavaLauncher"); 
            }
        }
        public void NavigateToServersScreen()
        {
            string file = System.IO.Path.Combine(System.Reflection.Assembly.GetEntryAssembly().Location, "servers_paid.json");
            if (File.Exists(file))
            {
                MainWindowFrame.Navigate(serversScreenPage);
                ServersButton.IsChecked = true;
            }
            else
            {
                NavigateToPlayScreen();
                ErrorScreenShow.errormsg("CantFindPaidServerList");
            }
        }
        public void NavigateToSettings()
        {
            settingsScreenPage.GeneralButton.IsChecked = true;
            MainWindowFrame.Navigate(settingsScreenPage);
            settingsScreenPage.SettingsScreenFrame.Navigate(generalSettingsPage);
        }

        public void NavigateToPlayScreen()
        {
            NavigateToMainPage(true);
            mainPage.PlayButton.IsChecked = true;
            mainPage.MainPageFrame.Navigate(playScreenPage);
            mainPage.LastButtonName = mainPage.PlayButton.Name;
        }
        public void NavigateToInstallationsPage()
        {
            NavigateToMainPage(true);
            mainPage.InstallationsButton.IsChecked = true;
            mainPage.MainPageFrame.Navigate(installationsScreen);
            mainPage.LastButtonName = mainPage.InstallationsButton.Name;
        }
        public void NavigateToSkinsPage()
        {
            NavigateToMainPage(true);
            mainPage.SkinsButton.IsChecked = true;
            mainPage.MainPageFrame.Navigate(noContentPage);
            mainPage.LastButtonName = mainPage.SkinsButton.Name;
        }
        public void NavigateToPatchNotes()
        {
            NavigateToMainPage(true);
            mainPage.PatchNotesButton.IsChecked = true;
            mainPage.MainPageFrame.Navigate(noContentPage);
            mainPage.LastButtonName = mainPage.PatchNotesButton.Name;
        }

        public void NavigateToNewProfilePage()
        {
            MainWindowOverlayFrame.Navigate(new AddProfilePage());
        }

        #endregion


    }
}
