using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BedrockLauncher.UpdateProcessor.Authentication
{
    public class XBLiveAuthentification
    {
        private const string bedrockLauncher_clientid = "2791be83-be4f-4e84-98a2-e94f8d71dd53";
        private const string oauth_url = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
        private const string token_url = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
        private const string redirect_url = "http://localhost:5000/BedrockLauncherOAuth/";
        private static string RedirectURI => Uri.EscapeDataString(redirect_url);

        public XBLiveAuthentification() { }

        private string code_challenge = null;

        private void GenerateS256(Random r)
        {
            char[] letters = new char[43];
            for (int i = 0; i < 43; i++)
            {
                letters[i] = (char)(r.Next() % 26 + 'A');
            }

            string s256 = new string(letters);
            code_challenge = s256;
        }

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
                response.RedirectLocation = "https://bedrocklauncher.github.io/";
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
            GenerateS256(r);

            string login_url = $"{oauth_url}?client_id={bedrockLauncher_clientid}"
                + "&response_type=code&scope=User.Read&response_mode=query&prompt=select_account"
                //+ $"&code_challenge={code_challenge}&code_challenge_method=S256"
                + $"&state={state}&redirect_uri={RedirectURI}";

            Process.Start(new ProcessStartInfo
            {
                FileName = login_url,
                UseShellExecute = true
            });

            string OAuthCode = await ListenForOAuthCodeResponse(state);
            return OAuthCode;
        }

        public async Task<string> GetOAuthToken(string code, bool developerMode = false)
        {
            HttpClient client = new HttpClient();

            Dictionary<string, string> post_content = new Dictionary<string, string>
            {
                { "client_id" , bedrockLauncher_clientid},
                { "code" , code},
                { "redirect_uri" , redirect_url},
                //{ "code_verifier", code_challenge },
                { "grant_type" , "authorization_code"},
                { "scope" , "User.Read"}
            };

            HttpContent content_url = new FormUrlEncodedContent(post_content);
            HttpResponseMessage response = await client.PostAsync(token_url, content_url);

            string response_content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(developerMode ? response_content : "Failed to request OAuth token");
            }

            using (JsonDocument json = JsonDocument.Parse(response_content))
            {
                if (json.RootElement.TryGetProperty("access_token", out JsonElement token))
                    return token.ToString();
                else
                    throw new KeyNotFoundException(developerMode ? response_content : "Failed to find OAuth token");
            }
        }
    }
}
