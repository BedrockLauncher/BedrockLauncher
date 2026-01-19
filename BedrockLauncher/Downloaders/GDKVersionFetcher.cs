using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.UpdateProcessor.Authentication;

namespace BedrockLauncher.Downloaders
{
    internal static class GDKVersionFetcher
    {
        public static async void FetchLatestUpdate()
        {
            Trace.WriteLine("Fetching latest update...");
            XBLiveAuthentification authentificator = new XBLiveAuthentification();
            await authentificator.GetOAuthToken();
        }
    }
}
