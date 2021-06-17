using System;
using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Core.Interfaces;

namespace BedrockLauncher.Core.Pages.Common
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

        public static void errormsg(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() => {
                ErrorScreen errorScreen = new ErrorScreen(Handler);
                errorScreen.ErrorType.SetResourceReference(TextBlock.TextProperty, title);
                errorScreen.ErrorText.SetResourceReference(TextBlock.TextProperty, message);
                Handler.SetDialogFrame(errorScreen);
            });

        }
    }
}
