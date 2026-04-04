using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Rapi;

namespace RapiAgent.Rpc
{
    internal class RapiGrpcClientRpc : IRapiGrpcClient
    {
        public async Task<RapiGrpcResponse> SendGrpcRequest(RapiGrpcRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Uri))
                throw new ArgumentException("Request URI is required.", nameof(request));

            using var httpClient = new HttpClient
            {
                Timeout = request.Timeout > 0
                    ? TimeSpan.FromSeconds(request.Timeout)
                    : TimeSpan.FromMinutes(1)
            };
            using var httpRequest = new HttpRequestMessage(
                string.IsNullOrWhiteSpace(request.Method) ? HttpMethod.Post : new HttpMethod(request.Method),
                request.Uri);

            httpRequest.Version = CreateVersion(request.VersionMajor, request.VersionMinor);
            httpRequest.VersionPolicy = CreateVersionPolicy(request.VersionPolicy);
            if (request.Body != null)
                httpRequest.Content = new ByteArrayContent(request.Body);

            ApplyHeaders(httpRequest, request.Headers);

            using var httpResponse = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            return new RapiGrpcResponse
            {
                Code = (int)httpResponse.StatusCode,
                Data = httpResponse.Content != null ? await httpResponse.Content.ReadAsByteArrayAsync() : null,
                VersionMajor = httpResponse.Version.Major,
                VersionMinor = httpResponse.Version.Minor,
                Headers = ToHeaders(httpResponse.Headers, httpResponse.Content?.Headers),
                Trailers = ToHeaders(httpResponse.TrailingHeaders)
            };
        }

        private static Version CreateVersion(int major, int minor)
        {
            if (major < 1)
                return new Version(2, 0);
            if (minor < 0)
                return new Version(major, 0);
            return new Version(major, minor);
        }

        private static HttpVersionPolicy CreateVersionPolicy(int policy)
        {
            return Enum.IsDefined(typeof(HttpVersionPolicy), policy)
                ? (HttpVersionPolicy)policy
                : HttpVersionPolicy.RequestVersionExact;
        }

        private static void ApplyHeaders(HttpRequestMessage request, List<RapiGrpcHeader>? headers)
        {
            if (headers == null)
                return;

            foreach (var header in headers)
            {
                if (header.Name == null || header.Value == null)
                    continue;
                if (!request.Headers.TryAddWithoutValidation(header.Name, header.Value))
                {
                    request.Content ??= new ByteArrayContent([]);
                    request.Content?.Headers.TryAddWithoutValidation(header.Name, header.Value);
                }
            }
        }

        private static List<RapiGrpcHeader> ToHeaders(params HttpHeaders?[] headerCollections)
        {
            return headerCollections
                .Where(headers => headers != null)
                .SelectMany(headers => headers!.SelectMany(header => header.Value.Select(value => new RapiGrpcHeader
                {
                    Name = header.Key,
                    Value = value
                })))
                .ToList();
        }
    }
}
