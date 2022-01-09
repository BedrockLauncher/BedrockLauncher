using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using System.Windows;
using System.Windows.Input;
using BedrockLauncher.Methods;
using System.Windows.Controls;
using BedrockLauncher.Pages;
using BedrockLauncher.Pages.Community;
using BedrockLauncher.Pages.Settings;
using BedrockLauncher.Pages.News;
using BedrockLauncher.Pages.Play;
using System.Windows.Data;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.Pages.FirstLaunch;
using System.Windows.Media.Animation;
using BedrockLauncher.Components;
using BedrockLauncher.Pages.Common;
using BedrockLauncher.Interfaces;
using BedrockLauncher.Downloaders;
using BedrockLauncher.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using PostSharp.Patterns.Model;
using BedrockLauncher.Enums;
using BedrockLauncher.Handlers;

namespace BedrockLauncher.ViewModels
{
    [NotifyPropertyChanged]
    public class MainViewModel : IDialogHander, ILauncherModel
    {
        public static MainViewModel Default { get; set; } = new MainViewModel();

        #region Event Handlers

        public event EventHandler ConfigUpdated;
        protected virtual void OnConfigUpdated(PropertyChangedEventArgs e)
        {
            EventHandler handler = ConfigUpdated;
            if (e.PropertyName == nameof(Config.CurrentInstallations))
                if (handler != null) handler(this, e);
        }

        #endregion

        #region Init

        public MainViewModel()
        {
            ErrorScreenShow.SetHandler(this);
            DialogPrompt.SetHandler(this);
        }
        public void Init(Grid MainFrame)
        {
            KeyboardNavigationMode = KeyboardNavigation.GetTabNavigation(MainFrame);
        }

        #endregion

        #region Properties

        public static UpdateHandler Updater { get; set; } = new UpdateHandler();
        public UserInterfaceModel ProgressBarState { get; set; } = new UserInterfaceModel();
        public PathHandler FilePaths { get; private set; } = new PathHandler();
        public PackageHandler PackageManager { get; set; } = new PackageHandler();



        private bool AllowedToCloseWithGameOpen { get; set; } = false;
        private KeyboardNavigationMode KeyboardNavigationMode { get; set; }
        public string CurrentInstallationUUID
        {
            get
            {
                Depends.On(CurrentProfileUUID);
                return Config.CurrentInstallationUUID;
            }
            set
            {
                Config.CurrentInstallationUUID = value;
            }
        }
        public string CurrentProfileUUID
        {
            get
            {
                return Config.CurrentProfileUUID;
            }
            set
            {
                Config.CurrentProfileUUID = value;
            }
        }
        public ObservableCollection<BLVersion> Versions { get; private set; } = new ObservableCollection<BLVersion>();
        public MCProfilesList Config { get; private set; } = new MCProfilesList();
        public SortBy_Installation Installations_SortFilter { get; set; } = SortBy_Installation.LatestPlayed;
        public string Installations_SearchFilter { get; set; } = string.Empty;
        public bool IsVersionsUpdating { get; set; }

        #endregion

        #region Methods

        public async Task LoadVersions()
        {
            if (IsVersionsUpdating) return;
            await Application.Current.Dispatcher.Invoke((Func<Task>)(async () =>
            {
                IsVersionsUpdating = true;
                await MainViewModel.Default.PackageManager.VersionDownloader.UpdateVersions(Versions);
                IsVersionsUpdating = false;
            }));
        }
        public void LoadConfig()
        {
            Config.PropertyChanged -= (sender, e) => OnConfigUpdated(e);
            Config = MCProfilesList.Load(MainViewModel.Default.FilePaths.GetProfilesFilePath(), Properties.LauncherSettings.Default.CurrentProfile, Properties.LauncherSettings.Default.CurrentProfile);
            Config.PropertyChanged += (sender, e) => OnConfigUpdated(e);
        }
        public void AttemptClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Action action = new Action(() =>
            {
                MainThread.Close();
            });

            if (Properties.LauncherSettings.Default.KeepLauncherOpen && MainViewModel.Default.PackageManager.isGameRunning)
            {
                if (!AllowedToCloseWithGameOpen)
                {
                    e.Cancel = true;
                    ShowPrompt_ClosingWithGameStillOpened(action);
                }
            }
            else
            {
                e.Cancel = false;
            }
        }
        public void SetOverlayFrame_Strict(object content)
        {
            SetOverlayFrame_Base(content, false);
        }
        public void SetOverlayFrame(object content)
        {
            bool animate = Properties.LauncherSettings.Default.AnimatePageTransitions;
            SetOverlayFrame_Base(content, animate);
        }
        public async void ShowPrompt_ClosingWithGameStillOpened(Action successAction)
        {
            await Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(async () =>
            {
                var title = MainThread.FindResource("Dialog_CloseGame_Title") as string;
                var content = MainThread.FindResource("Dialog_CloseGame_Text") as string;

                var result = await DialogPrompt.ShowDialog_YesNoCancel(title, content);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    await MainViewModel.Default.PackageManager.ClosePackage();
                    AllowedToCloseWithGameOpen = true;
                    if (successAction != null) successAction.Invoke();
                }
                else if (result == System.Windows.Forms.DialogResult.No)
                {
                    AllowedToCloseWithGameOpen = true;
                    if (successAction != null) successAction.Invoke();
                }
                else if (result == System.Windows.Forms.DialogResult.Cancel)
                {
                    AllowedToCloseWithGameOpen = false;
                }

            }));
        }
        public void SetDialogFrame(object content)
        {
            bool animate = Properties.LauncherSettings.Default.AnimatePageTransitions;
            bool isEmpty = content == null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var focusMode = (isEmpty ? KeyboardNavigationMode : KeyboardNavigationMode.None);
                KeyboardNavigation.SetTabNavigation(MainThread.MainFrame, focusMode);
                KeyboardNavigation.SetTabNavigation(MainThread.OverlayFrame, focusMode);
                Keyboard.ClearFocus();
            });

            if (animate)
            {
                if (isEmpty) PageAnimator.FrameFadeOut(MainThread.ErrorFrame, content);
                else PageAnimator.FrameFadeIn(MainThread.ErrorFrame, content);
            }
            else PageAnimator.Navigate(MainThread.ErrorFrame, content);
        }
        private void SetOverlayFrame_Base(object content, bool animate)
        {
            bool isEmpty = content == null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var focusMode = (isEmpty ? KeyboardNavigationMode : KeyboardNavigationMode.None);
                KeyboardNavigation.SetTabNavigation(MainThread.MainFrame, focusMode);
                Keyboard.ClearFocus();
            });

            if (animate)
            {
                if (isEmpty) PageAnimator.FrameSwipe_OverlayOut(MainThread.OverlayFrame, content);
                else PageAnimator.FrameSwipe_OverlayIn(MainThread.OverlayFrame, content);
            }
            else PageAnimator.Navigate(MainThread.OverlayFrame, content);
        }
        public async void KillGame() => await PackageManager.ClosePackage();
        public async void RepairVersion(BLVersion v) => await PackageManager.DownloadAndExtractPackage(v);
        public async void RemoveVersion(BLVersion v) => await PackageManager.RemovePackage(v);
        public async void Play(MCProfile p, BLInstallation i, bool KeepLauncherOpen, bool Save = true)
        {
            if (i == null) return;

            i.LastPlayed = DateTime.Now;
            MainViewModel.Default.Config.Installation_UpdateLP(i);

            if (Save)
            {
                Properties.LauncherSettings.Default.CurrentInstallation = i.InstallationUUID;
                Properties.LauncherSettings.Default.Save();
            }

            bool wasCanceled = false;
            if (i.Version.DisplayInstallStatus == "Not installed") await PackageManager.DownloadAndExtractPackage(i.Version);
            if (!wasCanceled) await PackageManager.LaunchPackage(i.Version, MainViewModel.Default.FilePaths.GetInstallationsFolderPath(p.Name, i.DirectoryName_Full), KeepLauncherOpen);
        }

        #endregion

        #region Filters/Sorters

        public bool Filter_InstallationList(object obj)
        {
            BLInstallation v = obj as BLInstallation;
            if (v == null) return false;
            else if (!Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return false;
            else if (!Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return false;
            else if (!v.DisplayName.Contains(Installations_SearchFilter)) return false;
            else return true;
        }
        public void Sort_InstallationList(ref CollectionView view)
        {
            view.SortDescriptions.Clear();
            if (Installations_SortFilter == SortBy_Installation.LatestPlayed) view.SortDescriptions.Add(new System.ComponentModel.SortDescription("LastPlayedT", System.ComponentModel.ListSortDirection.Descending));
            if (Installations_SortFilter == SortBy_Installation.Name) view.SortDescriptions.Add(new System.ComponentModel.SortDescription("DisplayName", System.ComponentModel.ListSortDirection.Ascending));
        }
        public bool Filter_VersionList(object obj)
        {
            BLVersion v = BLVersion.Convert(obj as MCVersion);

            if (v != null && v.IsInstalled)
            {
                if (!Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return false;
                else if (!Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return false;
                else return true;
            }
            else return false;

        }

        #endregion

        #region Extensions

        public static MainWindow MainThread
        {
            get
            {
                return App.Current.Dispatcher.Invoke(() =>
                {
                    return (MainWindow)App.Current.MainWindow;
                });
            }
        }

        #endregion
    }
}
