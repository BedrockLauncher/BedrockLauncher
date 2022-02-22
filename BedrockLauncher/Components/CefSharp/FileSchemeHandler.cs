#if ENABLE_CEFSHARP
using CefSharp;
#endif
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using System.Linq;
using System.Globalization;
using System.Collections;
using MimeMapping;

namespace BedrockLauncher.Components.CefSharp
{
#if ENABLE_CEFSHARP
    public class FileSchemeHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {

            Task.Run(() =>
            {
                using (callback)
                {
                    try
                    {
                        string url = request.Url;
                        var uri = new Uri(url);

                        //Get the absolute path and remove the leading slash
                        var asbolutePath = string.Format("{0}:\\", uri.Authority) +  uri.AbsolutePath.Substring(1);

                        var filePath = WebUtility.UrlDecode(Path.GetFullPath(asbolutePath));

                        if (File.Exists(filePath))
                        {
                            Stream stream = File.OpenRead(filePath);
                            //TODO: Fix
                            string mimeType = MimeUtility.GetMimeMapping(filePath);
                            //string mimeType = string.Empty;

                            // Reset the stream position to 0 so the stream can be copied into the underlying unmanaged buffer
                            stream.Position = 0;
                            // Populate the response values - No longer need to implement GetResponseHeaders (unless you need to perform a redirect)
                            ResponseLength = stream.Length;
                            MimeType = mimeType;
                            StatusCode = (int)HttpStatusCode.OK;
                            Stream = stream;

                            callback.Continue();
                        }
                        else
                        {
                            callback.Cancel();
                        }
                    }
                    catch (Exception ex)
                    {
                        callback.Cancel();
                        System.Diagnostics.Trace.WriteLine(ex);
                    }

                }
            });

            return CefReturnValue.ContinueAsync;
        }
    }
#endif


}