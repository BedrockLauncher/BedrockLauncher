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

namespace BedrockLauncher.Components.CefSharp
{
#if ENABLE_CEFSHARP
    public class SkinViewResourceSchemeHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            var names = typeof(SkinView3D.Class).Assembly.GetManifestResourceNames();
            Uri u = new Uri(request.Url);

            string resourceName = string.Format("Runtimes/{0}{1}", u.Authority, u.AbsolutePath).ToLower();
            string resourceManifestName = $"BedrockLauncher.SkinView3D.{resourceName.Replace("/", ".")}";
            var assembly = typeof(SkinView3D.Class).Assembly;
            var resources = assembly.GetManifestResourceNames();
            bool validResource = resources.ToList().Exists(x => x.Equals(resourceManifestName, StringComparison.InvariantCultureIgnoreCase));

            if (validResource)
            {
                Task.Run(() =>
                {
                    string resourcePath = resources.ToList().Where(x => x.Equals(resourceManifestName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    using (callback)
                    {
                        try
                        {

                            var sri = assembly.GetManifestResourceInfo(resourcePath);

                            if (sri != null)
                            {
                                Stream stream = assembly.GetManifestResourceStream(resourcePath);
                                string mimeType = MimeMapping.MimeUtility.GetMimeMapping(resourcePath);

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
            }
            else
            {
                callback.Cancel();
                System.Diagnostics.Trace.WriteLine($"SKINVIEWRESOURCESCHEMEHANDLER: Could not find \'{resourceName}\'!");
            }

            return CefReturnValue.ContinueAsync;
        }
    }
#endif
}