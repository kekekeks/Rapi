using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Rapi.Tests
{
    [TestFixture]
    public class RapiSystemInfoTests
    {
        [Test]
        public async Task ShouldFetchBasicSystemInfo()
        {
            var connection = await RapiConnection.Connect(RapiTestHost.Address);
            Assert.That(connection.FileSystemInfo.TempDirectory, Is.EqualTo(Path.GetTempPath()));
            Assert.That(connection.FileSystemInfo.Drives!.Count, Is.EqualTo(DriveInfo.GetDrives().Length));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Assert.That(connection.SystemInfo.Platform!.IsUnix, Is.True);
            else
                Assert.That(connection.SystemInfo.Platform!.IsUnix, Is.False);
        }
    }
}
