//using CefSharp;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
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
    public partial class CommunityPage : Page
    {

        private string DonateURL = "https://www.paypal.com/paypalme/CarJemGenerations";

        public CommunityPage()
        {
            InitializeComponent();
        }

        private async void Page_Initialized(object sender, EventArgs e)
        {
            
            string uri = "pack://application:,,,/BedrockLauncher;component/Pages/Web/Donate.html";
            var resource = Application.GetResourceStream(new Uri(uri));
            StreamReader streamReader = new StreamReader(resource.Stream);
            StringReader stringReader = new StringReader(streamReader.ReadToEnd());
            string html = await stringReader.ReadToEndAsync();
            var env = await Program.GetCoreWebView2Environment();
            await Browser.EnsureCoreWebView2Async(env);
            Browser.CoreWebView2.Settings.IsScriptEnabled = true;
            Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            Browser.CoreWebView2.NavigateToString(html);
            
        }

        private void Browser_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri == "https://www.paypal.com/donate")
            {
                System.Diagnostics.Process.Start(DonateURL);
                Browser.Stop();
                e.Cancel = true;
            }
            e.Cancel = false;
        }
    }
}
