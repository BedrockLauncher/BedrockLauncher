using System;
using System.Windows;
using System.Windows.Controls;

namespace BedrockLauncher
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
    }
    public static class ErrorScreenShow
    {
        public static void errormsg(string error = null)
        {
            // Show default error message
            if (error == null)
            {
                ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Navigate(new ErrorScreen());
            }
            switch (error)
            {
                case "autherror":
                    ErrorScreen errorScreen = new ErrorScreen();
                    errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, "AuthenticationFailed_Title");
                    errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, "AuthenticationFailed");
                    ((MainWindow)Application.Current.MainWindow).MainWindowOverlayFrame.Navigate(errorScreen);
                    break;
            }
        }
    }
}
