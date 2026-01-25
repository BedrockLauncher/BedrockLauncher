using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using XboxWebApi.Authentication;
using XboxWebApi.Authentication.Model;
using XboxWebApi.Common;

namespace BedrockLauncher.UpdateProcessor.Authentication
{
    public class XSTSAuthentication
    {
        private const string bedrockLauncher_clientid = "2791be83-be4f-4e84-98a2-e94f8d71dd53";
        private const string oauth_url = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize";
        private const string token_url = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
        private const string redirect_url = "http://localhost:5000/BedrockLauncherOAuth/";
        private const string scopes = "XboxLive.signin";
        private static string RedirectURI => Uri.EscapeDataString(redirect_url);

        public XSTSAuthentication() { }

        private WindowsLiveResponse live_response = null;

        private static async Task<string> ListenForOAuthCodeResponse(int expectedState)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(redirect_url);
            listener.Start();
            HttpListenerContext context = await listener.GetContextAsync();   // waits for redirect
            HttpListenerRequest request = context.Request;

            try
            {
                string code = request.QueryString["code"];
                string state = request.QueryString["state"];

                if (int.Parse(state) != expectedState)
                    throw new Exception("Intercepted an invalid OAuth answer.");

                HttpListenerResponse response = context.Response;
                response.StatusCode = 302;
                response.RedirectLocation = "https://bedrocklauncher.github.io/connected";
                response.OutputStream.Close();

                return code;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
            finally
            {
                listener.Stop();
            }

        }

        public async Task<string> GetOAuthCode()
        {
            Random r = new Random();
            int state = r.Next();

            string login_url = $"{oauth_url}?client_id={bedrockLauncher_clientid}&scope={scopes}"
                + "&response_type=code&response_mode=query&prompt=select_account"
                + $"&state={state}&redirect_uri={RedirectURI}";

            Process.Start(new ProcessStartInfo
            {
                FileName = login_url,
                UseShellExecute = true
            });

            string OAuthCode = await ListenForOAuthCodeResponse(state);
            return OAuthCode;
        }

        public async Task GetOAuthToken(string code, bool developerMode = false)
        {
            HttpClient client = new HttpClient();

            Dictionary<string, string> post_content = new Dictionary<string, string>
            {
                { "client_id" , bedrockLauncher_clientid},
                { "code" , code},
                { "redirect_uri" , redirect_url},
                { "grant_type" , "authorization_code"},
                { "scope" , scopes}
            };

            HttpContent content_url = new FormUrlEncodedContent(post_content);
            HttpResponseMessage response = await client.PostAsync(token_url, content_url);

            string response_content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(developerMode ? response_content : "Failed to request OAuth token");
            }

            Dictionary<string, JsonElement> json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response_content);
            NameValueCollection parsed_content = new NameValueCollection
            {
                { "token_type", $"{json["token_type"]}" },
                { "scope", $"{json["scope"]}" },
                { "expires_in", $"{json["expires_in"]}" },
                { "access_token", $"{json["access_token"]}" },
                { "refresh_token", "unused" },
                { "user_id", "unused" },
            };
            live_response = new WindowsLiveResponse(parsed_content);
        }

        public async Task<(string, string)> GetXSTSInfo()
        {
            AccessToken token = new AccessToken(live_response);

            AuthenticationService authenticator = new AuthenticationService(live_response);
            // To circumvent a syntax difference since we use Microsoft OAuth instead of the intended Auth flow.
            // In case this causes problems down the line, remove the `d=` later.
            authenticator.AccessToken.Jwt = $"d={authenticator.AccessToken.Jwt}";
            authenticator.UserToken = await AuthenticationService.AuthenticateXASUAsync(authenticator.AccessToken);
            authenticator.XToken = await AuthenticationService.AuthenticateXSTSAsync(authenticator.UserToken, authenticator.DeviceToken, authenticator.TitleToken);

            return (authenticator.XToken.UserInformation.Userhash, authenticator.XToken.Jwt);
        }
    }
}
