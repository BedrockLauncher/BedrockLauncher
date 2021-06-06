using System;
using System.Windows;
using System.Windows.Controls;

namespace BL_Core.Pages.Common
{
    /// <summary>
    /// Логика взаимодействия для ErrorScreen.xaml
    /// </summary>
    public partial class ErrorScreen : Page
    {

        public IDialogHander Handler { get; private set; }

        public ErrorScreen()
        {
            InitializeComponent();
        }

        public ErrorScreen(IDialogHander _hander)
        {
            InitializeComponent();
            Handler = _hander;
        }
        private void ErrorScreenCloseButton_Click(object sender, RoutedEventArgs e)
        {
            // As i understand it not only hide error screen overlay, but also clear it from memory
            Handler.SetDialogFrame(null);
        }

        private void ErrorScreenViewCrashButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("notepad.exe", $@"{Environment.CurrentDirectory}\Log.txt");
        }
    }
    public static class ErrorScreenShow
    {
        public static IDialogHander Handler { get; private set; }

        public static void SetHandler(IDialogHander _hander)
        {
            Handler = _hander;
        }

        public static void exceptionmsg(string title, Exception error = null)
        {
            Application.Current.Dispatcher.Invoke(() => {
                ErrorScreen errorScreen = new ErrorScreen(Handler);
                // Show default error message
                if (error == null)
                {
                    Handler.SetDialogFrame(new ErrorScreen(Handler));
                }
                errorScreen.ErrorType.Text = title;
                errorScreen.ErrorText.Text = error.Message;
                Handler.SetDialogFrame(errorScreen);
            });

        }
        public static void exceptionmsg(Exception error = null)
        {
            Application.Current.Dispatcher.Invoke(() => {
                ErrorScreen errorScreen = new ErrorScreen(Handler);
                // Show default error message
                if (error == null)
                {
                    Handler.SetDialogFrame(new ErrorScreen(Handler));
                }
                errorScreen.ErrorType.Text = error.HResult.ToString();
                errorScreen.ErrorText.Text = error.Message;
                Handler.SetDialogFrame(errorScreen);
            });

        }

        public static void errormsg(string error = null)
        {
            Application.Current.Dispatcher.Invoke(() => {
                ErrorScreen errorScreen = new ErrorScreen(Handler);
                // Show default error message
                if (error == null)
                {
                    Handler.SetDialogFrame(new ErrorScreen(Handler));
                }
                switch (error)
                {
                    case "autherror":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_AuthenticationFailed_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_AuthenticationFailed");
                        Handler.SetDialogFrame(errorScreen);
                        break;
                    case "CantFindJavaLauncher":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_CantFindJavaLauncher_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_CantFindJavaLauncher");
                        Handler.SetDialogFrame(errorScreen);
                        break;
                    case "CantFindExternalLauncher":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_CantFindExternalLauncher_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_CantFindExternalLauncher");
                        Handler.SetDialogFrame(errorScreen);
                        break;
                    case "appregistererror":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_AppReregisterFailed_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_AppReregisterFailed");
                        Handler.SetDialogFrame(errorScreen);
                        break;
                    case "applauncherror":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_AppLaunchFailed_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_AppLaunchFailed");
                        Handler.SetDialogFrame(errorScreen);
                        break;
                    case "downloadfailederror":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_AppDownloadFailed_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_AppDownloadFailed");
                        Handler.SetDialogFrame(errorScreen);
                        break;
                    case "notaskinpack":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_NotaSkinPack_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_NotaSkinPack");
                        Handler.SetDialogFrame(errorScreen);
                        break;
                    case "extractionfailed":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_AppExtractionFailed_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_AppExtractionFailed");
                        Handler.SetDialogFrame(errorScreen);
                        break;
                    case "CantFindPaidServerList":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_CantFindPaidServerList_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_CantFindPaidServerList");
                        Handler.SetDialogFrame(errorScreen);
                        break;
                }
            });

        }
    }
}
