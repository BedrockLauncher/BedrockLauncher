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
using System.Text.RegularExpressions;
using BedrockLauncher.UpdateProcessor.Databases;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Downloaders
{
    internal static class GDKVersionFetcher
    {
        private const string query_url = "https://packagespc.xboxlive.com/GetBasePackage/";
        private const string minecraft_uuid = "7792d9ce-355a-493c-afbd-768f4a77c3b0";
        private const string preview_uuid = "98bd2335-9b01-4e4c-bd05-ccc01614078b";

        private static async Task<List<VersionInfoJson>> RegisterLatestUpdate(string uhs, string xsts, VersionType type, List<VersionInfoJson> found)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, query_url + 
                (type == VersionType.Release ? minecraft_uuid : preview_uuid));
            request.Headers.Add("Authorization", $"XBL3.0 x={uhs};{xsts}");

            HttpResponseMessage response = await client.SendAsync(request);
            GetBasePackageResponse packageInfo = await response.Content.ReadAsJsonAsync<GetBasePackageResponse>();

            IEnumerable<string> paths = packageInfo.PackageFiles.Select(file => file.RelativeUrl).Where(url => url.Contains(".msixvc"));
            Regex pattern = new Regex(@"[^/]*/(?<release>\d*)\.(?<major>\d*)\.(?<minor>\d*)(?<patch>\d\d)[^/]*/.*_x64__8wekyb3d8bbwe\.msixvc");


            foreach (string path in paths)
            {
                Match match = pattern.Match(path);

                if (match == Match.Empty)
                {
                    Trace.WriteLine($"Failed to parse: {path}");
                    continue;
                }

                GroupCollection groups = match.Groups;
                string version_number = $"{groups["release"]}.{groups["major"]}.{groups["minor"]}.{groups["patch"]}";
                VersionInfoJson version = new VersionInfoJson($"{version_number}", path, type);
                
                Trace.WriteLine($"Found {type} version: {version_number}");
                found.Add(version);
            }

            return found;
        }

        public static async void FetchLatestUpdates()
        {
            Trace.WriteLine("Fetching latest update...");
            XSTSAuthentication authenticator = new XSTSAuthentication();

            bool isDeveloperBuild = RuntimeHandler.IsDeveloperModeEnabled();

            string? authCode = await authenticator.GetOAuthCode();

            if (string.IsNullOrEmpty(authCode))
                throw new FormatException();

            (string uhs, string xsts) = await authenticator.GetXSTSInfo(await authenticator.GetOAuthToken(authCode, isDeveloperBuild));

            List<VersionInfoJson> found = new List<VersionInfoJson>();

            await RegisterLatestUpdate(uhs, xsts, VersionType.Release, found);
            await RegisterLatestUpdate(uhs, xsts, VersionType.Preview, found);


            string filename = MainDataModel.Default.FilePaths.GetWinStoreVersionsDBFile();
            VersionJsonDb db = new VersionJsonDb();
            db.ReadJson(filename);

            IEnumerable<VersionInfoJson> new_versions = found.Except(db.list);
            foreach (VersionInfoJson version in new_versions)
                db.list.Add(version);

            db.WriteJson(filename);

            MainDataModel.Default.FetcherResult.RegisterVersions(new_versions);
            MainDataModel.Default.FetcherResult.State = Enums.VersionFetcherState.Finished;
        }
    }
}
