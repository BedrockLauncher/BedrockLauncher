using System;
using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.UI.Interfaces;

namespace BedrockLauncher.Pages.Preview
{
    /// <summary>
    /// Логика взаимодействия для ErrorScreen.xaml
    /// </summary>
    public partial class WaitingPage : Page, IDisposable
    {

        public IDialogHander Handler { get; private set; }


        public WaitingPage()
        {
            InitializeComponent();
        }

        public WaitingPage(IDialogHander _hander)
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {

        }
    }
}
