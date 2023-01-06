using System;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using PostSharp.Patterns.Model;
using BedrockLauncher.Handlers;
using BedrockLauncher.Core.Pages.Common;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.UI.Interfaces;
using BedrockLauncher.UI.Components;

namespace BedrockLauncher.ViewModels
{

    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]    //119 Lines
    public class MainViewModel : IDialogHander, ILauncherModel
    {
        public static MainViewModel Default { get; set; } = new MainViewModel();

        #region Init

        public MainViewModel()
        {
            ErrorScreenShow.SetHandler(this);
            DialogPrompt.SetHandler(this);
            UI.ViewModels.MainViewModel.SetHandler(this);
        }

        #endregion

        #region Properties

        public static UpdateHandler Updater { get; set; } = new UpdateHandler();
        public ProgressBarModel ProgressBarState { get; set; } = new ProgressBarModel();
        public PathHandler FilePaths { get; private set; } = new PathHandler();
        public PackageHandler PackageManager { get; set; } = new PackageHandler();
        public MCProfilesList Config { get; private set; } = new MCProfilesList();
        public ObservableCollection<MCVersion> Versions { get; private set; } = new ObservableCollection<MCVersion>();


        private bool AllowedToCloseWithGameOpen { get; set; } = false;
        public bool IsVersionsUpdating { get; private set; }
        public KeyboardNavigationMode MainFrame_TabNavigationMode { get; set; } = KeyboardNavigationMode.Continue;


        #endregion

        #region Methods

        public async Task LoadVersions(bool onLoad = false)
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                if (IsVersionsUpdating) return;
                IsVersionsUpdating = true;

                await PackageManager.VersionDownloader.UpdateVersions(Versions, onLoad);

                IsVersionsUpdating = false;
            });

        }
        public void LoadConfig()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Config = MCProfilesList.Load(FilePaths.GetProfilesFilePath(), Properties.LauncherSettings.Default.CurrentProfile, Properties.LauncherSettings.Default.CurrentProfile);
            });
        }
        public async void KillGame() => await PackageManager.ClosePackage();
        public async void RepairVersion(MCVersion v) => await PackageManager.DownloadPackage(v);
        public async void RemoveVersion(MCVersion v) => await PackageManager.RemovePackage(v);
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
            
            await PackageManager.LaunchPackage(i.Version, MainViewModel.Default.FilePaths.GetInstallationsFolderPath(p.Name, i.DirectoryName_Full), KeepLauncherOpen);
        }

        #endregion

        #region Dialog

        public void AttemptClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Action action = new Action(() => MainWindow.Close());

            bool doNotClose = Properties.LauncherSettings.Default.KeepLauncherOpen && 
                MainViewModel.Default.PackageManager.isGameRunning && !AllowedToCloseWithGameOpen;

            if (doNotClose) { e.Cancel = true; LauncherCanNotCloseDialog(action); }
            else e.Cancel = false;
        }
        public void SetOverlayFrame(object _content, bool _isStrict = false)
        {
            bool _animate = (_isStrict ? false : Properties.LauncherSettings.Default.AnimatePageTransitions);
            SetOverlayFrame_Base(_content, _animate);

            void SetOverlayFrame_Base(object content, bool animate)
            {
                bool isEmpty = content == null;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var focusMode = (isEmpty ? MainFrame_TabNavigationMode : KeyboardNavigationMode.None);
                    KeyboardNavigation.SetTabNavigation(MainWindow.MainFrame, focusMode);
                    Keyboard.ClearFocus();
                });

                PageAnimator.FrameSet_Overlay(OverlayFrame, content, animate);
            }
        }
        public void SetDialogFrame(object content)
        {
            bool isEmpty = content == null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var focusMode = (isEmpty ? MainFrame_TabNavigationMode : KeyboardNavigationMode.None);
                KeyboardNavigation.SetTabNavigation(MainWindow.MainFrame, focusMode);
                KeyboardNavigation.SetTabNavigation(MainWindow.OverlayFrame, focusMode);
                Keyboard.ClearFocus();
            });

            PageAnimator.FrameSet_Dialog(ErrorFrame, content);
        }
        public async Task ShowWaitingDialog(Func<Task> action)
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                SetDialogFrame(new WaitingPage());
                await action();
                SetDialogFrame(null);
            });
        }
        public async void LauncherCanNotCloseDialog(Action successAction)
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
        
        public Controls.Various.UpdateButton UpdateButton
        {
            get { Depends.On(MainWindow); return MainWindow.UpdateButton; }
        }
        public DependencyObject ProgressBarGrid
        {
            get { Depends.On(MainWindow); return MainWindow.ProgressBarGrid; }
        }

        [SafeForDependencyAnalysis]
        private MainWindow MainWindow
        {
            get => App.Current.Dispatcher.Invoke(() => (MainWindow)App.Current.MainWindow);
        }

        #endregion
    }
}
