using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreRPC.Transport.Http;
using NUnit.Framework;

namespace Rapi.Tests
{
    [TestFixture]
    [Ignore("Need SUT for this")]
    public class RapiWebTests
    {
        [Test]
        public async Task Should_Fetch_System_Info_Through_Proxy()
        {
            var proxy = new CoreRPCOverRapiTransport(new HttpClientTransport(RapiTestHost.Address), RapiTestHost.Address,
                new Dictionary<string, string>());
            var conn = await RapiConnection.Connect(proxy, null!);
            Assert.That(conn.SystemInfo.Platform!.IsLinux, Is.EqualTo(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)));
            Assert.That(conn.SystemInfo.Platform.IsWindows, Is.EqualTo(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)));
            Assert.That(conn.SystemInfo.Platform.IsOSX, Is.EqualTo(RuntimeInformation.IsOSPlatform(OSPlatform.OSX)));
        }
    }
}
