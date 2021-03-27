using System;
using System.Windows;
using System.Windows.Controls;
using System.Web.UI.WebControls;
using System.Windows.Documents;

namespace BedrockLauncher.Pages.ErrorScreen
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
            ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Content = null;
        }

        private void ErrorScreenViewCrashButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("notepad.exe", $@"{Environment.CurrentDirectory}\Log.txt");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Console.WriteLine("wha");
        }
    }
    public static class ErrorScreenShow
    {
        public static void errormsg(string error = null)
        {
            ErrorScreen errorScreen = new ErrorScreen();
            // Show default error message
            if (error == null)
            {
                ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Navigate(new ErrorScreen());
            }
            switch (error)
            {
                case "autherror":
                    errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "AuthenticationFailed_Title");
                    errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "AuthenticationFailed");
                    ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Navigate(errorScreen);
                    break;
                case "CantFindJavaLauncher":
                    errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "CantFindJavaLauncher_Title");
                    errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "CantFindJavaLauncher");
                    ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Navigate(errorScreen);
                    break;
                case "appregistererror":
                    errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "AppReregisterFailed_Title");
                    errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "AppReregisterFailed");
                    ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Navigate(errorScreen);
                    break;
                case "applauncherror":
                    errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "AppLaunchFailed_Title");
                    errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "AppLaunchFailed");
                    ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Navigate(errorScreen);
                    break;
                case "downloadfailederror":
                    errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "AppDownloadFailed_Title");
                    errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "AppDownloadFailed");
                    ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Navigate(errorScreen);
                    break;
                case "extractionfailed":
                    errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "AppExtractionFailed_Title");
                    errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "AppExtractionFailed");
                    ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Navigate(errorScreen);
                    break;
                case "CantFindPaidServerList":
                    errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "CantFindPaidServerList_Title");
                    errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "CantFindPaidServerList");
                    ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Navigate(errorScreen);
                    break;
            }
        }
    }
}
