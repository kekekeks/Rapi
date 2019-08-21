using System.Text;
using System.Threading.Tasks;
using CoreRPC.Transport.Http;
using Xunit;
using Xunit.Abstractions;

namespace Rapi.Tests
{
    public class RapiSftpTests : IClassFixture<RapiTestHost>
    {
        private readonly ITestOutputHelper _output;
        private readonly RapiTestHost _host;

        public RapiSftpTests(RapiTestHost host, ITestOutputHelper output)
        {
            _output = output;
            _host = host;
        }
        
        [Fact]
        public async Task ShouldDownloadFilesViaSftp()
        {
            var connection = await RapiConnection.Connect(new HttpClientTransport(_host.Address));
            var input = connection.Path.Combine(connection.FileSystemInfo.TempDirectory, "should_download_sftp_in");
            await connection.FileSystem.WriteFileContents(input, Encoding.UTF8.GetBytes("super secret"));
            _output.WriteLine("should_download_sftp_in contents successfully written.");

            var output = connection.Path.Combine(connection.FileSystemInfo.TempDirectory, "should_download_sftp_out");
            _output.WriteLine($"Downloading file from {input} to {output}");
            await connection.Sftp.Download(input, output, _host.Configuration.Sftp);

            var downloaded = await connection.FileSystem.ReadFileContents(output);
            var words = Encoding.UTF8.GetString(downloaded);
            Assert.Equal("super secret", words);
        }
        
        [Fact]
        public async Task ShouldUploadFilesViaSftp()
        {
            var connection = await RapiConnection.Connect(new HttpClientTransport(_host.Address));
            var input = connection.Path.Combine(connection.FileSystemInfo.TempDirectory, "should_upload_sftp_in");
            await connection.FileSystem.WriteFileContents(input, Encoding.UTF8.GetBytes("super secret"));
            _output.WriteLine("should_upload_sftp_in contents successfully written.");

            var output = connection.Path.Combine(connection.FileSystemInfo.TempDirectory, "should_upload_sftp_out");
            _output.WriteLine($"Uploading file from {input} to {output}");
            await connection.Sftp.Upload(input, output, _host.Configuration.Sftp);

            var downloaded = await connection.FileSystem.ReadFileContents(output);
            var words = Encoding.UTF8.GetString(downloaded);
            Assert.Equal("super secret", words);
        }
    }
}