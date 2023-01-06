#if ENABLE_CEFSHARP
using CefSharp;
# endif
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
    public class ResourceSchemeHandler : ResourceHandler
    {
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            var names = this.GetType().Assembly.GetManifestResourceNames();
            Uri u = new Uri(request.Url);
            string resourceName = string.Format("{0}{1}", u.Authority, u.AbsolutePath).ToLower();
            string resourcePath = @"/BedrockLauncher;component/" + resourceName;

            var assembly = Assembly.GetExecutingAssembly();
            var rm = new ResourceManager(assembly.GetName().Name + ".g", assembly);
            var resources = rm.GetResourceSet(CultureInfo.CurrentCulture, true, true);
            var resourceDictionary = resources.Cast<DictionaryEntry>().ToDictionary(r => r.Key.ToString(), r => r.Value.ToString());
            bool validResource = resourceDictionary.ContainsKey(resourceName);

            if (validResource)
            {
                Task.Run(() =>
                {
                    using (callback)
                    {
                        try
                        {

                            StreamResourceInfo sri = Application.GetResourceStream(new Uri(resourcePath, UriKind.Relative));

                            if (sri != null)
                            {
                                Stream stream = sri.Stream;
                                string mimeType = sri.ContentType;

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
            else callback.Cancel();

            rm.ReleaseAllResources();

            return CefReturnValue.ContinueAsync;
        }
    }
#endif
}