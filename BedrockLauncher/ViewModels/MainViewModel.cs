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
using System.Windows.Threading;
using BedrockLauncher.Extensions;
using BedrockLauncher.UpdateProcessor;

namespace BedrockLauncher.ViewModels
{

    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.ExcludeExplicitProperties)]    //119 Lines
    public class MainViewModel : IDialogHander, ILauncherModel
    {
        public static MainViewModel Default { get; set; } = new MainViewModel();

        #region Event Handlers

        /*
        public event EventHandler ConfigUpdated;
        protected virtual void OnConfigUpdated(PropertyChangedEventArgs e)
        {
            EventHandler handler = ConfigUpdated;
            if (e.PropertyName == nameof(Config.CurrentInstallations))
                if (handler != null) handler(this, e);
        }
        */

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
        public MCProfilesList Config { get; private set; } = new MCProfilesList();
        public ObservableCollection<BLVersion> Versions { get; private set; } = new ObservableCollection<BLVersion>();


        private bool AllowedToCloseWithGameOpen { get; set; } = false;
        public bool IsVersionsUpdating { get; private set; }
        private KeyboardNavigationMode KeyboardNavigationMode { get; set; }


        #endregion

        #region Methods

        public void LoadVersions()
        {

            if (IsVersionsUpdating) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsVersionsUpdating = true;
                new WaitingScreen().ShowDialogUntilTaskCompletion(MainViewModel.Default.PackageManager.VersionDownloader.UpdateVersions(Versions));
                IsVersionsUpdating = false;
            });
        }
        public void LoadConfig()
        {
            Config = MCProfilesList.Load(MainViewModel.Default.FilePaths.GetProfilesFilePath(), Properties.LauncherSettings.Default.CurrentProfile, Properties.LauncherSettings.Default.CurrentProfile);
        }
        public void AttemptClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Action action = new Action(() =>
            {
                MainWindow.Close();
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
                var title = Application.Current.FindResource("Dialog_CloseGame_Title") as string;
                var content = Application.Current.FindResource("Dialog_CloseGame_Text") as string;

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
                KeyboardNavigation.SetTabNavigation(MainWindow.MainFrame, focusMode);
                KeyboardNavigation.SetTabNavigation(MainWindow.OverlayFrame, focusMode);
                Keyboard.ClearFocus();
            });

            if (animate)
            {
                if (isEmpty) PageAnimator.FrameFadeOut(MainWindow.ErrorFrame, content);
                else PageAnimator.FrameFadeIn(MainWindow.ErrorFrame, content);
            }
            else PageAnimator.Navigate(MainWindow.ErrorFrame, content);
        }
        private void SetOverlayFrame_Base(object content, bool animate)
        {
            bool isEmpty = content == null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var focusMode = (isEmpty ? KeyboardNavigationMode : KeyboardNavigationMode.None);
                KeyboardNavigation.SetTabNavigation(MainWindow.MainFrame, focusMode);
                Keyboard.ClearFocus();
            });

            if (animate)
            {
                if (isEmpty) PageAnimator.FrameSwipe_OverlayOut(MainWindow.OverlayFrame, content);
                else PageAnimator.FrameSwipe_OverlayIn(MainWindow.OverlayFrame, content);
            }
            else PageAnimator.Navigate(MainWindow.OverlayFrame, content);
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



        #endregion

        #region Extensions

        public Frame ErrorFrame
        {
            get { Depends.On(MainWindow); return MainWindow.ErrorFrame; }
        }

        public Frame OverlayFrame
        {
            get { Depends.On(MainWindow); return MainWindow.OverlayFrame; }
        }
        
        public Controls.Misc.UpdateButton UpdateButton
        {
            get { Depends.On(MainWindow); return MainWindow.UpdateButton; }
        }

        [SafeForDependencyAnalysis]
        private MainWindow MainWindow
        {
            get => App.Current.Dispatcher.Invoke(() => (MainWindow)App.Current.MainWindow);
        }

        #endregion
    }
}
