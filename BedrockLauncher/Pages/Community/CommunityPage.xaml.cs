#if ENABLE_CEFSHARP
using CefSharp;
using CefSharp.Wpf;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BedrockLauncher.Pages.Community
{
    /// <summary>
    /// Interaction logic for Community.xaml
    /// </summary>
    public partial class CommunityPage : Page, IDisposable
    {
#if ENABLE_CEFSHARP
        ChromiumWebBrowser Browser = new ChromiumWebBrowser();
#endif

        public CommunityPage()
        {
            InitializeComponent();
            InitializeChromium();
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            InitializeChromium();
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
            Browser.Address = "resources://Pages/Web/Donate.html";
            Browser.RequestHandler = new BrowserRequestHandler();
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

#if ENABLE_CEFSHARP
    public class BrowserRequestHandler : IRequestHandler
    {
        private string DonateURL = "https://www.paypal.com/paypalme/CarJemGenerations";


        public bool GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
        {
            callback.Dispose();
            return false;
        }

        public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            return null;
        }

        public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            if (request.Url == "https://www.paypal.com/donate")
            {
                System.Diagnostics.Process.Start(DonateURL);
                return true;
            }
            return false;
        }

        public bool OnCertificateError(IWebBrowser chromiumWebBrowser, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
        {
            callback.Dispose();
            return false;
        }

        public void OnDocumentAvailableInMainFrame(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {

        }

        public bool OnOpenUrlFromTab(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
        {
            return false;
        }

        public void OnPluginCrashed(IWebBrowser chromiumWebBrowser, IBrowser browser, string pluginPath)
        {
            throw new Exception("Plugin crashed!");
        }

        public bool OnQuotaRequest(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
        {
            callback.Dispose();
            return false;
        }

        public void OnRenderProcessTerminated(IWebBrowser chromiumWebBrowser, IBrowser browser, CefTerminationStatus status)
        {
            throw new Exception("Browser render process is terminated!");
        }

        public void OnRenderViewReady(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {

        }

        public bool OnSelectClientCertificate(IWebBrowser chromiumWebBrowser, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
        {
            callback.Dispose();
            return false;
        }
    }
#endif

}
