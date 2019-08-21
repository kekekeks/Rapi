using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreRPC.Transport.Http;
using Xunit;

namespace Rapi.Tests
{
    public class RapiSystemInfoTests : IClassFixture<RapiTestHost>
    {
        private readonly RapiTestHost _host;

        public RapiSystemInfoTests(RapiTestHost host) => _host = host;

        [Fact]
        public async Task ShouldFetchBasicSystemInfo()
        {
            var connection = await RapiConnection.Connect(new HttpClientTransport(_host.Address));
            Assert.Equal(Path.GetTempPath(), connection.FileSystemInfo.TempDirectory);
            Assert.Equal(DriveInfo.GetDrives().Length, connection.FileSystemInfo.Drives.Count);
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Assert.True(connection.SystemInfo.Platform.IsUnix);
            else Assert.False(connection.SystemInfo.Platform.IsUnix);
        }
    }
}