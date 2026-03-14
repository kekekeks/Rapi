using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Rapi;

namespace RapiAgent.Rpc
{
    public class RapiWebRequestRpc : IRapiWebRequestRpc
    {
        public async Task<RapiWebResponse> SendWebRequest(RapiWebRequest req)
        {
            using (var cl = new HttpClient())
            {
                var hreq = new HttpRequestMessage(new HttpMethod(req.Method), req.Uri);
                if (req.Body != null) 
                    hreq.Content = new StreamContent(req.Body);
                cl.Timeout = TimeSpan.FromSeconds(req.Timeout);
                if(req.Headers!=null)
                    foreach (var hdr in req.Headers)
                        if (!hreq.Headers.TryAddWithoutValidation(hdr.Key, hdr.Value))
                            hreq.Content?.Headers.TryAddWithoutValidation(hdr.Key, hdr.Value);
                
                var res = await cl.SendAsync(hreq);
                return new RapiWebResponse()
                {
                    Code = (int) res.StatusCode,
                    Data = await res.Content.ReadAsStreamAsync(),
                    Headers = res.Headers.ToDictionary(x => x.Key, x => string.Join(", ", x.Value))
                };

            }
        }
    }
}