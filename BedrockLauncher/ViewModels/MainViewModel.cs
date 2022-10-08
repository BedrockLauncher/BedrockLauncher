using System;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using BedrockLauncher.Controls;
using PostSharp.Patterns.Model;
using BedrockLauncher.Handlers;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.UI.Interfaces;
using BedrockLauncher.UI.Components;
using System.Windows.Threading;

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
        public BLProfileList Config { get; private set; } = new BLProfileList();
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

                await PackageManager.VersionDownloader.UpdateVersionList(Versions, onLoad);

                IsVersionsUpdating = false;
            });

        }
        public void LoadConfig()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Config = BLProfileList.Load(FilePaths.GetProfilesFilePath(), Properties.LauncherSettings.Default.CurrentProfileUUID, Properties.LauncherSettings.Default.CurrentProfileUUID);
            });
        }
        public async void KillGame() => await PackageManager.ClosePackage();
        public async void RepairVersion(MCVersion v) => await PackageManager.DownloadPackage(v);
        public async void RemoveVersion(MCVersion v) => await PackageManager.RemovePackage(v);
        public async void Play(BLProfile p, BLInstallation i, bool KeepLauncherOpen, bool Save = true)
        {
            if (i == null) return;

            i.LastPlayed = DateTime.Now;
            MainViewModel.Default.Config.Installation_UpdateLP(i);

            if (Save)
            {
                Properties.LauncherSettings.Default.CurrentInstallationUUID = i.InstallationUUID;
                Properties.LauncherSettings.Default.Save();
            }


            var Version = i.Version;
            var Path = MainViewModel.Default.FilePaths.GetInstallationPackageDataPath(p.UUID, i.DirectoryName_Full);

            await PackageManager.InstallPackage(Version, Path);
            await PackageManager.LaunchPackage(Version, Path, KeepLauncherOpen);
        }

        public async void Install(BLProfile p, BLInstallation i)
        {
            if (i == null) return;

            var Version = i.Version;
            var Path = MainViewModel.Default.FilePaths.GetInstallationPackageDataPath(p.UUID, i.DirectoryName_Full);

            await PackageManager.InstallPackage(Version, Path);
        }

        #endregion

        #region Dialog


        public object ErrorFrame_Content { get; set; }
        public object OverlayFrame_Content { get; set; }

        public bool MainFrame_isEnabled
        {
            get
            {
                Depends.On(ErrorFrame_Content);
                Depends.On(OverlayFrame_Content);
                return ErrorFrame_Content == null && OverlayFrame_Content == null;
            }
        }
        public bool OverlayFrame_isEnabled
        {
            get
            {
                Depends.On(ErrorFrame_Content);
                return ErrorFrame_Content == null;
            }
        }

        public bool IsErrorDialogEmpty()
        {
            return ErrorFrame_Content == null;
        }

        public void AttemptClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Action action = new Action(() => MainWindow.Close());

            bool doNotClose = Properties.LauncherSettings.Default.KeepLauncherOpen && 
                MainViewModel.Default.PackageManager.isGameRunning && !AllowedToCloseWithGameOpen;

            if (doNotClose) { e.Cancel = true; LauncherCanNotCloseDialog(action); }
            else e.Cancel = false;
        }
        public void SetOverlayFrame(object content, bool isStrict = false)
        {
            bool animate = (isStrict ? false : Properties.LauncherSettings.Default.AnimatePageTransitions);
            Application.Current.Dispatcher.Invoke(Keyboard.ClearFocus);
            PageAnimator.FrameSet_Overlay(OverlayFrame, content, animate);
        }
        public void SetDialogFrame(object content)
        {
            Application.Current.Dispatcher.Invoke(Keyboard.ClearFocus);
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
                var title = BedrockLauncher.Localization.Language.LanguageManager.GetResource("Dialog_CloseGame_Title") as string;
                var content = BedrockLauncher.Localization.Language.LanguageManager.GetResource("Dialog_CloseGame_Text") as string;

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

        public Component_UpdateButton UpdateButton
        {
            get { Depends.On(MainWindow); return MainWindow.UpdateButton; }
        }
        public DependencyObject ProgressBarGrid
        {
            get { Depends.On(MainWindow); return MainWindow.MainPage.ProgressBarGrid; }
        }

        [SafeForDependencyAnalysis]
        private MainWindow MainWindow
        {
            get => App.Current.Dispatcher.Invoke(() => (MainWindow)App.Current.MainWindow);
        }

        #endregion
    }
}
