using System.Collections.Generic;
using System.Runtime.InteropServices;
using CoreRPC.Transport.Http;
using Xunit;

namespace Rapi.Tests
{
    public class RapiWebTests : IClassFixture<RapiTestHost>
    {
        private readonly RapiTestHost _host;

        public RapiWebTests(RapiTestHost host) => _host = host;

        [Fact]
        public void Should_Fetch_System_Info_Through_Proxy()
        {
            var proxy = new CoreRPCOverRapiTransport(new HttpClientTransport(_host.Address), _host.Address,
                new Dictionary<string, string>());
            var conn = RapiConnection.Connect(proxy, null).Result;
            Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Linux), conn.SystemInfo.Platform.IsLinux);
            Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), conn.SystemInfo.Platform.IsWindows);
            Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), conn.SystemInfo.Platform.IsOSX);
        }
    }
}