using System;
using System.Windows;
using System.Windows.Controls;
using System.Web.UI.WebControls;
using System.Windows.Documents;
using BedrockLauncher.Methods;

namespace BedrockLauncher.Pages.Common
{
    /// <summary>
    /// Логика взаимодействия для ErrorScreen.xaml
    /// </summary>
    public partial class ErrorScreen : Page
    {
        public ErrorScreen()
        {
            InitializeComponent();
        }
        private void ErrorScreenCloseButton_Click(object sender, RoutedEventArgs e)
        {
            // As i understand it not only hide error screen overlay, but also clear it from memory
            ViewModels.LauncherModel.Default.SetDialogFrame(null);
        }

        private void ErrorScreenViewCrashButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("notepad.exe", $@"{Environment.CurrentDirectory}\Log.txt");
        }
    }
    public static class ErrorScreenShow
    {
        public static void exceptionmsg(string title, Exception error = null)
        {
            Application.Current.Dispatcher.Invoke(() => {
                ErrorScreen errorScreen = new ErrorScreen();
                // Show default error message
                if (error == null)
                {
                    ViewModels.LauncherModel.Default.SetDialogFrame(new ErrorScreen());
                }
                errorScreen.ErrorType.Text = title;
                errorScreen.ErrorText.Text = error.Message;
                ViewModels.LauncherModel.Default.SetDialogFrame(errorScreen);
            });

        }
        public static void exceptionmsg(Exception error = null)
        {
            Application.Current.Dispatcher.Invoke(() => {
                ErrorScreen errorScreen = new ErrorScreen();
                // Show default error message
                if (error == null)
                {
                    ViewModels.LauncherModel.Default.SetDialogFrame(new ErrorScreen());
                }
                errorScreen.ErrorType.Text = error.HResult.ToString();
                errorScreen.ErrorText.Text = error.Message;
                ViewModels.LauncherModel.Default.SetDialogFrame(errorScreen);
            });

        }

        public static void errormsg(string error = null)
        {
            Application.Current.Dispatcher.Invoke(() => {
                ErrorScreen errorScreen = new ErrorScreen();
                // Show default error message
                if (error == null)
                {
                    ViewModels.LauncherModel.Default.SetDialogFrame(new ErrorScreen());
                }
                switch (error)
                {
                    case "autherror":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_AuthenticationFailed_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_AuthenticationFailed");
                        ViewModels.LauncherModel.Default.SetDialogFrame(errorScreen);
                        break;
                    case "CantFindJavaLauncher":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_CantFindJavaLauncher_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_CantFindJavaLauncher");
                        ViewModels.LauncherModel.Default.SetDialogFrame(errorScreen);
                        break;
                    case "CantFindExternalLauncher":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_CantFindExternalLauncher_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_CantFindExternalLauncher");
                        ViewModels.LauncherModel.Default.SetDialogFrame(errorScreen);
                        break;
                    case "appregistererror":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_AppReregisterFailed_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_AppReregisterFailed");
                        ViewModels.LauncherModel.Default.SetDialogFrame(errorScreen);
                        break;
                    case "applauncherror":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_AppLaunchFailed_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_AppLaunchFailed");
                        ViewModels.LauncherModel.Default.SetDialogFrame(errorScreen);
                        break;
                    case "downloadfailederror":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_AppDownloadFailed_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_AppDownloadFailed");
                        ViewModels.LauncherModel.Default.SetDialogFrame(errorScreen);
                        break;
                    case "notaskinpack":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_NotaSkinPack_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_NotaSkinPack");
                        ViewModels.LauncherModel.Default.SetDialogFrame(errorScreen);
                        break;
                    case "extractionfailed":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_AppExtractionFailed_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_AppExtractionFailed");
                        ViewModels.LauncherModel.Default.SetDialogFrame(errorScreen);
                        break;
                    case "CantFindPaidServerList":
                        errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "Error_CantFindPaidServerList_Title");
                        errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "Error_CantFindPaidServerList");
                        ViewModels.LauncherModel.Default.SetDialogFrame(errorScreen);
                        break;
                }
            });

        }
    }
}
