using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Rapi.Tests
{
    internal sealed class GrpcEchoHost : IAsyncDisposable
    {
        private static int _globalPort = 6000;
        private readonly WebApplication _app;

        private GrpcEchoHost(WebApplication app, string address)
        {
            _app = app;
            Address = address;
        }

        public string Address { get; }

        public static async Task<GrpcEchoHost> Start()
        {
            var port = Interlocked.Increment(ref _globalPort);
            var address = $"http://127.0.0.1:{port}";

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(port, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
            });
            builder.Services.AddGrpc();

            var app = builder.Build();
            app.MapGrpcService<TestGrpcService>();
            await app.StartAsync();

            return new GrpcEchoHost(app, address);
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
