using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionsDotNET.HTTP2
{
    public class Http2Handler : WinHttpHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            request.Version = new Version("2.0");
            request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Mozilla", "5.0"));
            request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Gecko", "20100101"));
            request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Firefox", "91.0"));
            return base.SendAsync(request, cancellationToken);
        }
    }
}
