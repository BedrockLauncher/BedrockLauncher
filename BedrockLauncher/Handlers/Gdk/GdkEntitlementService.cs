using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BedrockLauncher.Classes;
using BedrockLauncher.UpdateProcessor.Enums;

namespace BedrockLauncher.Handlers
{
    /// <summary>
    /// Result of a Microsoft entitlement check for a GDK Minecraft version.
    /// </summary>
    internal sealed class GdkEntitlementResult
    {
        public bool IsEntitled { get; }
        public string Message { get; }
        public MicrosoftAccountIdentity Account { get; }

        public GdkEntitlementResult(bool isEntitled, string message, MicrosoftAccountIdentity account = null)
        {
            IsEntitled = isEntitled;
            Message = message;
            Account = account;
        }

        public static GdkEntitlementResult Entitled(MicrosoftAccountIdentity account) =>
            new GdkEntitlementResult(true, null, account);

        public static GdkEntitlementResult NotEntitled(string message) =>
            new GdkEntitlementResult(false, message);
    }

    /// <summary>
    /// Verifies that the user owns the entitlement required to install/launch a GDK Minecraft version,
    /// reusing the Microsoft account login already present in the launcher's Welcome flow. The GDK
    /// pipeline must authenticate against Microsoft before downloading the MSIXVC payload; this service
    /// is the single place where that entitlement is confirmed.
    /// </summary>
    internal static class GdkEntitlementService
    {
        /// <summary>
        /// Confirms the given profile has a usable Microsoft account entitlement for the requested version.
        /// First tries the account already linked to the profile silently, then falls back to the Microsoft
        /// account Windows is signed in with. UWP versions never reach this path.
        /// </summary>
        public static async Task<GdkEntitlementResult> VerifyAsync(BLProfile profile, VersionType type)
        {
            try
            {
                // Try the account the profile was created with, silently.
                if (profile != null && !string.IsNullOrWhiteSpace(profile.MicrosoftAccountId))
                {
                    bool hasToken = await MicrosoftAccountAuthentication.TryAcquireTokenSilentlyAsync(profile.MicrosoftAccountId);
                    if (hasToken)
                    {
                        return GdkEntitlementResult.Entitled(
                            new MicrosoftAccountIdentity(profile.MicrosoftAccountId, profile.MicrosoftAccountName));
                    }
                }

                // Fall back to whatever Microsoft account Windows is currently connected to.
                MicrosoftAccountIdentity account = await MicrosoftAccountAuthentication.TryGetDefaultAccountAsync();
                if (account != null)
                {
                    return GdkEntitlementResult.Entitled(account);
                }

                return GdkEntitlementResult.NotEntitled(
                    "Sign in with a Microsoft account that owns Minecraft to install this version.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"GDK entitlement verification failed for {type}: {ex}");
                return GdkEntitlementResult.NotEntitled(
                    "Could not verify your Microsoft entitlement. Please sign in again and retry.");
            }
        }
    }
}
