using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Rapi
{
    public class RapiHttpMessageHandler : HttpMessageHandler
    {
        private readonly IRapiWebRequestRpc _rapiWeb;

        public RapiHttpMessageHandler(IRapiWebRequestRpc rapiWeb)
        {
            _rapiWeb = rapiWeb;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var hdrs = new Dictionary<string, string>();
            foreach (var hdr in request.Content.Headers)
                hdrs[hdr.Key] = hdr.Value.First();
            foreach (var hdr in request.Headers)
                hdrs[hdr.Key] = hdr.Value.First();

            var res = await _rapiWeb.SendWebRequest(new RapiWebRequest
            {
                Headers = hdrs,
                Body = await request.Content.ReadAsByteArrayAsync(),
                Method = request.Method.ToString().ToUpperInvariant(),
                Timeout = 60,
                Uri = request.RequestUri.ToString()
            });



            var resp = new HttpResponseMessage((HttpStatusCode) res.Code)
            {
                Content = new ByteArrayContent(res.Data)
            };

            if (res.Headers != null)
                foreach (var hdr in res.Headers)
                    if (!resp.Headers.TryAddWithoutValidation(hdr.Key, hdr.Value))
                        resp.Content?.Headers.TryAddWithoutValidation(hdr.Key, hdr.Value);
            return resp;
        }
    }
}