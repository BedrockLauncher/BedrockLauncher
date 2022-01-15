using System;
using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Interfaces;

namespace BedrockLauncher.Pages.Common
{
    /// <summary>
    /// Логика взаимодействия для ErrorScreen.xaml
    /// </summary>
    public partial class WaitingPage : Page
    {

        public IDialogHander Handler { get; private set; }

        public WaitingPage()
        {
            InitializeComponent();
            Init();
        }

        public WaitingPage(IDialogHander _hander)
        {
            InitializeComponent();
            Handler = _hander;
            Init();
        }


        private void Init()
        {

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
}
