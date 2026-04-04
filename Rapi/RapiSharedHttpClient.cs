using System;
using System.Net.Http;

namespace Rapi
{
    internal static class RapiSharedHttpClient
    {
        public static HttpClient Instance { get; } = new(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            UseCookies = false
        });
    }
}
