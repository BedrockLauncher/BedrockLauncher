using System.Net.Http;

namespace BedrockLauncher.Downloaders
{
    internal static class SharedHttpClient
    {
        internal static HttpClient Instance { get; } = new HttpClient();
    }
}