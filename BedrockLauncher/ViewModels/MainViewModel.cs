using System;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using PostSharp.Patterns.Model;
using BedrockLauncher.Handlers;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.UI.Interfaces;
using BedrockLauncher.UI.Components;
using System.Windows.Threading;
using BedrockLauncher.Backend.Backporting;
using BedrockLauncher.Pages.General;

namespace BedrockLauncher.ViewModels
{

    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]    //119 Lines
    public class MainViewModel : IDialogHander, ILauncherModel, IBackwardsCommunication
    {
        public static MainViewModel Default { get; set; } = new MainViewModel();

        #region Init

        public MainViewModel()
        {
            MainDataModel.SetBackwardsCommunicationHost(this);
            ErrorScreenShow.SetHandler(this);
            DialogPrompt.SetHandler(this);
            UI.ViewModels.MainViewModel.SetHandler(this);
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
                MainDataModel.Default.PackageManager.isGameRunning && !MainDataModel.Default.AllowedToCloseWithGameOpen;

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
                    await MainDataModel.Default.PackageManager.ClosePackage();
                    MainDataModel.Default.AllowedToCloseWithGameOpen = true;
                    if (successAction != null) successAction.Invoke();
                }
                else if (result == System.Windows.Forms.DialogResult.No)
                {
                    MainDataModel.Default.AllowedToCloseWithGameOpen = true;
                    if (successAction != null) successAction.Invoke();
                }
                else if (result == System.Windows.Forms.DialogResult.Cancel)
                {
                   MainDataModel.Default.AllowedToCloseWithGameOpen = false;
                }

            }));
        }

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

        [SafeForDependencyAnalysis]
        private MainWindow MainWindow
        {
            get => App.Current.Dispatcher.Invoke(() => (MainWindow)App.Current.MainWindow);
        }

        #endregion

        #region Backwards Communication

        public DependencyObject ProgressBarGrid
        {
            get { Depends.On(MainWindow); return MainWindow.MainPage.ProgressBarGrid; }
        }
        public async Task<System.Windows.Forms.DialogResult> ShowDialog_YesNo(string title, string content)
        {
            return await MainViewModel.Default.ShowDialog_YesNo(title, content);
        }
        public void errormsg(string dialogTitle, string dialogText, Exception ex2)
        {
            ErrorScreenShow.errormsg(dialogTitle, dialogText, ex2);
        }
        public Task<bool> exceptionmsg(Exception ex)
        {
            return ErrorScreenShow.exceptionmsg(ex);
        }
        public void UpdateAnimatePageTransitions(bool value)
        {
            Navigator.AnimatePageTransitions = value;
        }

        #endregion
    }
}
