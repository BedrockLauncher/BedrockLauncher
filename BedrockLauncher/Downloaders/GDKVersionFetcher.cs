using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedrockLauncher.UpdateProcessor.Authentication;
using BedrockLauncher.Handlers;
using System.Net.Http;
using BedrockLauncher.UpdateProcessor.Enums;
using XboxWebApi.Common;
using BedrockLauncher.UpdateProcessor.Classes;

namespace BedrockLauncher.Downloaders
{
    internal static class GDKVersionFetcher
    {
        private const string query_url = "https://packagespc.xboxlive.com/GetBasePackage/";
        private const string minecraft_uuid = "7792d9ce-355a-493c-afbd-768f4a77c3b0";
        private const string preview_uuid = "7792d9ce-355a-493c-afbd-768f4a77c3b0";

        private static async Task QueryLatestUpdate(string uhs, string xsts, VersionType type)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, query_url + 
                (type == VersionType.Release ? minecraft_uuid : preview_uuid));
            request.Headers.Add("Authorization", $"XBL3.0 x={uhs};{xsts}");

            HttpResponseMessage response = await client.SendAsync(request);
            var json = await response.Content.ReadAsJsonAsync<GetBasePackageResponse>();
            return;
        }

        public static async void FetchLatestUpdate()
        {
            Trace.WriteLine("Fetching latest update...");
            XSTSAuthentication authenticator = new XSTSAuthentication();

            bool isDeveloperBuild = RuntimeHandler.IsDeveloperModeEnabled();

            string? authCode = await authenticator.GetOAuthCode();

            if (string.IsNullOrEmpty(authCode))
                throw new FormatException();

            (string uhs, string xsts) = await authenticator.GetXSTSInfo(await authenticator.GetOAuthToken(authCode, isDeveloperBuild));

            await QueryLatestUpdate(uhs, xsts);
        }
    }
}
