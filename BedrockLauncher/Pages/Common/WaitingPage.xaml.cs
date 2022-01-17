using System;
using System.Windows;
using System.Windows.Controls;
using BedrockLauncher.Interfaces;
using CefSharp;
using CefSharp.Wpf;

namespace BedrockLauncher.Pages.Common
{
    /// <summary>
    /// Логика взаимодействия для ErrorScreen.xaml
    /// </summary>
    public partial class WaitingPage : Page
    {

        public IDialogHander Handler { get; private set; }

        ChromiumWebBrowser Browser = new ChromiumWebBrowser();


        public WaitingPage()
        {
            InitializeComponent();
            InitializeChromium();
            Init();
        }

        public WaitingPage(IDialogHander _hander)
        {
            InitializeComponent();
            Handler = _hander;
            InitializeChromium();
            Init();
        }
        private void InitializeChromium()
        {
            BrowserHost.Child = Browser;
            BedrockLauncher.Components.CefSharp.CefSharpLoader.InitBrowser(ref Browser);
            Browser.VerticalAlignment = VerticalAlignment.Stretch;
            Browser.HorizontalAlignment = HorizontalAlignment.Stretch;
            Browser.Focusable = false;
            Browser.ZoomLevelIncrement = 0;
            Browser.Address = "resources://Pages/Web/Loader.html";
            Browser.LoadingStateChanged += Browser_LoadingStateChanged;
        }

        private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {

            }
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
