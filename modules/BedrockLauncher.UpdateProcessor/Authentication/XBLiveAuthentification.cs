using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BedrockLauncher.UpdateProcessor.Authentication
{
    public class XBLiveAuthentification
    {
        private const string bedrockLauncher_clientid = "2791be83-be4f-4e84-98a2-e94f8d71dd53";
        private const string oauth_url = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
        private const string redirect_uri = "http://localhost:5000/BedrockLauncherOAuth/";

        public XBLiveAuthentification() { }

        private static async Task<string> ListenForOAuthResponse(int expectedState)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(redirect_uri);
            listener.Start();
            HttpListenerContext context = await listener.GetContextAsync();   // waits for redirect
            HttpListenerRequest request = context.Request;

            string code = "";
            try
            {
                code = request.QueryString["code"];
                string state = request.QueryString["state"];

                if (int.Parse(state) != expectedState)
                    throw new Exception("Intercepted an invalid OAuth answer.");
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
            finally
            {
                listener.Stop();
            }
            return code;

        }

        public async Task<string> GetOAuthToken()
        {
            HttpClient client = new HttpClient();

            int state = new Random().Next();

            string login_url = $"{oauth_url}?client_id={bedrockLauncher_clientid}"
                + "&response_type=code&scope=User.Read&response_mode=query&prompt=select_account"
                + $"&state={state}";

            Process.Start(new ProcessStartInfo
            {
                FileName = login_url,
                UseShellExecute = true
            });

            string OAuthCode = await ListenForOAuthResponse(state);
            if (string.IsNullOrEmpty(OAuthCode))
                return null;

            return OAuthCode;
        }
    }
}
