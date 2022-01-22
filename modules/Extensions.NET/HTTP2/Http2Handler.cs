using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Extensions.Http2
{
    public class Http2Handler : HttpClientHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Version = new Version("2.0");
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Gecko", "20100101"));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Firefox", "91.0"));
            return base.SendAsync(request, cancellationToken);
        }


    }
}
