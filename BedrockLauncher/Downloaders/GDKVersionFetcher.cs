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
            XSTSAuthentication authenticator = new XSTSAuthentication();

            bool isDeveloperBuild = RuntimeHandler.IsDeveloperModeEnabled();

            string? authCode = await authenticator.GetOAuthCode();

            if (string.IsNullOrEmpty(authCode))
                throw new FormatException();

            await authenticator.GetOAuthToken(authCode, isDeveloperBuild);

            (string uhs, string xsts) = await authenticator.GetXSTSInfo();
        }
    }
}
