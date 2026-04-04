using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Rapi.Tests
{
    internal sealed class HttpEchoHost : IAsyncDisposable
    {
        private static int _globalPort = 7000;
        private readonly WebApplication _app;

        private HttpEchoHost(WebApplication app, string address)
        {
            _app = app;
            Address = address;
        }

        public string Address { get; }

        public static async Task<HttpEchoHost> Start()
        {
            var port = Interlocked.Increment(ref _globalPort);
            var address = $"http://127.0.0.1:{port}";

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(port);
            });

            var app = builder.Build();
            app.MapMethods("/echo", ["GET", "POST"], async context =>
            {
                var body = context.Request.Query["payload"].ToString();
                if (HttpMethods.IsPost(context.Request.Method))
                {
                    using var reader = new StreamReader(context.Request.Body);
                    body = await reader.ReadToEndAsync();
                }
                var forwardedHeader = context.Request.Headers["x-rapi-test"].ToString();

                context.Response.Headers["x-rapi-response"] = "ack";
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync($"{forwardedHeader}:{body}");
            });

            await app.StartAsync();
            return new HttpEchoHost(app, address);
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
