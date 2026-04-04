using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using RapiAgent.Rpc;

namespace Rapi.Tests
{
    [TestFixture]
    public class RapiWebTests
    {
        private HttpEchoHost _httpHost = null!;
        private ServiceProvider _serviceProvider = null!;
        private RapiWebRequestRpc _rpc = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _httpHost = await HttpEchoHost.Start();

            var services = new ServiceCollection();
            services.AddHttpClient("WebRequest")
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    UseCookies = false
                });

            _serviceProvider = services.BuildServiceProvider();
            _rpc = new RapiWebRequestRpc(_serviceProvider.GetRequiredService<IHttpClientFactory>());
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (_serviceProvider != null)
                await _serviceProvider.DisposeAsync();
            if (_httpHost != null)
                await _httpHost.DisposeAsync();
        }

        [Test]
        public async Task ShouldProxyHttpRequests()
        {
            using var requestBody = new MemoryStream(Encoding.UTF8.GetBytes("payload"));
            var response = await _rpc.SendWebRequest(new RapiWebRequest
            {
                Uri = _httpHost.Address + "/echo",
                Method = "POST",
                Body = requestBody,
                Timeout = 60,
                Headers = new Dictionary<string, string>
                {
                    ["x-rapi-test"] = "forwarded"
                }
            });

            using var responseStream = response.Data!;
            using var reader = new StreamReader(responseStream, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            var headers = response.Headers!;

            Assert.That(response.Code, Is.EqualTo(200));
            Assert.That(body, Is.EqualTo("forwarded:payload"));
            Assert.That(headers["x-rapi-response"], Is.EqualTo("ack"));
            Assert.That(headers["content-type"], Does.StartWith("text/plain"));
        }
    }
}
