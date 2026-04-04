using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Rapi;

namespace RapiAgent.Rpc
{
    public class RapiWebRequestRpc : IRapiWebRequestRpc
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RapiWebRequestRpc(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<RapiWebResponse> SendWebRequest(RapiWebRequest req)
        {
            using var httpClient = _httpClientFactory.CreateClient(RapiHttpClientNames.WebRequest);
            httpClient.Timeout = TimeSpan.FromSeconds(req.Timeout);

            using var httpRequest = new HttpRequestMessage(new HttpMethod(req.Method!), req.Uri);
            if (req.Body != null)
                httpRequest.Content = new StreamContent(req.Body);
            if (req.Headers != null)
                foreach (var header in req.Headers)
                    if (!httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value))
                        httpRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);

            using var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            var data = response.Content == null
                ? null
                : new MemoryStream(await response.Content.ReadAsByteArrayAsync(), writable: false);

            return new RapiWebResponse
            {
                Code = (int)response.StatusCode,
                Data = data,
                Headers = ToHeaders(response.Headers, response.Content?.Headers)
            };
        }

        private static Dictionary<string, string> ToHeaders(params HttpHeaders?[] headerCollections)
        {
            return headerCollections
                .Where(headers => headers != null)
                .SelectMany(headers => headers!)
                .GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => string.Join(", ", group.SelectMany(header => header.Value)),
                    StringComparer.OrdinalIgnoreCase);
        }
    }
}
