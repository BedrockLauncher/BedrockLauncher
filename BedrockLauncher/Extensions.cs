using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher
{
    public static class Extensions
    {
        public static IFrame GetFrame(this ChromiumWebBrowser browser, string FrameName)
        {
            IFrame frame = null;

            var identifiers = browser.GetBrowser().GetFrameIdentifiers();

            foreach (var i in identifiers)
            {
                frame = browser.GetBrowser().GetFrame(i);
                if (frame.Name == FrameName)
                    return frame;
            }

            return null;
        }
    }
}
