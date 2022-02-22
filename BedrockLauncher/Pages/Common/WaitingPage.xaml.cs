using System;
using System.Windows;
using System.Windows.Controls;
#if ENABLE_CEFSHARP
using CefSharp;
using CefSharp.Wpf;
#endif
using BedrockLauncher.UI.Interfaces;

namespace BedrockLauncher.Core.Pages.Common
{
    /// <summary>
    /// Логика взаимодействия для ErrorScreen.xaml
    /// </summary>
    public partial class WaitingPage : Page, IDisposable
    {

        public IDialogHander Handler { get; private set; }

#if ENABLE_CEFSHARP
        ChromiumWebBrowser Browser = new ChromiumWebBrowser();
#endif


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
#if ENABLE_CEFSHARP
            BrowserHost.Child = Browser;
            BedrockLauncher.Components.CefSharp.CefSharpLoader.InitBrowser(ref Browser);
            Browser.VerticalAlignment = VerticalAlignment.Stretch;
            Browser.HorizontalAlignment = HorizontalAlignment.Stretch;
            Browser.Focusable = false;
            Browser.ZoomLevelIncrement = 0;
            Browser.Address = "resources://Pages/Web/Loader.html";
            Browser.LoadingStateChanged += Browser_LoadingStateChanged;
#endif
        }

#if ENABLE_CEFSHARP
        private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {

            }
        }
#endif


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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
#if ENABLE_CEFSHARP
            Browser?.Dispose();
#endif
        }
    }
}
