using BedrockLauncher.Classes;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using BedrockLauncher.Methods;
using BedrockLauncher.UpdateProcessor;
using System.Collections.Generic;

namespace BedrockLauncher.Downloaders
{
    public class VersionDownloader
    {
        private HttpClient http_client = new HttpClient();
        private Win10StoreNetwork store_manager = new Win10StoreNetwork();

        private async Task DownloadFile(string url, string to, DownloadProgress progress, CancellationToken cancellationToken)
        {
            using (var resp = await http_client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                using (var inStream = await resp.Content.ReadAsStreamAsync())
                using (var outStream = new FileStream(to, FileMode.Create))
                {
                    long? totalSize = resp.Content.Headers.ContentLength;
                    progress(0, totalSize);
                    long transferred = 0;
                    byte[] buf = new byte[1024 * 1024];
                    while (true)
                    {
                        int n = await inStream.ReadAsync(buf, 0, buf.Length, cancellationToken);
                        if (n == 0)
                            break;
                        await outStream.WriteAsync(buf, 0, n, cancellationToken);
                        transferred += n;
                        progress(transferred, totalSize);
                    }
                }
            }
        }
        public void EnableUserAuthorization()
        {
            store_manager.setMSAUserToken(Win10AuthenticationManager.GetWUToken(Properties.LauncherSettings.Default.CurrentInsiderAccount));
        }
        public async Task Download(string versionName, string updateIdentity, int revisionNumber, string destination, DownloadProgress progress, CancellationToken cancellationToken)
        {
            string link = await store_manager.getDownloadLink(updateIdentity, revisionNumber, true);
            if (link == null)
                throw new ArgumentException(string.Format("Bad updateIdentity for {0}", versionName));
            System.Diagnostics.Debug.WriteLine("Resolved download link: " + link);
            await DownloadFile(link, destination, progress, cancellationToken);
        }

        public delegate void DownloadProgress(long current, long? total);
    }
}