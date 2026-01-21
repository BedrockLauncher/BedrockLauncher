using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.UpdateProcessor.Authentication;
using BedrockLauncher.Handlers;

namespace BedrockLauncher.Downloaders
{
    internal static class GDKVersionFetcher
    {
        public static async void FetchLatestUpdate()
        {
            Trace.WriteLine("Fetching latest update...");
            XBLiveAuthentification authentificator = new XBLiveAuthentification();
            try 
            {
                bool isDeveloperBuild = RuntimeHandler.IsDeveloperModeEnabled();

                string? authCode = await authentificator.GetOAuthCode();

                if (string.IsNullOrEmpty(authCode))
                    throw new FormatException();

                string? authToken = await authentificator.GetOAuthToken(authCode, isDeveloperBuild);

                if (string.IsNullOrEmpty(authToken))
                    throw new FormatException();

                Trace.WriteLine(authToken);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }
    }
}
