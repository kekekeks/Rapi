using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Rapi.Tests
{
    [TestFixture]
    public class RapiFileStreamTests
    {
        [Test]
        public async Task ShouldTransferFilesThroughFileStreamEndpoints()
        {
            var connection = await RapiConnection.Connect(RapiTestHost.Address);
            Assert.That(connection.RapiFileStream, Is.Not.Null);

            var root = connection.Path.Combine(connection.FileSystemInfo.TempDirectory!, RapiTestHost.DirectoryName);
            if (!await connection.FileSystem.DirectoryExists(root))
                await connection.FileSystem.CreateDirectory(root);

            var file = connection.Path.Combine(root, "file-stream.txt");
            var expected = "file stream payload";

            using (var writeStream = new MemoryStream(Encoding.UTF8.GetBytes(expected)))
                await connection.RapiFileStream!.WriteFileContents(file, writeStream);

            using var readStream = await connection.RapiFileStream!.ReadFileContentsStream(file);
            using var reader = new StreamReader(readStream, Encoding.UTF8);
            var actual = await reader.ReadToEndAsync();

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
