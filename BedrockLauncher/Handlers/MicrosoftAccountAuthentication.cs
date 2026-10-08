using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace BedrockLauncher.Handlers
{
    internal sealed class MicrosoftAccountIdentity
    {
        public string Id { get; }
        public string UserName { get; }

        public MicrosoftAccountIdentity(string id, string userName)
        {
            Id = id;
            UserName = userName;
        }
    }

    internal static class MicrosoftAccountAuthentication
    {
        private const string ProviderAuthority = "https://login.microsoft.com";
        private const string ProviderId = "consumers";
        private const string TokenScope =
            "service::dcat.update.microsoft.com::MBI_SSL";
        private const string ClientId =
            "{28520974-CE92-4F36-A219-3F255AF7E61E}";

        private static async Task<WebAccountProvider> GetProviderAsync()
        {
            var provider =
                await WebAuthenticationCoreManager.FindAccountProviderAsync(
                    ProviderAuthority,
                    ProviderId);

            if (provider == null)
            {
                throw new InvalidOperationException(
                    "The Microsoft consumer account provider is unavailable.");
            }

            return provider;
        }

        /// <summary>
        /// Asks the token broker for a token of an already connected account without showing any UI.
        /// Returns false when the account is unknown to Windows or needs the user to sign in again.
        /// </summary>
        internal static async Task<bool> TryAcquireTokenSilentlyAsync(
            string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                return false;

            var provider = await GetProviderAsync();
            var account =
                await WebAuthenticationCoreManager.FindAccountAsync(
                    provider,
                    accountId);

            if (account == null)
                return false;

            var request =
                new WebTokenRequest(
                    provider,
                    TokenScope,
                    ClientId);
            var result =
                await WebAuthenticationCoreManager.GetTokenSilentlyAsync(
                    request,
                    account);

            return result.ResponseStatus == WebTokenRequestStatus.Success;
        }

        /// <summary>
        /// Returns the Microsoft account Windows is already signed in with, without showing any UI.
        /// Null when Windows has no connected Microsoft account or the user must sign in explicitly.
        /// </summary>
        internal static async Task<MicrosoftAccountIdentity> TryGetDefaultAccountAsync()
        {
            try
            {
                var provider = await GetProviderAsync();
                var request =
                    new WebTokenRequest(
                        provider,
                        TokenScope,
                        ClientId);
                var result =
                    await WebAuthenticationCoreManager.GetTokenSilentlyAsync(
                        request);

                return result.ResponseStatus == WebTokenRequestStatus.Success
                    ? ReadIdentity(result)
                    : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"Default Microsoft account lookup failed: {ex.Message}");
                return null;
            }
        }

        private static MicrosoftAccountIdentity ReadIdentity(
            WebTokenRequestResult result)
        {
            var account =
                result.ResponseData
                    .Select(response => response.WebAccount)
                    .FirstOrDefault(candidate => candidate != null);

            if (account == null ||
                string.IsNullOrWhiteSpace(account.Id) ||
                string.IsNullOrWhiteSpace(account.UserName))
            {
                return null;
            }

            return new MicrosoftAccountIdentity(
                account.Id,
                account.UserName);
        }
    }
}
