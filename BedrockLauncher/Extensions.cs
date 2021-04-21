using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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


        public static string GetAvaliableFileName(string fileName, string directory, string format = "_{0}")
        {
            int i = 0;

            string newFileName = fileName;

            if (!File.Exists(Path.Combine(directory, newFileName))) return newFileName;
            else while (File.Exists(Path.Combine(directory, newFileName))) 
            {
                    i++;
                    newFileName = Path.GetFileNameWithoutExtension(fileName) + string.Format(format, i) + "." + Path.GetExtension(fileName);
            }

            return string.Empty;
        }
    }
}
