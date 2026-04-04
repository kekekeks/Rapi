using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Rapi
{
    public class RapiGrpcMessageHandler : HttpMessageHandler
    {
        private readonly IRapiGrpcClient _rapiGrpc;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

        public RapiGrpcMessageHandler(IRapiGrpcClient rapiGrpc)
        {
            _rapiGrpc = rapiGrpc;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await _rapiGrpc.SendGrpcRequest(new RapiGrpcRequest
            {
                Uri = request.RequestUri?.ToString(),
                Method = request.Method.Method,
                Body = request.Content != null ? await request.Content.ReadAsByteArrayAsync(cancellationToken) : null,
                Timeout = (int)Math.Ceiling(Timeout.TotalSeconds),
                VersionMajor = request.Version.Major,
                VersionMinor = request.Version.Minor,
                VersionPolicy = (int)request.VersionPolicy,
                Headers = ToHeaders(request)
            });

            var httpResponse = new HttpResponseMessage((HttpStatusCode)response.Code)
            {
                Content = new StreamContent(new MemoryStream(response.Data ?? [])),
                RequestMessage = request,
                Version = CreateVersion(response.VersionMajor, response.VersionMinor)
            };

            ApplyHeaders(httpResponse, response.Headers);
            ApplyTrailers(httpResponse, response.Trailers);
            return httpResponse;
        }

        private static Version CreateVersion(int major, int minor)
        {
            if (major < 1)
                return new Version(2, 0);
            if (minor < 0)
                return new Version(major, 0);
            return new Version(major, minor);
        }

        private static List<RapiGrpcHeader> ToHeaders(HttpRequestMessage request)
        {
            var headers = new List<RapiGrpcHeader>();
            if (request.Content != null)
                headers.AddRange(ToHeaders(request.Content.Headers));
            headers.AddRange(ToHeaders(request.Headers));
            return headers;
        }

        private static IEnumerable<RapiGrpcHeader> ToHeaders(HttpHeaders headers) =>
            headers.SelectMany(header => header.Value.Select(value => new RapiGrpcHeader
            {
                Name = header.Key,
                Value = value
            }));

        private static void ApplyHeaders(HttpResponseMessage response, List<RapiGrpcHeader>? headers)
        {
            if (headers == null)
                return;

            foreach (var header in headers)
            {
                if (header.Name == null || header.Value == null)
                    continue;
                if (!response.Headers.TryAddWithoutValidation(header.Name, header.Value))
                    response.Content?.Headers.TryAddWithoutValidation(header.Name, header.Value);
            }
        }

        private static void ApplyTrailers(HttpResponseMessage response, List<RapiGrpcHeader>? trailers)
        {
            if (trailers == null)
                return;

            foreach (var trailer in trailers)
            {
                if (trailer.Name == null || trailer.Value == null)
                    continue;
                response.TrailingHeaders.TryAddWithoutValidation(trailer.Name, trailer.Value);
            }
        }
    }
}
